import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Briefcase, Camera, Check, Info, KeyRound, LogOut, Plus, Save, ShieldCheck, Smartphone, Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { PermissionDefinition, RoleOption, UnitOption, UserCredentials, UserDetails } from "@/shared/api/schema";
import { formatDateTime } from "@/shared/i18n/format";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Avatar } from "@/shared/ui/Avatar";
import { Badge } from "@/shared/ui/Badge";
import { Button } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { Checkbox } from "@/shared/ui/Checkbox";
import { confirm } from "@/shared/ui/ConfirmHost";
import { CopyButton } from "@/shared/ui/CopyButton";
import { DescriptionList } from "@/shared/ui/DescriptionList";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Field } from "@/shared/ui/Field";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { adminApi, adminKeys, permissionsQuery, userQuery, userRolesQuery, userUnitsQuery } from "../api";
import { PermissionModules } from "../components/PermissionModules";
import styles from "./admin.module.css";

/**
 * The temporary password of an account just created, kept for the page of that account (shown once, never stored in
 * the history state, so a reload does not show it again).
 */
let createdCredentials: UserCredentials | null = null;

/**
 * Creates an account or changes one: personal data, positions, individual permissions and account security.
 * @returns The page.
 */
export function UserEditorPage() {
    const { t } = useLingui();
    const { id } = useParams();
    const user = useCurrentUser();
    const canEditOverrides = hasPermission(user, Permissions.rolesManage);
    const existing = useQuery({ ...userQuery(id ?? ""), enabled: id !== undefined });
    const units = useQuery(userUnitsQuery(true));
    const roles = useQuery(userRolesQuery);
    const permissions = useQuery({ ...permissionsQuery, enabled: canEditOverrides });

    if (units.isPending || roles.isPending || (canEditOverrides && permissions.isPending) || (id !== undefined && existing.isPending)) {
        return <Spinner block />;
    }

    const error = existing.error ?? units.error ?? roles.error ?? permissions.error;
    if (error) {
        return <Alert tone="danger">{describeError(error, t`Не вдалося завантажити обліковий запис.`)}</Alert>;
    }

    return (
        <UserForm
            key={id ?? "new"}
            existing={existing.data ?? null}
            units={units.data ?? []}
            roles={roles.data ?? []}
            catalog={canEditOverrides ? (permissions.data ?? []) : []}
        />
    );
}

/**
 * A position in the form.
 */
interface PositionRow {
    /** A key of the row (new rows have no unit yet). */
    readonly key: string;
    readonly unitId: string;
    readonly roleId: string;
    readonly title: string;
    readonly isPrimary: boolean;
    /** Whether the administrator manages the position's organization. */
    readonly isManaged: boolean;
    /** The unit, organization and role of a position the administrator does not manage. */
    readonly description?: string;
}

/** The individual setting of a permission. */
type Override = "inherit" | "grant" | "deny";

/**
 * Props of {@link UserForm}.
 */
interface UserFormProps {
    readonly existing: UserDetails | null;
    readonly units: readonly UnitOption[];
    readonly roles: readonly RoleOption[];
    /** The permission catalog; empty when the administrator may not set individual permissions. */
    readonly catalog: readonly PermissionDefinition[];
}

/**
 * The positions of a loaded account as form rows.
 * @param details The account.
 * @returns The rows.
 */
function positionRows(details: UserDetails | null): PositionRow[] {
    return (details?.positions ?? []).map((position, index) => ({
        key: `${index}:${position.unitId}:${position.roleId}`,
        unitId: position.unitId,
        roleId: position.roleId,
        title: position.title ?? "",
        isPrimary: position.isPrimary,
        isManaged: position.isManaged,
        description: `${position.unitName} · ${position.organization.name} · ${position.roleName}`,
    }));
}

/**
 * The individual permissions of a loaded account.
 * @param details The account.
 * @returns The settings by permission code.
 */
function overridesOf(details: UserDetails | null): ReadonlyMap<string, Override> {
    return new Map<string, Override>([
        ...(details?.grantedPermissions ?? []).map((code) => [code, "grant"] as const),
        ...(details?.deniedPermissions ?? []).map((code) => [code, "deny"] as const),
    ]);
}

/**
 * The account form.
 * @param props The account and the choices.
 * @returns The page content.
 */
function UserForm({ existing, units, roles, catalog }: UserFormProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const user = useCurrentUser();
    const canManage = hasPermission(user, Permissions.usersManage);
    const canSecurity = hasPermission(user, Permissions.usersSecurity);
    const canEditAccount = existing?.canEditAccount ?? canManage;
    const canEditPositions = existing?.canEditPositions ?? canManage;
    const [positions, setPositions] = useState<readonly PositionRow[]>(() => positionRows(existing));
    const [overrides, setOverrides] = useState<ReadonlyMap<string, Override>>(() => overridesOf(existing));
    const [credentials, setCredentials] = useState<UserCredentials | null>(() =>
        existing && createdCredentials?.userId === existing.id ? createdCredentials : null,
    );
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);
    const primaryIsForeign = positions.some((position) => !position.isManaged && position.isPrimary);
    const effective = new Set(existing?.effectivePermissions ?? []);

    // The password of a just created account is shown on its page once.
    useEffect(() => {
        createdCredentials = null;
    }, []);

    const schema = z.object({
        lastName: z
            .string()
            .trim()
            .min(1, t`Вкажіть прізвище.`)
            .max(64),
        firstName: z
            .string()
            .trim()
            .min(1, t`Вкажіть ім'я.`)
            .max(64),
        middleName: z.string().trim().max(64),
        phoneNumber: z.string().trim().max(32),
        email: z
            .string()
            .trim()
            .min(1, t`Вкажіть електронну пошту.`)
            .max(256)
            .pipe(z.email(t`Некоректна адреса.`)),
    });

    const { register, handleSubmit, formState, reset } = useForm({
        resolver: zodResolver(schema),
        defaultValues: {
            lastName: existing?.lastName ?? "",
            firstName: existing?.firstName ?? "",
            middleName: existing?.middleName ?? "",
            phoneNumber: existing?.phoneNumber ?? "",
            email: existing?.email ?? "",
        },
    });

    /**
     * Changes a position.
     * @param key The row.
     * @param change The changed fields.
     */
    function updatePosition(key: string, change: Partial<PositionRow>): void {
        setPositions((rows) => rows.map((row) => (row.key === key ? { ...row, ...change } : row)));
    }

    /**
     * Makes a position the main one.
     * @param key The row.
     */
    function setPrimary(key: string): void {
        setPositions((rows) => rows.map((row) => ({ ...row, isPrimary: row.key === key })));
    }

    /**
     * Sets the individual setting of a permission.
     * @param code The permission.
     * @param value The setting.
     */
    function setOverride(code: string, value: Override): void {
        setOverrides((current) => {
            const next = new Map(current);
            if (value === "inherit") {
                next.delete(code);
            } else {
                next.set(code, value);
            }

            return next;
        });
    }

    /**
     * Loads the account again after a change.
     * @returns A promise that completes when reloaded.
     */
    async function reload(): Promise<void> {
        await queryClient.invalidateQueries({ queryKey: adminKeys.all });
    }

    /**
     * Runs an action and reports its outcome.
     * @param action The action; returns the success message.
     */
    async function run(action: () => Promise<string>): Promise<void> {
        setBusy(true);
        setMessage(null);
        setError(null);
        try {
            setMessage(await action());
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
        } finally {
            setBusy(false);
        }
    }

    /**
     * Saves the account.
     * @param values The personal data.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        if (positions.some((position) => !position.unitId || !position.roleId)) {
            setError(t`Для кожної посади оберіть підрозділ і роль.`);
            return;
        }

        const command = {
            email: values.email,
            lastName: values.lastName,
            firstName: values.firstName,
            middleName: values.middleName || null,
            phoneNumber: values.phoneNumber || null,
            positions: positions.map((position) => ({
                unitId: position.unitId,
                roleId: position.roleId,
                title: position.title.trim() || null,
                isPrimary: position.isPrimary,
            })),
            grantedPermissions: [...overrides].filter(([, value]) => value === "grant").map(([code]) => code),
            deniedPermissions: [...overrides].filter(([, value]) => value === "deny").map(([code]) => code),
        };

        await run(async () => {
            if (existing) {
                await adminApi.updateUser(existing.id, command);
                reset(values);
                await reload();
                return t`Зміни збережено.`;
            }

            const created = await adminApi.createUser(command);
            createdCredentials = created;
            await reload();
            toast(t`Обліковий запис створено.`);
            await navigate(`/admin/users/${created.userId}`, { replace: true });
            return t`Обліковий запис створено.`;
        });
    }

    /**
     * Blocks or unblocks the account after a confirmation.
     */
    async function toggleActive(): Promise<void> {
        if (!existing) {
            return;
        }

        const activate = !existing.isActive;
        const question = activate ? t`Розблокувати обліковий запис?` : t`Заблокувати обліковий запис? Усі сесії буде завершено.`;
        if (!(await confirm({ title: question, confirmText: activate ? t`Розблокувати` : t`Заблокувати`, danger: !activate }))) {
            return;
        }

        await run(async () => {
            await adminApi.setUserActive(existing.id, activate);
            await reload();
            return activate ? t`Обліковий запис розблоковано.` : t`Обліковий запис заблоковано.`;
        });
    }

    /**
     * Issues a new temporary password after a confirmation.
     */
    async function resetPassword(): Promise<void> {
        const question = t`Видати новий тимчасовий пароль? Поточний пароль перестане діяти, а сесії буде завершено.`;
        if (!existing || !(await confirm({ title: question, confirmText: t`Видати тимчасовий пароль` }))) {
            return;
        }

        await run(async () => {
            setCredentials(await adminApi.resetPassword(existing.id));
            await reload();
            return t`Тимчасовий пароль видано.`;
        });
    }

    /**
     * Resets the authenticator after a confirmation.
     */
    async function resetAuthenticator(): Promise<void> {
        const question = t`Скинути автентифікатор? Під час наступного входу користувач прив'яже новий.`;
        if (!existing || !(await confirm({ title: question, confirmText: t`Скинути автентифікатор` }))) {
            return;
        }

        await run(async () => {
            await adminApi.resetAuthenticator(existing.id);
            await reload();
            return t`Автентифікатор скинуто.`;
        });
    }

    /**
     * Ends every session of the user after a confirmation.
     */
    async function revokeSessions(): Promise<void> {
        if (!existing || !(await confirm({ title: t`Завершити всі сесії користувача?`, confirmText: t`Завершити всі сесії` }))) {
            return;
        }

        await run(async () => {
            await adminApi.revokeSessions(existing.id);
            await reload();
            return t`Сесії завершено.`;
        });
    }

    const unitOptions = [
        { value: "", label: t`Оберіть…` },
        ...units.map((unit) => ({ value: unit.id, label: unit.name, depth: unit.depth })),
    ];
    const roleOptions = [{ value: "", label: t`Оберіть…` }, ...roles.map((role) => ({ value: role.id, label: role.name }))];

    return (
        <>
            <PageHeader
                title={existing?.fullName ?? t`Новий користувач`}
                subtitle={
                    existing
                        ? existing.email
                        : t`Обліковий запис створюється з тимчасовим паролем; під час першого входу користувач змінить його та прив'яже автентифікатор.`
                }
                breadcrumbs={[
                    { text: t`Адміністрування`, to: "/admin" },
                    { text: t`Користувачі`, to: "/admin/users" },
                    { text: existing ? t`Обліковий запис` : t`Новий` },
                ]}
            >
                {existing && (
                    <div className={layout.cluster}>
                        <Avatar name={existing.fullName} photoId={existing.photoId} />
                        {existing.photoId === null && (
                            <Badge tone="dashed" icon={Camera}>
                                {t`Без фото`}
                            </Badge>
                        )}
                        {existing.isActive ? (
                            <Badge tone="inverse">{t`Активний`}</Badge>
                        ) : (
                            <Badge tone="danger" icon={Ban}>
                                {t`Заблоковано`}
                            </Badge>
                        )}
                        <Badge tone={existing.twoFactorEnabled ? "outline" : "dashed"} icon={ShieldCheck}>
                            {existing.twoFactorEnabled ? t`2FA увімкнено` : t`2FA не налаштовано`}
                        </Badge>
                        {existing.mustChangePassword && (
                            <Badge tone="dashed" icon={KeyRound}>
                                {t`Тимчасовий пароль`}
                            </Badge>
                        )}
                    </div>
                )}
            </PageHeader>

            <div className={layout.stackLg}>
                {credentials && (
                    <div className={styles.secret}>
                        <span className={layout.strong}>{t`Тимчасовий пароль для ${credentials.email} (показується один раз):`}</span>
                        <span className={styles.secretValue}>{credentials.temporaryPassword}</span>
                        <div className={layout.cluster}>
                            <CopyButton
                                size="sm"
                                value={`${t`Логін: ${credentials.email}`}\n${t`Тимчасовий пароль: ${credentials.temporaryPassword}`}`}
                            >
                                {t`Скопіювати`}
                            </CopyButton>
                            <span className={[layout.textSm, layout.muted].join(" ")}>
                                {t`Передайте його користувачу захищеним каналом. Під час першого входу пароль потрібно змінити.`}
                            </span>
                        </div>
                    </div>
                )}
                {message && <Alert tone="inverse">{message}</Alert>}
                {error && <Alert tone="danger">{error}</Alert>}
                {canEditPositions && !canEditAccount && (
                    <Alert tone="outline" icon={Info} title={t`Обліковий запис кількох організацій`}>
                        {t`Користувач також обіймає посади в організаціях, якими ви не керуєте. Ви можете змінювати лише його посади у своїх організаціях; особисті дані, e-mail, статус і безпеку облікового запису змінює адміністратор, що керує всіма його організаціями.`}
                    </Alert>
                )}

                <form className={layout.split} noValidate onSubmit={handleSubmit(save)}>
                    <div className={layout.stackLg}>
                        <Card title={t`Особисті дані`}>
                            <fieldset className={styles.formGrid} disabled={!canEditAccount}>
                                <Field label={t`Прізвище`} required error={formState.errors.lastName?.message}>
                                    {(ids) => <TextInput {...ids} {...register("lastName")} maxLength={64} />}
                                </Field>
                                <Field label={t`Ім'я`} required error={formState.errors.firstName?.message}>
                                    {(ids) => <TextInput {...ids} {...register("firstName")} maxLength={64} />}
                                </Field>
                                <Field label={t`По батькові`}>
                                    {(ids) => <TextInput {...ids} {...register("middleName")} maxLength={64} />}
                                </Field>
                                <Field label={t`Телефон`}>
                                    {(ids) => (
                                        <TextInput {...ids} {...register("phoneNumber")} type="tel" maxLength={32} placeholder="+380…" />
                                    )}
                                </Field>
                                <Field
                                    label={t`Електронна пошта (логін)`}
                                    required
                                    error={formState.errors.email?.message}
                                    className={styles.full}
                                >
                                    {(ids) => <TextInput {...ids} {...register("email")} type="email" inputMode="email" maxLength={256} />}
                                </Field>
                            </fieldset>
                        </Card>

                        <Card
                            title={t`Посади`}
                            subtitle={t`Роль посади визначає дозволи; підрозділ — область доступу до документів і організацію, у якій діють адміністративні дозволи ролі.`}
                            actions={
                                canEditPositions && (
                                    <Button
                                        variant="secondary"
                                        size="sm"
                                        icon={Plus}
                                        onClick={() =>
                                            setPositions((rows) => [
                                                ...rows,
                                                {
                                                    key: crypto.randomUUID(),
                                                    unitId: "",
                                                    roleId: "",
                                                    title: "",
                                                    isPrimary: rows.length === 0,
                                                    isManaged: true,
                                                },
                                            ])
                                        }
                                    >
                                        {t`Додати посаду`}
                                    </Button>
                                )
                            }
                        >
                            {positions.length === 0 ? (
                                <EmptyState
                                    icon={Briefcase}
                                    title={t`Посад не призначено`}
                                    text={t`Без посади користувач не матиме доступу до модулів.`}
                                />
                            ) : (
                                <div className={styles.positions}>
                                    {positions
                                        .filter((position) => !position.isManaged)
                                        .map((position) => (
                                            <div key={position.key} className={styles.foreign}>
                                                <span className={layout.stackSm}>
                                                    <span className={layout.strong}>{position.title || "—"}</span>
                                                    <span className={[layout.textSm, layout.muted].join(" ")}>{position.description}</span>
                                                    <span className={[layout.textXs, layout.muted].join(" ")}>
                                                        {t`Посада в іншій організації. Змінює адміністратор цієї організації.`}
                                                    </span>
                                                </span>
                                                {position.isPrimary && <Badge tone="outline">{t`Основна`}</Badge>}
                                            </div>
                                        ))}
                                    {positions
                                        .filter((position) => position.isManaged)
                                        .map((position) => (
                                            <fieldset
                                                key={position.key}
                                                className={[styles.fieldset, styles.position].join(" ")}
                                                disabled={!canEditPositions}
                                            >
                                                <Field label={t`Підрозділ`}>
                                                    {(ids) => (
                                                        <Select
                                                            {...ids}
                                                            value={position.unitId}
                                                            disabled={!canEditPositions}
                                                            onChange={(value) => updatePosition(position.key, { unitId: value })}
                                                            options={unitOptions}
                                                        />
                                                    )}
                                                </Field>
                                                <Field label={t`Роль`}>
                                                    {(ids) => (
                                                        <Select
                                                            {...ids}
                                                            value={position.roleId}
                                                            disabled={!canEditPositions}
                                                            onChange={(value) => updatePosition(position.key, { roleId: value })}
                                                            options={roleOptions}
                                                        />
                                                    )}
                                                </Field>
                                                <Field label={t`Назва посади`}>
                                                    {(ids) => (
                                                        <TextInput
                                                            {...ids}
                                                            value={position.title}
                                                            maxLength={128}
                                                            placeholder={t`напр. доцент`}
                                                            onChange={(event) =>
                                                                updatePosition(position.key, { title: event.target.value })
                                                            }
                                                        />
                                                    )}
                                                </Field>
                                                <div className={styles.primaryCheck}>
                                                    <Checkbox
                                                        radio
                                                        name="primary-position"
                                                        checked={position.isPrimary}
                                                        disabled={primaryIsForeign}
                                                        onChange={() => setPrimary(position.key)}
                                                    >
                                                        {t`Основна`}
                                                    </Checkbox>
                                                </div>
                                                {canEditPositions && (
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        iconOnly
                                                        icon={Trash2}
                                                        onClick={() =>
                                                            setPositions((rows) => rows.filter((row) => row.key !== position.key))
                                                        }
                                                    >
                                                        {t`Прибрати посаду`}
                                                    </Button>
                                                )}
                                            </fieldset>
                                        ))}
                                </div>
                            )}
                        </Card>

                        {catalog.length > 0 && canEditAccount && (
                            <Card title={t`Індивідуальні дозволи`} subtitle={t`Надати або заборонити окремі дозволи поверх ролей посад.`}>
                                <PermissionModules permissions={catalog}>
                                    {(permission) => (
                                        <Field
                                            key={permission.code}
                                            label={permission.title ?? permission.code}
                                            hint={effective.has(permission.code) ? t`діє зараз` : undefined}
                                        >
                                            {(ids) => (
                                                <Select
                                                    {...ids}
                                                    compact
                                                    value={overrides.get(permission.code) ?? "inherit"}
                                                    onChange={(value) => setOverride(permission.code, value as Override)}
                                                    options={[
                                                        { value: "inherit", label: t`За ролями` },
                                                        { value: "grant", label: t`Надати` },
                                                        { value: "deny", label: t`Заборонити` },
                                                    ]}
                                                />
                                            )}
                                        </Field>
                                    )}
                                </PermissionModules>
                            </Card>
                        )}
                    </div>

                    <div className={layout.stackLg}>
                        {(canEditAccount || canEditPositions) && (
                            <Card variant="muted">
                                <Button type="submit" block icon={Save} busy={busy}>
                                    {existing ? t`Зберегти зміни` : t`Створити обліковий запис`}
                                </Button>
                            </Card>
                        )}

                        {existing && (
                            <Card title={t`Безпека облікового запису`}>
                                <div className={layout.stack}>
                                    {canManage && existing.canEditAccount && (
                                        <Button
                                            variant={existing.isActive ? "danger" : "primary"}
                                            block
                                            icon={existing.isActive ? Ban : Check}
                                            disabled={busy}
                                            onClick={() => void toggleActive()}
                                        >
                                            {existing.isActive ? t`Заблокувати` : t`Розблокувати`}
                                        </Button>
                                    )}
                                    {canSecurity && existing.canManageSecurity && (
                                        <>
                                            <Button
                                                variant="secondary"
                                                block
                                                icon={KeyRound}
                                                disabled={busy}
                                                onClick={() => void resetPassword()}
                                            >
                                                {t`Видати тимчасовий пароль`}
                                            </Button>
                                            <Button
                                                variant="secondary"
                                                block
                                                icon={Smartphone}
                                                disabled={busy}
                                                onClick={() => void resetAuthenticator()}
                                            >
                                                {t`Скинути автентифікатор`}
                                            </Button>
                                            <Button
                                                variant="secondary"
                                                block
                                                icon={LogOut}
                                                disabled={busy}
                                                onClick={() => void revokeSessions()}
                                            >
                                                {t`Завершити всі сесії`}
                                            </Button>
                                        </>
                                    )}
                                </div>
                                <DescriptionList
                                    items={[
                                        [t`Створено`, formatDateTime(existing.createdAt)],
                                        [t`Останній вхід`, formatDateTime(existing.lastSignInAt)],
                                    ]}
                                />
                            </Card>
                        )}
                    </div>
                </form>
            </div>
        </>
    );
}
