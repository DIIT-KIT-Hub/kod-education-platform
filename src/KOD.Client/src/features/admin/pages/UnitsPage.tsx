import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Building2, Layers, Network, Pencil, Plus } from "lucide-react";
import { describeError } from "@/shared/api/problem";
import type { UnitListItem } from "@/shared/api/schema";
import { unitTypeIcon } from "@/shared/lib/unitTypeIcons";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { DataGrid, GridPrimary, type GridColumn } from "@/shared/ui/DataGrid";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { unitListQuery, unitOptionsQuery, unitTypesQuery } from "../api";
import { useListParams } from "../components/useListParams";
import styles from "./admin.module.css";

/**
 * The units the user manages: search, organization and type filters, sorting and pages.
 * @returns The page.
 */
export function UnitsPage() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const cabinetWide = hasPermission(user, Permissions.administrationAll);
    const { list, get, setFilter } = useListParams();
    const typeId = get("type");
    const organizationId = get("organization");
    const units = useQuery(unitListQuery({ ...list, typeId, organizationId }));
    const types = useQuery(unitTypesQuery);
    const options = useQuery(unitOptionsQuery);
    const organizations = (options.data ?? []).filter((unit) => unit.depth === 0);

    const columns: GridColumn<UnitListItem>[] = [
        {
            key: "name",
            title: t`Назва`,
            sortKey: "name",
            primary: true,
            render: (unit) => (
                <GridPrimary to={`/admin/units/${unit.id}`} secondary={unit.parentName ?? t`Організація верхнього рівня`}>
                    {unit.name}
                </GridPrimary>
            ),
        },
        { key: "code", title: t`Код`, sortKey: "code", render: (unit) => <span className={layout.mono}>{unit.code}</span> },
        {
            key: "type",
            title: t`Тип`,
            sortKey: "type",
            render: (unit) => (
                <span className={layout.row}>
                    <Icon icon={unitTypeIcon(unit.type.icon)} size="sm" />
                    {unit.type.name}
                </span>
            ),
        },
        { key: "staff", title: t`Працівників`, numeric: true, render: (unit) => unit.staffCount },
        {
            key: "status",
            title: t`Статус`,
            render: (unit) => (unit.isActive ? <Badge tone="outline">{t`Активний`}</Badge> : <Badge tone="dashed">{t`Неактивний`}</Badge>),
        },
        {
            key: "actions",
            title: t`Дії`,
            actions: true,
            render: (unit) => (
                <ButtonLink to={`/admin/units/${unit.id}`} variant="ghost" size="sm" icon={Pencil}>
                    {t`Редагувати`}
                </ButtonLink>
            ),
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Організації та підрозділи`}
                subtitle={
                    cabinetWide
                        ? t`Усі організації кабінету та їхні підрозділи.`
                        : t`Організації, структурою яких ви керуєте, та їхні підрозділи.`
                }
                breadcrumbs={[{ text: t`Адміністрування`, to: "/admin" }, { text: t`Структура` }]}
                actions={
                    <>
                        <ButtonLink to="/structure" variant="secondary" icon={Network}>
                            {t`Дерево`}
                        </ButtonLink>
                        <ButtonLink to="/admin/unit-types" variant="secondary" icon={Layers}>
                            {t`Типи підрозділів`}
                        </ButtonLink>
                        {cabinetWide && (
                            <ButtonLink to="/admin/units/new?top=true" variant="secondary" icon={Building2}>
                                {t`Нова організація`}
                            </ButtonLink>
                        )}
                        <ButtonLink to="/admin/units/new" icon={Plus}>
                            {t`Новий підрозділ`}
                        </ButtonLink>
                    </>
                }
            />

            <div className={styles.toolbar}>
                <SearchInput value={list.search ?? ""} placeholder={t`Назва або код`} onSearch={(value) => setFilter("search", value)} />
                {organizations.length > 1 && (
                    <div className={styles.filter}>
                        <Select
                            compact
                            aria-label={t`Організація`}
                            value={organizationId ?? ""}
                            onChange={(value) => setFilter("organization", value)}
                            options={[
                                { value: "", label: t`Усі організації` },
                                ...organizations.map((unit) => ({ value: unit.id, label: unit.name })),
                            ]}
                        />
                    </div>
                )}
                <div className={styles.filter}>
                    <Select
                        compact
                        aria-label={t`Тип`}
                        value={typeId ?? ""}
                        onChange={(value) => setFilter("type", value)}
                        options={[
                            { value: "", label: t`Усі типи` },
                            ...(types.data ?? []).map((type) => ({ value: type.id, label: type.name })),
                        ]}
                    />
                </div>
            </div>

            {units.isPending && <Spinner block />}
            {units.isError && <Alert tone="danger">{describeError(units.error, t`Не вдалося завантажити підрозділи.`)}</Alert>}
            {units.data && (
                <DataGrid
                    columns={columns}
                    result={units.data}
                    rowKey={(unit) => unit.id}
                    caption={t`Підрозділи`}
                    emptyIcon={Network}
                    emptyTitle={t`Підрозділів не знайдено`}
                />
            )}
        </>
    );
}
