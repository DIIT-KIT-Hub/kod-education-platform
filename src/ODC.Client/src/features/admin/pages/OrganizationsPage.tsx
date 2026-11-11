import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Building2, Info, KeyRound, Pause, Play, Plus, Trash2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { describeError } from "@/shared/api/problem";
import type { OrganizationListItem } from "@/shared/api/schema";
import { formatDateTime } from "@/shared/i18n/format";
import { organizationStateNames } from "@/shared/i18n/display";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { confirm } from "@/shared/ui/ConfirmHost";
import { DataGrid, type GridColumn } from "@/shared/ui/DataGrid";
import { Dialog } from "@/shared/ui/Dialog";
import { Field } from "@/shared/ui/Field";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SkeletonTable } from "@/shared/ui/Skeleton";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { adminApi, adminKeys, organizationsQuery } from "../api";

/**
 * The registry of organizations for a platform administrator: every organization with its own database, its state and
 * schema; suspend, resume, new database passwords and deletion. A new organization is a new top-level unit of the
 * structure.
 * @returns The page.
 */
export function OrganizationsPage() {
    const { t, i18n } = useLingui();
    const queryClient = useQueryClient();
    const organizations = useQuery(organizationsQuery);
    const [busy, setBusy] = useState<string | null>(null);
    const [deleting, setDeleting] = useState<OrganizationListItem | null>(null);

    /**
     * Runs an action on an organization, reports its outcome and reloads the registry.
     * @param id The organization.
     * @param action The action; returns the success message.
     */
    async function run(id: string, action: () => Promise<string>): Promise<void> {
        setBusy(id);
        try {
            toast(await action());
            await queryClient.invalidateQueries({ queryKey: adminKeys.organizations });
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`), { tone: "error" });
        } finally {
            setBusy(null);
        }
    }

    /**
     * Suspends an organization after a confirmation.
     * @param organization The organization.
     */
    async function suspend(organization: OrganizationListItem): Promise<void> {
        const question = t`Призупинити «${organization.name}»? Організація перестане відповідати на запити, а люди, які працюють лише в ній, вийдуть з системи. Дані збережуться.`;
        if (await confirm({ title: question, confirmText: t`Призупинити`, danger: true })) {
            await run(organization.id, async () => {
                await adminApi.suspendOrganization(organization.id);
                return t`Організацію «${organization.name}» призупинено.`;
            });
        }
    }

    /**
     * Opens a suspended organization again.
     * @param organization The organization.
     */
    async function resume(organization: OrganizationListItem): Promise<void> {
        await run(organization.id, async () => {
            await adminApi.resumeOrganization(organization.id);
            return t`Організація «${organization.name}» знову працює.`;
        });
    }

    /**
     * Gives the database roles of an organization new passwords after a confirmation.
     * @param organization The organization.
     */
    async function rotate(organization: OrganizationListItem): Promise<void> {
        const question = t`Змінити паролі бази «${organization.name}»? Застосунок одразу почне користуватися новими.`;
        if (await confirm({ title: question, confirmText: t`Змінити паролі` })) {
            await run(organization.id, async () => {
                await adminApi.rotateOrganizationPasswords(organization.id);
                return t`Паролі бази «${organization.name}» змінено.`;
            });
        }
    }

    const columns: GridColumn<OrganizationListItem>[] = [
        {
            key: "name",
            title: t`Організація`,
            primary: true,
            render: (organization) => (
                <span className={layout.stackSm}>
                    <strong>{organization.name}</strong>
                    {organization.shortName && <span className={[layout.textSm, layout.muted].join(" ")}>{organization.shortName}</span>}
                </span>
            ),
        },
        { key: "code", title: t`Код`, render: (organization) => <span className={layout.mono}>{organization.code}</span> },
        {
            key: "state",
            title: t`Стан`,
            render: (organization) => (
                <span className={layout.row}>
                    <Badge tone={organization.state === "Active" ? "inverse" : organization.state === "Suspended" ? "outline" : "dashed"}>
                        {i18n._(organizationStateNames[organization.state])}
                    </Badge>
                    {organization.state === "Active" && !organization.isCurrent && <Badge tone="danger">{t`База не оновлена`}</Badge>}
                </span>
            ),
        },
        {
            key: "database",
            title: t`База даних`,
            render: (organization) => (
                <span className={layout.stackSm}>
                    <span className={layout.mono}>{organization.databaseName}</span>
                    <span className={[layout.textSm, layout.muted].join(" ")}>
                        {organization.ownRoles ? t`Власні ролі PostgreSQL` : t`Обліковий запис майстер-бази`}
                        {organization.migratedAt && ` · ${t`оновлено ${formatDateTime(organization.migratedAt)}`}`}
                    </span>
                </span>
            ),
        },
        { key: "members", title: t`Працівників`, numeric: true, render: (organization) => organization.memberCount },
        {
            key: "actions",
            title: t`Дії`,
            actions: true,
            render: (organization) => (
                <span className={layout.cluster}>
                    {organization.state === "Active" && (
                        <Button
                            variant="ghost"
                            size="sm"
                            icon={Pause}
                            busy={busy === organization.id}
                            onClick={() => void suspend(organization)}
                        >
                            {t`Призупинити`}
                        </Button>
                    )}
                    {organization.state === "Suspended" && (
                        <>
                            <Button
                                variant="ghost"
                                size="sm"
                                icon={Play}
                                busy={busy === organization.id}
                                onClick={() => void resume(organization)}
                            >
                                {t`Відновити`}
                            </Button>
                            <Button
                                variant="ghost"
                                size="sm"
                                icon={Trash2}
                                disabled={busy !== null}
                                onClick={() => setDeleting(organization)}
                            >
                                {t`Видалити`}
                            </Button>
                        </>
                    )}
                    {organization.ownRoles && organization.state !== "Deleting" && (
                        <Button
                            variant="ghost"
                            size="sm"
                            icon={KeyRound}
                            disabled={busy !== null}
                            onClick={() => void rotate(organization)}
                        >
                            {t`Нові паролі бази`}
                        </Button>
                    )}
                </span>
            ),
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Організації`}
                subtitle={t`Кожна організація має власну базу даних, ролі PostgreSQL і ключ шифрування файлів.`}
                breadcrumbs={[{ text: t`Адміністрування`, to: "/admin" }, { text: t`Організації` }]}
                actions={
                    <ButtonLink to="/admin/units/new" icon={Plus}>
                        {t`Нова організація`}
                    </ButtonLink>
                }
            />

            <div className={layout.stackLg}>
                <Alert tone="outline" icon={Info}>
                    {t`Нова організація — це новий підрозділ верхнього рівня з типом «організація» у структурі. Видалити можна лише призупинену організацію: разом з нею назавжди зникнуть її база, файли та ключ. Резервні копії робить оператор сервера.`}
                </Alert>
                {organizations.isPending && <SkeletonTable columns={5} />}
                {organizations.data && (
                    <DataGrid
                        columns={columns}
                        result={{
                            items: organizations.data,
                            page: 1,
                            pageSize: Math.max(organizations.data.length, 1),
                            totalCount: organizations.data.length,
                        }}
                        paged={false}
                        rowKey={(organization) => organization.id}
                        caption={t`Організації`}
                        emptyIcon={Building2}
                        emptyTitle={t`Організацій ще немає`}
                        emptyText={t`Створіть підрозділ верхнього рівня з типом «організація».`}
                    />
                )}
            </div>

            <DeleteOrganizationDialog
                organization={deleting}
                onClose={() => setDeleting(null)}
                onDeleted={async () => {
                    setDeleting(null);
                    await queryClient.invalidateQueries({ queryKey: adminKeys.organizations });
                }}
            />
        </>
    );
}

/**
 * Props of {@link DeleteOrganizationDialog}.
 */
interface DeleteOrganizationDialogProps {
    /** The organization to delete (null: closed). */
    readonly organization: OrganizationListItem | null;
    /** Closes the dialog. */
    readonly onClose: () => void;
    /** Called after the deletion. */
    readonly onDeleted: () => Promise<void>;
}

/**
 * Deletes a suspended organization after its code is typed again.
 * @param props The props.
 * @returns The dialog.
 */
function DeleteOrganizationDialog({ organization, onClose, onDeleted }: DeleteOrganizationDialogProps) {
    const { t } = useLingui();
    const [code, setCode] = useState("");
    const [busy, setBusy] = useState(false);
    const matches = organization !== null && code.trim().toUpperCase() === organization.code.toUpperCase();

    /**
     * Closes the dialog and forgets the typed code.
     */
    function close(): void {
        setCode("");
        onClose();
    }

    /**
     * Deletes the organization.
     * @param event The submit event.
     */
    async function submit(event: FormEvent): Promise<void> {
        event.preventDefault();
        if (!organization || !matches) {
            return;
        }

        setBusy(true);
        try {
            await adminApi.deleteOrganization(organization.id, code.trim());
            toast(t`Організацію «${organization.name}» видалено.`);
            setCode("");
            await onDeleted();
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося видалити організацію.`), { tone: "error" });
        } finally {
            setBusy(false);
        }
    }

    return (
        <Dialog
            open={organization !== null}
            onClose={close}
            locked={busy}
            title={t`Видалити організацію назавжди?`}
            description={organization?.name}
        >
            <form className={layout.stack} noValidate onSubmit={(event) => void submit(event)}>
                <p>
                    {t`Буде видалено базу даних організації, її ролі PostgreSQL, файли та ключ шифрування, а також членства людей у ній. Повернути їх можна лише з резервної копії оператора.`}
                </p>
                <Field label={t`Введіть код організації (${organization?.code ?? ""})`}>
                    {(ids) => <TextInput {...ids} value={code} onChange={(event) => setCode(event.target.value)} autoComplete="off" />}
                </Field>
                <div className={layout.cluster}>
                    <Button variant="secondary" onClick={close} disabled={busy}>
                        {t`Скасувати`}
                    </Button>
                    <Button type="submit" variant="danger" icon={Trash2} busy={busy} disabled={!matches}>
                        {t`Видалити назавжди`}
                    </Button>
                </div>
            </form>
        </Dialog>
    );
}
