import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Network, Plus, SearchX, Settings } from "lucide-react";
import { useSearchParams } from "react-router";
import { describeError } from "@/shared/api/problem";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import { Alert } from "@/shared/ui/Alert";
import { ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { EmptyState } from "@/shared/ui/EmptyState";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Spinner } from "@/shared/ui/Spinner";
import { unitTreeQuery } from "../api";
import { filterTree, UnitTree } from "../components/UnitTree";
import styles from "./structure.module.css";

/**
 * The organizational tree: every organization with its units, filtered by name or code.
 * @returns The page.
 */
export function StructurePage() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const [params, setParams] = useSearchParams();
    const search = params.get("search") ?? "";
    const tree = useQuery(unitTreeQuery);
    const canManage = hasPermission(user, Permissions.structureManage);
    const canCreateOrganization = canManage && hasPermission(user, Permissions.administrationAll);
    const text = search.trim().toLocaleLowerCase();
    const nodes = tree.data ? filterTree(tree.data, text) : [];

    return (
        <>
            <PageHeader
                title={t`Структура`}
                subtitle={t`Організації, їхні підрозділи та працівники.`}
                actions={
                    canManage && (
                        <>
                            <ButtonLink to="/admin/units" variant="secondary" icon={Settings}>
                                {t`Керувати структурою`}
                            </ButtonLink>
                            {canCreateOrganization && (
                                <ButtonLink to="/admin/units/new?top=true" icon={Plus}>
                                    {t`Нова організація`}
                                </ButtonLink>
                            )}
                        </>
                    )
                }
            />

            {tree.data && tree.data.length > 0 && (
                <div className={styles.toolbar}>
                    <SearchInput
                        value={search}
                        placeholder={t`Назва, абревіатура або код підрозділу`}
                        onSearch={(value) => {
                            const next = new URLSearchParams(params);
                            if (value) {
                                next.set("search", value);
                            } else {
                                next.delete("search");
                            }

                            setParams(next, { replace: true });
                        }}
                    />
                </div>
            )}

            {tree.isPending && <Spinner block />}
            {tree.isError && <Alert tone="danger">{describeError(tree.error, t`Не вдалося завантажити структуру.`)}</Alert>}
            {tree.data && (
                <Card>
                    {tree.data.length === 0 ? (
                        <EmptyState icon={Network} title={t`Структуру ще не заповнено`} />
                    ) : nodes.length === 0 ? (
                        <EmptyState icon={SearchX} title={t`Підрозділів не знайдено`} text={t`Спробуйте іншу назву або код.`} />
                    ) : (
                        // A new search opens the branches that lead to the results again.
                        <UnitTree key={text} nodes={nodes} expanded={text.length > 0} />
                    )}
                </Card>
            )}
        </>
    );
}
