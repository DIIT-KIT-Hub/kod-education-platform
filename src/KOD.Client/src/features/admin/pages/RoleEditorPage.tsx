import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Save, Trash2 } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { PermissionDefinition, RoleDetails } from "@/shared/api/schema";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { Button } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { Checkbox } from "@/shared/ui/Checkbox";
import { confirm } from "@/shared/ui/ConfirmHost";
import { Field } from "@/shared/ui/Field";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Spinner } from "@/shared/ui/Spinner";
import { Textarea } from "@/shared/ui/Textarea";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { adminApi, adminKeys, permissionsQuery, roleQuery } from "../api";
import { PermissionModules } from "../components/PermissionModules";
import styles from "./admin.module.css";

/**
 * Creates or changes a role and its permissions.
 * @returns The page.
 */
export function RoleEditorPage() {
    const { t } = useLingui();
    const { id } = useParams();
    const existing = useQuery({ ...roleQuery(id ?? ""), enabled: id !== undefined });
    const permissions = useQuery(permissionsQuery);

    if (permissions.isPending || (id !== undefined && existing.isPending)) {
        return <Spinner block />;
    }

    const error = existing.error ?? permissions.error;
    if (error) {
        return <Alert tone="danger">{describeError(error, t`Не вдалося завантажити роль.`)}</Alert>;
    }

    return <RoleForm key={id ?? "new"} existing={existing.data ?? null} catalog={permissions.data ?? []} />;
}

/**
 * Props of {@link RoleForm}.
 */
interface RoleFormProps {
    readonly existing: RoleDetails | null;
    readonly catalog: readonly PermissionDefinition[];
}

/**
 * The role form: the permission matrix and the role's name.
 * @param props The role and the permission catalog.
 * @returns The form.
 */
function RoleForm({ existing, catalog }: RoleFormProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [granted, setGranted] = useState<ReadonlySet<string>>(() => new Set(existing?.permissions ?? []));
    const [error, setError] = useState<string | null>(null);
    const [deleting, setDeleting] = useState(false);
    const readOnly = existing?.isAdministrator === true;

    const schema = z.object({
        code: z
            .string()
            .trim()
            .min(1, t`Вкажіть код ролі.`)
            .max(64)
            .regex(/^[a-z0-9-]+$/u, t`Лише малі латинські літери, цифри та «-».`),
        name: z
            .string()
            .trim()
            .min(1, t`Вкажіть назву ролі.`)
            .max(128),
        description: z.string().trim().max(512),
        isLeadership: z.boolean(),
    });

    const { register, handleSubmit, formState } = useForm({
        resolver: zodResolver(schema),
        defaultValues: {
            code: existing?.code ?? "",
            name: existing?.name ?? "",
            description: existing?.description ?? "",
            isLeadership: existing?.isLeadership ?? false,
        },
    });

    /**
     * Grants or withdraws a permission.
     * @param code The permission.
     * @param on Whether it is granted.
     */
    function toggle(code: string, on: boolean): void {
        setGranted((current) => {
            const next = new Set(current);
            if (on) {
                next.add(code);
            } else {
                next.delete(code);
            }

            return next;
        });
    }

    /**
     * Saves the role.
     * @param values The form values.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        const command = { ...values, description: values.description || null, permissions: [...granted] };
        try {
            if (existing) {
                await adminApi.updateRole(existing.id, command);
            } else {
                await adminApi.createRole(command);
            }

            await queryClient.invalidateQueries({ queryKey: adminKeys.all });
            toast(t`Роль збережено`);
            await navigate("/admin/roles", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зберегти роль. Спробуйте ще раз.`));
        }
    }

    /**
     * Deletes the role after a confirmation.
     */
    async function remove(): Promise<void> {
        if (!existing || !(await confirm({ title: t`Видалити роль?`, confirmText: t`Видалити`, danger: true }))) {
            return;
        }

        setDeleting(true);
        try {
            await adminApi.deleteRole(existing.id);
            await queryClient.invalidateQueries({ queryKey: adminKeys.all });
            toast(t`Роль видалено`);
            await navigate("/admin/roles", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
        } finally {
            setDeleting(false);
        }
    }

    return (
        <>
            <PageHeader
                title={existing?.name ?? t`Нова роль`}
                subtitle={readOnly ? t`Системний адміністратор завжди має всі дозволи.` : t`Позначте дозволи, які надає роль.`}
                breadcrumbs={[
                    { text: t`Адміністрування`, to: "/admin" },
                    { text: t`Ролі`, to: "/admin/roles" },
                    { text: existing ? t`Налаштування` : t`Нова` },
                ]}
            >
                {existing && (
                    <div className={layout.cluster}>
                        <Badge tone="dashed">
                            <span className={layout.mono}>{existing.code}</span>
                        </Badge>
                        <Badge>{t`Користувачів: ${existing.userCount}`}</Badge>
                    </div>
                )}
            </PageHeader>

            <form className={layout.split} noValidate onSubmit={handleSubmit(save)}>
                <div className={layout.stackLg}>
                    {error && <Alert tone="danger">{error}</Alert>}
                    <Card title={t`Дозволи`}>
                        <fieldset className={styles.fieldset} disabled={readOnly}>
                            <PermissionModules permissions={catalog}>
                                {(permission) => (
                                    <Checkbox
                                        key={permission.code}
                                        checked={readOnly || granted.has(permission.code)}
                                        onChange={(event) => toggle(permission.code, event.target.checked)}
                                        hint={permission.description}
                                    >
                                        {permission.title ?? permission.code}
                                    </Checkbox>
                                )}
                            </PermissionModules>
                        </fieldset>
                    </Card>
                </div>

                <div className={layout.stackLg}>
                    <Card title={t`Роль`}>
                        <div className={layout.stack}>
                            {!existing && (
                                <Field
                                    label={t`Код`}
                                    required
                                    hint={t`Латиниця, цифри, «-». Використовується в імпорті.`}
                                    error={formState.errors.code?.message}
                                >
                                    {(ids) => (
                                        <TextInput
                                            {...ids}
                                            {...register("code")}
                                            className={styles.mono}
                                            maxLength={64}
                                            placeholder="project-manager"
                                        />
                                    )}
                                </Field>
                            )}
                            <Field label={t`Назва`} required error={formState.errors.name?.message}>
                                {(ids) => <TextInput {...ids} {...register("name")} maxLength={128} />}
                            </Field>
                            <Field label={t`Опис`}>
                                {(ids) => <Textarea {...ids} {...register("description")} maxLength={512} rows={3} />}
                            </Field>
                            <Checkbox
                                {...register("isLeadership")}
                                hint={t`Працівників із цією роллю показано першими у складі підрозділу з позначкою «Керівник».`}
                            >
                                {t`Керівна посада`}
                            </Checkbox>
                        </div>
                    </Card>
                    <Card variant="muted">
                        <div className={layout.stack}>
                            <Button type="submit" block icon={Save} busy={formState.isSubmitting} disabled={deleting}>
                                {t`Зберегти`}
                            </Button>
                            {existing && !existing.isSystem && (
                                <Button
                                    variant="danger"
                                    block
                                    icon={Trash2}
                                    busy={deleting}
                                    disabled={formState.isSubmitting || existing.userCount > 0}
                                    onClick={() => void remove()}
                                >
                                    {t`Видалити роль`}
                                </Button>
                            )}
                        </div>
                    </Card>
                </div>
            </form>
        </>
    );
}
