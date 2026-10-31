import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Plus, Settings2, Shield } from "lucide-react";
import { describeError } from "@/shared/api/problem";
import type { RoleListItem } from "@/shared/api/schema";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { DataGrid, GridPrimary, type GridColumn } from "@/shared/ui/DataGrid";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Spinner } from "@/shared/ui/Spinner";
import { roleListQuery } from "../api";
import { useListParams } from "../components/useListParams";
import styles from "./admin.module.css";

/**
 * The roles: sets of permissions for positions.
 * @returns The page.
 */
export function RolesPage() {
    const { t } = useLingui();
    const { list, setFilter } = useListParams();
    const roles = useQuery(roleListQuery(list));

    const columns: GridColumn<RoleListItem>[] = [
        {
            key: "name",
            title: t`Роль`,
            sortKey: "name",
            primary: true,
            render: (role) => (
                <GridPrimary to={`/admin/roles/${role.id}`} secondary={role.description}>
                    {role.name}
                </GridPrimary>
            ),
        },
        { key: "code", title: t`Код`, sortKey: "code", render: (role) => <span className={layout.mono}>{role.code}</span> },
        {
            key: "kind",
            title: t`Тип`,
            render: (role) => (
                <span className={layout.cluster}>
                    {role.isSystem ? <Badge tone="outline">{t`Системна`}</Badge> : <Badge tone="dashed">{t`Власна`}</Badge>}
                    {role.isLeadership && <Badge>{t`Керівна`}</Badge>}
                </span>
            ),
        },
        { key: "permissions", title: t`Дозволи`, numeric: true, render: (role) => role.permissionCount },
        { key: "users", title: t`Користувачі`, numeric: true, render: (role) => role.userCount },
        {
            key: "actions",
            title: t`Дії`,
            actions: true,
            render: (role) => (
                <ButtonLink to={`/admin/roles/${role.id}`} variant="ghost" size="sm" icon={Settings2}>
                    {t`Налаштувати`}
                </ButtonLink>
            ),
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Ролі та дозволи`}
                subtitle={t`Роль — набір дозволів для посади. Користувач отримує дозволи всіх своїх посад.`}
                breadcrumbs={[{ text: t`Адміністрування`, to: "/admin" }, { text: t`Ролі` }]}
                actions={
                    <ButtonLink to="/admin/roles/new" icon={Plus}>
                        {t`Нова роль`}
                    </ButtonLink>
                }
            />

            <div className={styles.toolbar}>
                <SearchInput
                    value={list.search ?? ""}
                    placeholder={t`Назва або код ролі`}
                    onSearch={(value) => setFilter("search", value)}
                />
            </div>

            {roles.isPending && <Spinner block />}
            {roles.isError && <Alert tone="danger">{describeError(roles.error, t`Не вдалося завантажити ролі.`)}</Alert>}
            {roles.data && (
                <DataGrid
                    columns={columns}
                    result={roles.data}
                    rowKey={(role) => role.id}
                    caption={t`Ролі`}
                    emptyIcon={Shield}
                    emptyTitle={t`Ролей не знайдено`}
                />
            )}
        </>
    );
}
