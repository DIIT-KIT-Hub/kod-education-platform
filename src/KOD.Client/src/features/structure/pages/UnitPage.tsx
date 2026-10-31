import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Network, Pencil, Plus, Users } from "lucide-react";
import { Link, useParams, useSearchParams } from "react-router";
import { ApiError, describeError } from "@/shared/api/problem";
import type { UnitDetails, UnitStaffItem } from "@/shared/api/schema";
import { unitTypeIcon } from "@/shared/lib/unitTypeIcons";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { DataGrid, type GridColumn } from "@/shared/ui/DataGrid";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Spinner } from "@/shared/ui/Spinner";
import { unitQuery, unitStaffQuery } from "../api";
import styles from "./structure.module.css";

/**
 * A unit: its place in the structure, nested units and the staff.
 * @returns The page.
 */
export function UnitPage() {
    const { t } = useLingui();
    const { id = "" } = useParams();
    const unit = useQuery(unitQuery(id));

    if (unit.isPending) {
        return <Spinner block />;
    }

    if (unit.isError) {
        const missing = unit.error instanceof ApiError && unit.error.status === 404;
        return (
            <>
                <PageHeader title={t`Підрозділ`} breadcrumbs={[{ text: t`Структура`, to: "/structure" }, { text: t`Підрозділ` }]} />
                {missing ? (
                    <Card>
                        <EmptyState icon={Network} title={t`Підрозділ не знайдено`} />
                    </Card>
                ) : (
                    <Alert tone="danger">{describeError(unit.error, t`Не вдалося завантажити підрозділ.`)}</Alert>
                )}
            </>
        );
    }

    return <UnitView key={unit.data.id} unit={unit.data} />;
}

/**
 * The loaded unit.
 * @param props The unit.
 * @returns The page content.
 */
function UnitView({ unit }: { readonly unit: UnitDetails }) {
    const { t } = useLingui();
    const user = useCurrentUser();
    const canManage = hasPermission(user, Permissions.structureManage) && unit.canManage;

    return (
        <>
            <PageHeader
                title={unit.name}
                subtitle={unit.type.name}
                breadcrumbs={[
                    { text: t`Структура`, to: "/structure" },
                    ...unit.path.map((item) =>
                        item.id === unit.id ? { text: item.name } : { text: item.name, to: `/structure/${item.id}` },
                    ),
                ]}
                actions={
                    canManage && (
                        <>
                            <ButtonLink href={`/admin/units/new?parent=${unit.id}`} variant="secondary" icon={Plus}>
                                {t`Додати підрозділ`}
                            </ButtonLink>
                            <ButtonLink href={`/admin/units/${unit.id}`} variant="secondary" icon={Pencil}>
                                {t`Редагувати`}
                            </ButtonLink>
                        </>
                    )
                }
            >
                <div className={layout.cluster}>
                    <Badge tone="dashed">{unit.code}</Badge>
                    {unit.organization.id !== unit.id && (
                        <Link to={`/structure/${unit.organization.id}`}>
                            <Badge tone="outline">{t`Організація: ${unit.organization.name}`}</Badge>
                        </Link>
                    )}
                    {unit.shortName && <Badge>{unit.shortName}</Badge>}
                    {!unit.isActive && <Badge tone="danger">{t`Неактивний`}</Badge>}
                </div>
            </PageHeader>

            <div className={layout.stackLg}>
                {unit.children.length > 0 && (
                    <section className={[styles.units, layout.stagger].join(" ")} aria-label={t`Вкладені підрозділи`}>
                        {unit.children.map((child) => (
                            <Link key={child.id} className={styles.unit} to={`/structure/${child.id}`}>
                                <span className={styles.tile}>
                                    <Icon icon={unitTypeIcon(child.type.icon)} />
                                </span>
                                <span className={styles.unitBody}>
                                    <span className={styles.unitName}>{child.name}</span>
                                    <span className={[layout.textSm, layout.muted].join(" ")}>
                                        {child.type.name} · {t`працівників: ${child.staffCount}`}
                                    </span>
                                </span>
                            </Link>
                        ))}
                    </section>
                )}

                <section>
                    <h2 className={styles.sectionTitle}>{t`Працівники`}</h2>
                    <Staff unitId={unit.id} />
                </section>
            </div>
        </>
    );
}

/**
 * The staff of a unit: search and pages in the address.
 * @param props The unit.
 * @returns The list.
 */
function Staff({ unitId }: { readonly unitId: string }) {
    const { t } = useLingui();
    const [params, setParams] = useSearchParams();
    const search = params.get("search") ?? undefined;
    const page = Math.max(1, Number(params.get("page")) || 1);
    const pageSize = Number(params.get("pageSize")) || 20;
    const staff = useQuery(unitStaffQuery(unitId, { search, page, pageSize }));

    const columns: GridColumn<UnitStaffItem>[] = [
        {
            key: "name",
            title: t`Працівник`,
            primary: true,
            render: (item) => (
                <span className={layout.row}>
                    <PersonName name={item.fullName} photoId={item.photoId} />
                    {item.isLeadership && <Badge tone="inverse">{t`Керівник`}</Badge>}
                </span>
            ),
        },
        {
            key: "position",
            title: t`Посада`,
            render: (item) => (
                <>
                    {item.position}
                    {!item.isPrimary && <span className={[layout.textXs, layout.muted].join(" ")}> {t`(сумісництво)`}</span>}
                </>
            ),
        },
        {
            key: "email",
            title: t`E-mail`,
            render: (item) => (
                <a className={layout.link} href={`mailto:${item.email}`}>
                    {item.email}
                </a>
            ),
        },
        {
            key: "phone",
            title: t`Телефон`,
            nowrap: true,
            render: (item) =>
                item.phoneNumber ? (
                    <a className={layout.link} href={`tel:${item.phoneNumber}`}>
                        {item.phoneNumber}
                    </a>
                ) : (
                    "—"
                ),
        },
    ];

    return (
        <>
            <div className={styles.toolbar}>
                <SearchInput
                    value={search ?? ""}
                    placeholder={t`Пошук працівника`}
                    onSearch={(value) => {
                        const next = new URLSearchParams(params);
                        if (value) {
                            next.set("search", value);
                        } else {
                            next.delete("search");
                        }

                        next.delete("page");
                        setParams(next, { replace: true, preventScrollReset: true });
                    }}
                />
            </div>
            {staff.isPending && <Spinner block />}
            {staff.isError && <Alert tone="danger">{describeError(staff.error, t`Не вдалося завантажити працівників.`)}</Alert>}
            {staff.data && (
                <DataGrid
                    columns={columns}
                    result={staff.data}
                    rowKey={(item) => `${item.userId}:${item.position}:${item.isPrimary}`}
                    caption={t`Працівники підрозділу`}
                    emptyIcon={Users}
                    emptyTitle={t`Працівників не знайдено`}
                />
            )}
        </>
    );
}
