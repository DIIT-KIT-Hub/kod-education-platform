import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Check, Newspaper, Pencil, Pin, Plus } from "lucide-react";
import { useSearchParams } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { NewsListItem } from "@/shared/api/schema";
import { newsCategoryNames } from "@/shared/i18n/display";
import { formatDateTime } from "@/shared/i18n/format";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { DataGrid, GridPrimary, type GridColumn } from "@/shared/ui/DataGrid";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Spinner } from "@/shared/ui/Spinner";
import { newsListQuery } from "../api";
import styles from "./news.module.css";

/**
 * Drafts and published articles of the organizations whose news the user manages.
 * @returns The page.
 */
export function NewsManagePage() {
    const { t } = useLingui();
    const [params, setParams] = useSearchParams();
    const search = params.get("search") ?? undefined;
    const result = useQuery(
        newsListQuery({
            search,
            includeDrafts: true,
            page: Math.max(1, Number(params.get("page")) || 1),
            pageSize: Number(params.get("pageSize")) || 20,
            sortBy: params.get("sortBy") ?? undefined,
            descending: params.get("descending") === "true",
        }),
    );

    const columns: GridColumn<NewsListItem>[] = [
        {
            key: "title",
            title: t`Заголовок`,
            sortKey: "title",
            primary: true,
            render: (item) => (
                <GridPrimary to={`/news/manage/${item.id}`} secondary={t(newsCategoryNames[item.category])}>
                    {item.title}
                </GridPrimary>
            ),
        },
        {
            key: "status",
            title: t`Статус`,
            render: (item) => (
                <div className={layout.cluster}>
                    {item.publishedAt ? (
                        <Badge tone="inverse" icon={Check}>
                            {t`Опубліковано`}
                        </Badge>
                    ) : (
                        <Badge tone="dashed">{t`Чернетка`}</Badge>
                    )}
                    {item.isPinned && <Badge icon={Pin}>{t`Закріплено`}</Badge>}
                </div>
            ),
        },
        { key: "published", title: t`Опубліковано`, nowrap: true, render: (item) => formatDateTime(item.publishedAt) },
        { key: "updated", title: t`Змінено`, sortKey: "updated", nowrap: true, render: (item) => formatDateTime(item.updatedAt) },
        { key: "organization", title: t`Організація`, render: (item) => item.organization?.name ?? t`Увесь кабінет` },
        { key: "author", title: t`Автор`, render: (item) => <PersonName name={item.authorName} photoId={item.authorPhotoId} /> },
        {
            key: "actions",
            title: t`Дії`,
            actions: true,
            render: (item) => (
                <ButtonLink to={`/news/manage/${item.id}`} variant="ghost" size="sm" icon={Pencil}>
                    {t`Редагувати`}
                </ButtonLink>
            ),
        },
    ];

    return (
        <>
            <PageHeader
                title={t`Керування новинами`}
                subtitle={t`Чернетки та опубліковані матеріали організацій, новинами яких ви керуєте.`}
                breadcrumbs={[{ text: t`Новини`, to: "/news" }, { text: t`Керування` }]}
                actions={
                    <ButtonLink to="/news/manage/new" icon={Plus}>
                        {t`Нова публікація`}
                    </ButtonLink>
                }
            />
            <div className={styles.toolbar}>
                <SearchInput
                    value={search ?? ""}
                    placeholder={t`Заголовок або анонс`}
                    onSearch={(value) => {
                        const next = new URLSearchParams(params);
                        if (value) {
                            next.set("search", value);
                        } else {
                            next.delete("search");
                        }

                        next.delete("page");
                        setParams(next, { replace: true });
                    }}
                />
            </div>
            {result.isPending && <Spinner block />}
            {result.isError && <Alert tone="danger">{describeError(result.error, t`Не вдалося завантажити новини.`)}</Alert>}
            {result.data && (
                <DataGrid
                    columns={columns}
                    result={result.data}
                    rowKey={(item) => item.id}
                    caption={t`Новини`}
                    emptyIcon={Newspaper}
                    emptyTitle={t`Публікацій ще немає`}
                />
            )}
        </>
    );
}
