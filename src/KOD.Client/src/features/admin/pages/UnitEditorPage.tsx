import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, Info, Save } from "lucide-react";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Link, useNavigate, useParams, useSearchParams } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { UnitDetails, UnitOption, UnitTypeItem } from "@/shared/api/schema";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { Checkbox } from "@/shared/ui/Checkbox";
import { Field } from "@/shared/ui/Field";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { adminApi, adminKeys, unitOptionsQuery, unitQuery, unitTypesQuery } from "../api";
import styles from "./admin.module.css";

/**
 * Creates a unit or an organization (`?top=true`, `?parent=`) or changes one.
 * @returns The page.
 */
export function UnitEditorPage() {
    const { t } = useLingui();
    const { id } = useParams();
    const existing = useQuery({ ...unitQuery(id ?? ""), enabled: id !== undefined });
    const options = useQuery(unitOptionsQuery);
    const types = useQuery(unitTypesQuery);

    if (options.isPending || types.isPending || (id !== undefined && existing.isPending)) {
        return <Spinner block />;
    }

    const error = existing.error ?? options.error ?? types.error;
    if (error) {
        return <Alert tone="danger">{describeError(error, t`Не вдалося завантажити підрозділ.`)}</Alert>;
    }

    return <UnitForm key={id ?? "new"} existing={existing.data ?? null} units={options.data ?? []} types={types.data ?? []} />;
}

/**
 * Props of {@link UnitForm}.
 */
interface UnitFormProps {
    readonly existing: UnitDetails | null;
    readonly units: readonly UnitOption[];
    readonly types: readonly UnitTypeItem[];
}

/**
 * The unit form.
 * @param props The unit and the choices.
 * @returns The form.
 */
function UnitForm({ existing, units, types }: UnitFormProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [params] = useSearchParams();
    const cabinetWide = hasPermission(useCurrentUser(), Permissions.administrationAll);
    const top = existing === null && params.get("top") === "true";
    const canBeTopLevel = cabinetWide || existing?.parentId === null;
    const [error, setError] = useState<string | null>(null);

    // A unit of an organization the user does not manage, or a new organization without the cabinet-wide permission.
    const notice = existing
        ? existing.canManage
            ? null
            : t`Підрозділ належить організації, структурою якої ви не керуєте, тому його можна лише переглянути.`
        : top && !cabinetWide
          ? t`Організації верхнього рівня створює лише адміністратор усіх організацій. Ви можете додавати підрозділи й дочірні організації у своїх організаціях.`
          : null;
    const readOnly = notice !== null;

    const parentFromAddress = units.find((unit) => unit.id === params.get("parent"))?.id;
    const defaultParent = top ? "" : (parentFromAddress ?? units[0]?.id ?? "");
    const defaultType = types.find((type) => type.isOrganization === (defaultParent === ""))?.id ?? "";

    const schema = z.object({
        name: z
            .string()
            .trim()
            .min(1, t`Вкажіть назву.`)
            .max(256),
        shortName: z.string().trim().max(64),
        code: z
            .string()
            .trim()
            .min(1, t`Вкажіть код.`)
            .max(32)
            .regex(/^[A-Za-z0-9_-]+$/u, t`Лише латинські літери, цифри, «-» та «_».`),
        typeId: z.string().min(1, t`Оберіть тип.`),
        parentId: z.string(),
        sortOrder: z.number({ error: t`Вкажіть число.` }).int(),
        isActive: z.boolean(),
    });

    const { register, control, handleSubmit, formState } = useForm({
        resolver: zodResolver(schema),
        defaultValues: {
            name: existing?.name ?? "",
            shortName: existing?.shortName ?? "",
            code: existing?.code ?? "",
            typeId: existing?.type.id ?? defaultType,
            parentId: existing ? (existing.parentId ?? "") : defaultParent,
            sortOrder: existing?.sortOrder ?? 0,
            isActive: existing?.isActive ?? true,
        },
    });

    /**
     * Saves the unit.
     * @param values The form values.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        const command = {
            code: values.code,
            name: values.name,
            shortName: values.shortName || null,
            typeId: values.typeId,
            parentId: values.parentId || null,
            sortOrder: values.sortOrder,
            isActive: values.isActive,
        };
        try {
            if (existing) {
                await adminApi.updateUnit(existing.id, command);
            } else {
                await adminApi.createUnit(command);
            }

            await queryClient.invalidateQueries({ queryKey: adminKeys.all });
            await queryClient.invalidateQueries({ queryKey: ["structure"] });
            toast(t`Підрозділ збережено`);
            await navigate("/admin/units", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зберегти підрозділ. Спробуйте ще раз.`));
        }
    }

    // Organization types first, then the levels inside organizations.
    const typeOptions = [...types]
        .sort((a, b) => Number(b.isOrganization) - Number(a.isOrganization))
        .map((type) => ({ value: type.id, label: type.isOrganization ? `${type.name} · ${t`організація`}` : type.name }));
    const parentOptions = [
        ...(canBeTopLevel ? [{ value: "", label: t`— немає (організація верхнього рівня) —` }] : []),
        ...units.filter((unit) => unit.id !== existing?.id).map((unit) => ({ value: unit.id, label: unit.name, depth: unit.depth })),
    ];

    return (
        <>
            <PageHeader
                title={existing ? existing.name : top ? t`Нова організація` : t`Новий підрозділ`}
                breadcrumbs={[
                    { text: t`Адміністрування`, to: "/admin" },
                    { text: t`Структура`, to: "/admin/units" },
                    { text: existing ? t`Редагування` : t`Новий` },
                ]}
                actions={
                    existing && (
                        <ButtonLink to={`/structure/${existing.id}`} variant="secondary" icon={Eye}>
                            {t`Переглянути`}
                        </ButtonLink>
                    )
                }
            />

            <form className={layout.split} noValidate onSubmit={handleSubmit(save)}>
                <div className={layout.stackLg}>
                    {notice && (
                        <Alert tone="outline" icon={Info}>
                            {notice}
                        </Alert>
                    )}
                    {error && <Alert tone="danger">{error}</Alert>}
                    <Card>
                        <fieldset className={styles.formGrid} disabled={readOnly}>
                            <Field label={t`Повна назва`} required error={formState.errors.name?.message} className={styles.full}>
                                {(ids) => <TextInput {...ids} {...register("name")} maxLength={256} />}
                            </Field>
                            <Field label={t`Скорочена назва`}>
                                {(ids) => <TextInput {...ids} {...register("shortName")} maxLength={64} />}
                            </Field>
                            <Field
                                label={t`Код`}
                                required
                                hint={t`Латиниця, цифри, «-», «_». Унікальний серед усіх організацій; використовується в імпорті.`}
                                error={formState.errors.code?.message}
                            >
                                {(ids) => <TextInput {...ids} {...register("code")} className={styles.mono} maxLength={32} />}
                            </Field>
                            <Field
                                label={t`Тип`}
                                required
                                error={formState.errors.typeId?.message}
                                hint={
                                    cabinetWide && (
                                        <>
                                            {t`Немає потрібного типу?`}{" "}
                                            <Link className={layout.link} to="/admin/unit-types/new">
                                                {t`Додайте його`}
                                            </Link>
                                        </>
                                    )
                                }
                            >
                                {(ids) => (
                                    <Controller
                                        control={control}
                                        name="typeId"
                                        render={({ field }) => (
                                            <Select
                                                {...ids}
                                                ref={field.ref}
                                                value={field.value}
                                                onChange={field.onChange}
                                                onBlur={field.onBlur}
                                                disabled={readOnly}
                                                placeholder={t`— оберіть тип —`}
                                                options={typeOptions}
                                            />
                                        )}
                                    />
                                )}
                            </Field>
                            <Field
                                label={t`Батьківський підрозділ`}
                                hint={
                                    canBeTopLevel
                                        ? t`Без батьківського підрозділу — окрема організація верхнього рівня.`
                                        : t`Лише підрозділи організацій, структурою яких ви керуєте.`
                                }
                            >
                                {(ids) => (
                                    <Controller
                                        control={control}
                                        name="parentId"
                                        render={({ field }) => (
                                            <Select
                                                {...ids}
                                                ref={field.ref}
                                                value={field.value}
                                                onChange={field.onChange}
                                                onBlur={field.onBlur}
                                                disabled={readOnly}
                                                options={parentOptions}
                                            />
                                        )}
                                    />
                                )}
                            </Field>
                            <Field label={t`Порядок відображення`} error={formState.errors.sortOrder?.message}>
                                {(ids) => <TextInput {...ids} {...register("sortOrder", { valueAsNumber: true })} type="number" />}
                            </Field>
                            <div className={styles.full}>
                                <Checkbox {...register("isActive")} hint={t`Неактивні підрозділи зберігаються для історії документів.`}>
                                    {t`Активний`}
                                </Checkbox>
                            </div>
                        </fieldset>
                    </Card>
                </div>
                {!readOnly && (
                    <div className={layout.stackLg}>
                        <Card variant="muted">
                            <Button type="submit" block icon={Save} busy={formState.isSubmitting}>
                                {t`Зберегти`}
                            </Button>
                        </Card>
                    </div>
                )}
            </form>
        </>
    );
}
