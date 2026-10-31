import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Eye, Info, Layers, Pencil, Plus } from "lucide-react";
import { describeError } from "@/shared/api/problem";
import type { UnitTypeItem } from "@/shared/api/schema";
import { unitTypeIcon } from "@/shared/lib/unitTypeIcons";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { DataGrid, GridPrimary, type GridColumn } from "@/shared/ui/DataGrid";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Spinner } from "@/shared/ui/Spinner";
import { unitTypesQuery } from "../api";

/**
 * The catalog of unit types, shared by every organization (only a cabinet-wide administrator changes it).
 * @returns The page.
 */
export function UnitTypesPage() {
    const { t } = useLingui();
    const cabinetWide = hasPermission(useCurrentUser(), Permissions.administrationAll);
    const types = useQuery(unitTypesQuery);

    const columns: GridColumn<UnitTypeItem>[] = [
        {
            key: "name",
            title: t`Тип`,
            primary: true,
            render: (type) => (
                <GridPrimary to={`/admin/unit-types/${type.id}`} secondary={type.description}>
                    <span className={layout.row}>
                        <Icon icon={unitTypeIcon(type.icon)} size="sm" />
                        {type.name}
                    </span>
                </GridPrimary>
            ),
        },
        { key: "code", title: t`Код`, render: (type) => <span className={layout.mono}>{type.code}</span> },
        {
            key: "level",
            title: t`Рівень`,
            render: (type) =>
                type.isOrganization ? <Badge tone="inverse">{t`Організація`}</Badge> : <Badge tone="outline">{t`Підрозділ`}</Badge>,
        },
        { key: "units", title: t`Підрозділів`, numeric: true, render: (type) => type.unitCount },
        {
            key: "actions",
            title: t`Дії`,
            actions: true,
            render: (type) => (
                <ButtonLink to={`/admin/unit-types/${type.id}`} variant="ghost" size="sm" icon={cabinetWide ? Pencil : Eye}>
                    {cabinetWide ? t`Редагувати` : t`Переглянути`}
                </ButtonLink>
            ),
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Типи підрозділів`}
                subtitle={t`Назви рівнів структури у словнику вашої організації: департамент, управління, відділ, сектор, філія…`}
                breadcrumbs={[
                    { text: t`Адміністрування`, to: "/admin" },
                    { text: t`Структура`, to: "/admin/units" },
                    { text: t`Типи підрозділів` },
                ]}
                actions={
                    cabinetWide && (
                        <ButtonLink to="/admin/unit-types/new" icon={Plus}>
                            {t`Новий тип`}
                        </ButtonLink>
                    )
                }
            />

            <div className={layout.stackLg}>
                {!cabinetWide && (
                    <Alert tone="outline" icon={Info}>
                        {t`Каталог типів спільний для всіх організацій кабінету, тому змінювати його може лише адміністратор усіх організацій. Ви можете використовувати ці типи для підрозділів своїх організацій.`}
                    </Alert>
                )}
                {types.isPending && <Spinner block />}
                {types.isError && <Alert tone="danger">{describeError(types.error, t`Не вдалося завантажити типи підрозділів.`)}</Alert>}
                {types.data && (
                    <DataGrid
                        columns={columns}
                        result={{ items: types.data, page: 1, pageSize: Math.max(types.data.length, 1), totalCount: types.data.length }}
                        paged={false}
                        rowKey={(type) => type.id}
                        caption={t`Типи підрозділів`}
                        emptyIcon={Layers}
                        emptyTitle={t`Типів ще немає`}
                        emptyText={t`Створіть хоча б один тип, позначений як організація.`}
                    />
                )}
            </div>
        </>
    );
}
