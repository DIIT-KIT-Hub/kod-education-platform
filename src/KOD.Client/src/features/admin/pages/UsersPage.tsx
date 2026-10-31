import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, Ban, Download, Timer, Upload, UserPlus, Users } from "lucide-react";
import { Link } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { SpreadsheetFormat, UserListItem, UserStatusFilter } from "@/shared/api/schema";
import { userStatusNames } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Avatar } from "@/shared/ui/Avatar";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { DataGrid, type GridColumn } from "@/shared/ui/DataGrid";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { userListQuery, userRolesQuery, userUnitsQuery, type UserFilters } from "../api";
import { useListParams } from "../components/useListParams";
import styles from "./admin.module.css";

/** The account states, in the order of the filter. */
const statuses = Object.keys(userStatusNames) as UserStatusFilter[];

/**
 * The address of the user export with the current filters.
 * @param filters The filters.
 * @param format The file format.
 * @returns The address.
 */
function exportHref(filters: UserFilters, format: SpreadsheetFormat): string {
    const query = new URLSearchParams({ format, Status: filters.status });
    for (const [name, value] of [
        ["Search", filters.search],
        ["UnitId", filters.unitId],
        ["RoleId", filters.roleId],
    ] as const) {
        if (value) {
            query.set(name, value);
        }
    }

    return `/api/v1/user-exports?${query.toString()}`;
}

/**
 * The user accounts: search, status, unit and role filters, export.
 * @returns The page.
 */
export function UsersPage() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const { list, get, setFilter } = useListParams();
    const filters: UserFilters = {
        ...list,
        status: statuses.find((status) => status === get("status")) ?? "All",
        unitId: get("unit"),
        roleId: get("role"),
    };
    const users = useQuery(userListQuery(filters));
    const units = useQuery(userUnitsQuery(false));
    const roles = useQuery(userRolesQuery);

    const columns: GridColumn<UserListItem>[] = [
        {
            key: "name",
            title: t`Користувач`,
            sortKey: "name",
            primary: true,
            render: (item) => (
                <span className={layout.row}>
                    <Avatar name={item.fullName} photoId={item.photoId} size="sm" decorative />
                    <span className={layout.stackSm}>
                        <Link className={layout.strong} to={`/admin/users/${item.id}`}>
                            {item.fullName}
                        </Link>
                        <span className={[layout.textSm, layout.muted].join(" ")}>{item.email}</span>
                    </span>
                </span>
            ),
        },
        {
            key: "position",
            title: t`Посада`,
            render: (item) => (
                <span className={layout.stackSm}>
                    <span>{item.position ?? "—"}</span>
                    {item.unit && <span className={[layout.textSm, layout.muted].join(" ")}>{item.unit}</span>}
                </span>
            ),
        },
        {
            key: "status",
            title: t`Статус`,
            render: (item) => (
                <span className={layout.cluster}>
                    {!item.isActive ? (
                        <Badge tone="danger" icon={Ban}>
                            {t`Заблоковано`}
                        </Badge>
                    ) : item.isLockedOut ? (
                        <Badge tone="outline" icon={Timer}>
                            {t`Тимчасово заблоковано`}
                        </Badge>
                    ) : (
                        <Badge tone="inverse">{t`Активний`}</Badge>
                    )}
                    {!item.twoFactorEnabled && (
                        <span title={t`Автентифікатор ще не прив'язано`}>
                            <Badge tone="dashed">{t`Без 2FA`}</Badge>
                        </span>
                    )}
                </span>
            ),
        },
        {
            key: "lastSignIn",
            title: t`Останній вхід`,
            sortKey: "lastSignIn",
            nowrap: true,
            render: (item) => formatDateTime(item.lastSignInAt),
        },
        {
            key: "actions",
            title: t`Дії`,
            actions: true,
            render: (item) => (
                <ButtonLink to={`/admin/users/${item.id}`} variant="ghost" size="sm" iconEnd={ArrowRight}>
                    {t`Відкрити`}
                </ButtonLink>
            ),
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Користувачі`}
                subtitle={
                    hasPermission(user, Permissions.administrationAll)
                        ? t`Облікові записи працівників і зовнішніх користувачів усіх організацій кабінету. Самостійна реєстрація вимкнена.`
                        : t`Облікові записи працівників і зовнішніх користувачів ваших організацій. Самостійна реєстрація вимкнена.`
                }
                breadcrumbs={[{ text: t`Адміністрування`, to: "/admin" }, { text: t`Користувачі` }]}
                actions={
                    <>
                        {hasPermission(user, Permissions.usersExport) && (
                            <>
                                <ButtonLink href={exportHref(filters, "Xlsx")} download="" variant="secondary" icon={Download}>
                                    XLSX
                                </ButtonLink>
                                <ButtonLink href={exportHref(filters, "Csv")} download="" variant="secondary" icon={Download}>
                                    CSV
                                </ButtonLink>
                            </>
                        )}
                        {hasPermission(user, Permissions.usersImport) && (
                            <ButtonLink to="/admin/users/import" variant="secondary" icon={Upload}>
                                {t`Імпорт`}
                            </ButtonLink>
                        )}
                        {hasPermission(user, Permissions.usersManage) && (
                            <ButtonLink to="/admin/users/new" icon={UserPlus}>
                                {t`Додати`}
                            </ButtonLink>
                        )}
                    </>
                }
            />

            <div className={styles.toolbar}>
                <SearchInput value={list.search ?? ""} placeholder={t`ПІБ або e-mail`} onSearch={(value) => setFilter("search", value)} />
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Статус`}
                        value={filters.status}
                        onChange={(value) => setFilter("status", value === "All" ? undefined : value)}
                        options={statuses.map((status) => ({ value: status, label: t(userStatusNames[status]) }))}
                    />
                </div>
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Підрозділ`}
                        value={filters.unitId ?? ""}
                        onChange={(value) => setFilter("unit", value)}
                        options={[
                            { value: "", label: t`Усі підрозділи` },
                            ...(units.data ?? []).map((unit) => ({ value: unit.id, label: unit.name, depth: unit.depth })),
                        ]}
                    />
                </div>
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Роль`}
                        value={filters.roleId ?? ""}
                        onChange={(value) => setFilter("role", value)}
                        options={[
                            { value: "", label: t`Усі ролі` },
                            ...(roles.data ?? []).map((role) => ({ value: role.id, label: role.name })),
                        ]}
                    />
                </div>
            </div>

            {users.isPending && <Spinner block />}
            {users.isError && <Alert tone="danger">{describeError(users.error, t`Не вдалося завантажити користувачів.`)}</Alert>}
            {users.data && (
                <DataGrid
                    columns={columns}
                    result={users.data}
                    rowKey={(item) => item.id}
                    caption={t`Користувачі`}
                    emptyIcon={Users}
                    emptyTitle={t`Користувачів не знайдено`}
                />
            )}
        </>
    );
}
