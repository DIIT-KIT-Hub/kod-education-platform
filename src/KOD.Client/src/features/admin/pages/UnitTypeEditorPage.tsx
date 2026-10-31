import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Info, Save, Trash2 } from "lucide-react";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { UnitTypeItem } from "@/shared/api/schema";
import { unitTypeIcons } from "@/shared/lib/unitTypeIcons";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { Button } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { Checkbox } from "@/shared/ui/Checkbox";
import { confirm } from "@/shared/ui/ConfirmHost";
import { Field } from "@/shared/ui/Field";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Spinner } from "@/shared/ui/Spinner";
import { Textarea } from "@/shared/ui/Textarea";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { adminApi, adminKeys, unitTypeQuery } from "../api";
import styles from "./admin.module.css";

/**
 * Creates or changes a unit type.
 * @returns The page.
 */
export function UnitTypeEditorPage() {
    const { t } = useLingui();
    const { id } = useParams();
    const existing = useQuery({ ...unitTypeQuery(id ?? ""), enabled: id !== undefined });

    if (id !== undefined && existing.isPending) {
        return <Spinner block />;
    }

    if (existing.isError) {
        return <Alert tone="danger">{describeError(existing.error, t`Не вдалося завантажити тип підрозділу.`)}</Alert>;
    }

    return <UnitTypeForm key={id ?? "new"} existing={existing.data ?? null} />;
}

/**
 * The unit type form.
 * @param props The type being changed, or null for a new one.
 * @returns The form.
 */
function UnitTypeForm({ existing }: { readonly existing: UnitTypeItem | null }) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const cabinetWide = hasPermission(useCurrentUser(), Permissions.administrationAll);
    const [error, setError] = useState<string | null>(null);
    const [deleting, setDeleting] = useState(false);

    const schema = z.object({
        name: z
            .string()
            .trim()
            .min(1, t`Вкажіть назву.`)
            .max(128),
        code: z
            .string()
            .trim()
            .min(1, t`Вкажіть код.`)
            .max(32)
            .regex(/^[A-Za-z0-9_-]+$/u, t`Лише латинські літери, цифри, «-» та «_».`),
        description: z.string().trim().max(512),
        icon: z.string(),
        isOrganization: z.boolean(),
        sortOrder: z.number({ error: t`Вкажіть число.` }).int(),
    });

    const { register, control, handleSubmit, formState } = useForm({
        resolver: zodResolver(schema),
        defaultValues: {
            name: existing?.name ?? "",
            code: existing?.code ?? "",
            description: existing?.description ?? "",
            icon: existing?.icon ?? "folder-tree",
            isOrganization: existing?.isOrganization ?? false,
            sortOrder: existing?.sortOrder ?? 100,
        },
    });

    /**
     * Saves the type.
     * @param values The form values.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        const command = { ...values, description: values.description || null };
        try {
            if (existing) {
                await adminApi.updateUnitType(existing.id, command);
            } else {
                await adminApi.createUnitType(command);
            }

            await queryClient.invalidateQueries({ queryKey: adminKeys.all });
            await queryClient.invalidateQueries({ queryKey: ["structure"] });
            toast(t`Тип підрозділу збережено`);
            await navigate("/admin/unit-types", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зберегти тип підрозділу. Спробуйте ще раз.`));
        }
    }

    /**
     * Deletes the type after a confirmation.
     */
    async function remove(): Promise<void> {
        if (!existing || !(await confirm({ title: t`Видалити тип підрозділу?`, confirmText: t`Видалити`, danger: true }))) {
            return;
        }

        setDeleting(true);
        try {
            await adminApi.deleteUnitType(existing.id);
            await queryClient.invalidateQueries({ queryKey: adminKeys.all });
            toast(t`Тип підрозділу видалено`);
            await navigate("/admin/unit-types", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
        } finally {
            setDeleting(false);
        }
    }

    return (
        <>
            <PageHeader
                title={existing?.name ?? t`Новий тип підрозділу`}
                breadcrumbs={[
                    { text: t`Адміністрування`, to: "/admin" },
                    { text: t`Структура`, to: "/admin/units" },
                    { text: t`Типи підрозділів`, to: "/admin/unit-types" },
                    { text: existing ? t`Редагування` : t`Новий` },
                ]}
            >
                {existing && (
                    <div className={layout.cluster}>
                        <Badge tone="dashed">
                            <span className={layout.mono}>{existing.code}</span>
                        </Badge>
                        <Badge>{t`Підрозділів: ${existing.unitCount}`}</Badge>
                    </div>
                )}
            </PageHeader>

            <form className={layout.split} noValidate onSubmit={handleSubmit(save)}>
                <fieldset className={[styles.fieldset, layout.stackLg].join(" ")} disabled={!cabinetWide}>
                    {!cabinetWide && (
                        <Alert tone="outline" icon={Info}>
                            {t`Каталог типів спільний для всіх організацій кабінету, тому змінювати його може лише адміністратор усіх організацій.`}
                        </Alert>
                    )}
                    {error && <Alert tone="danger">{error}</Alert>}
                    <Card>
                        <div className={styles.formGrid}>
                            <Field
                                label={t`Назва`}
                                required
                                hint={t`Як рівень називається у вашій організації: «Департамент», «Кафедра», «Відділення», «Цех»…`}
                                error={formState.errors.name?.message}
                            >
                                {(ids) => <TextInput {...ids} {...register("name")} maxLength={128} />}
                            </Field>
                            <Field label={t`Код`} required hint={t`Латиниця, цифри, «-», «_».`} error={formState.errors.code?.message}>
                                {(ids) => (
                                    <TextInput
                                        {...ids}
                                        {...register("code")}
                                        className={styles.mono}
                                        maxLength={32}
                                        placeholder="WORKSHOP"
                                    />
                                )}
                            </Field>
                            <Field label={t`Опис`} className={styles.full}>
                                {(ids) => <Textarea {...ids} {...register("description")} maxLength={512} rows={2} />}
                            </Field>
                            <Field label={t`Порядок у списках`} error={formState.errors.sortOrder?.message}>
                                {(ids) => <TextInput {...ids} {...register("sortOrder", { valueAsNumber: true })} type="number" />}
                            </Field>
                            <div className={styles.full}>
                                <Checkbox
                                    {...register("isOrganization")}
                                    hint={t`Підрозділи цього типу можуть стояти на верхньому рівні структури й обмежують дозвіл «Документи організації». Підходить для компанії, установи, навчального чи медичного закладу, фонду, дочірнього підприємства.`}
                                >
                                    {t`Це організація`}
                                </Checkbox>
                            </div>
                        </div>
                    </Card>
                    <Card title={t`Іконка`}>
                        <Controller
                            control={control}
                            name="icon"
                            render={({ field }) => (
                                <div className={styles.icons} role="radiogroup" aria-label={t`Іконка`}>
                                    {Object.entries(unitTypeIcons).map(([name, icon]) => (
                                        <label key={name} className={styles.icon} title={name}>
                                            <input
                                                type="radio"
                                                name={field.name}
                                                value={name}
                                                checked={field.value === name}
                                                aria-label={name}
                                                onChange={() => field.onChange(name)}
                                            />
                                            <Icon icon={icon} />
                                        </label>
                                    ))}
                                </div>
                            )}
                        />
                    </Card>
                </fieldset>
                {cabinetWide && (
                    <div className={layout.stackLg}>
                        <Card variant="muted">
                            <div className={layout.stack}>
                                <Button type="submit" block icon={Save} busy={formState.isSubmitting} disabled={deleting}>
                                    {t`Зберегти`}
                                </Button>
                                {existing && (
                                    <Button
                                        variant="danger"
                                        block
                                        icon={Trash2}
                                        busy={deleting}
                                        disabled={formState.isSubmitting || existing.unitCount > 0}
                                        onClick={() => void remove()}
                                    >
                                        {t`Видалити тип`}
                                    </Button>
                                )}
                                {existing && existing.unitCount > 0 && (
                                    <span className={[layout.textSm, layout.muted].join(" ")}>
                                        {t`Тип можна видалити, коли жоден підрозділ його не має.`}
                                    </span>
                                )}
                            </div>
                        </Card>
                    </div>
                )}
            </form>
        </>
    );
}
