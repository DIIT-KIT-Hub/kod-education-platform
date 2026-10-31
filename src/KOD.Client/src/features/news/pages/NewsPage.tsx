import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { Eye, MessageCircle, Newspaper, Pin, Plus, Settings, SmilePlus } from "lucide-react";
import { Link, useSearchParams } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { NewsCategory, NewsListItem } from "@/shared/api/schema";
import { newsCategoryNames } from "@/shared/i18n/display";
import { formatDate } from "@/shared/i18n/format";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Pagination } from "@/shared/ui/Pagination";
import { PersonName } from "@/shared/ui/PersonName";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Spinner } from "@/shared/ui/Spinner";
import { Tab, Tabs } from "@/shared/ui/Tabs";
import { newsListQuery } from "../api";
import styles from "./news.module.css";

/** The categories, in the order of the tabs. */
const categories = Object.keys(newsCategoryNames) as NewsCategory[];

/**
 * The news of the organization: categories, search and pages of cards.
 * @returns The page.
 */
export function NewsPage() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const [params, setParams] = useSearchParams();
    const category = categories.find((item) => item === params.get("category"));
    const search = params.get("search") ?? undefined;
    const page = Math.max(1, Number(params.get("page")) || 1);
    const pageSize = Number(params.get("pageSize")) || 9;
    const result = useQuery(newsListQuery({ category, search, page, pageSize }));
    const canManage = hasPermission(user, Permissions.newsManage);

    return (
        <>
            <PageHeader
                title={t`Новини`}
                subtitle={t`Новини, оголошення та події організації.`}
                actions={
                    canManage && (
                        <>
                            <ButtonLink to="/news/manage" variant="secondary" icon={Settings}>
                                {t`Керування`}
                            </ButtonLink>
                            <ButtonLink to="/news/manage/new" icon={Plus}>
                                {t`Нова публікація`}
                            </ButtonLink>
                        </>
                    )
                }
            />

            <Tabs label={t`Категорії`}>
                <Tab to="/news" active={!category}>
                    {t`Усі`}
                </Tab>
                {categories.map((item) => (
                    <Tab key={item} to={`/news?category=${item}`} active={category === item}>
                        {t(newsCategoryNames[item])}
                    </Tab>
                ))}
            </Tabs>

            <div className={styles.toolbar}>
                <SearchInput
                    value={search ?? ""}
                    placeholder={t`Пошук у новинах`}
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
            {result.data &&
                (result.data.items.length === 0 ? (
                    <Card>
                        <EmptyState icon={Newspaper} title={t`Новин не знайдено`} text={t`Спробуйте іншу категорію або пошуковий запит.`} />
                    </Card>
                ) : (
                    <>
                        <div className={[styles.grid, layout.stagger].join(" ")}>
                            {result.data.items.map((article) => (
                                <NewsCard key={article.id} article={article} />
                            ))}
                        </div>
                        <Pagination
                            page={result.data.page}
                            pageSize={result.data.pageSize}
                            totalCount={result.data.totalCount}
                            pageSizes={[9, 18, 36, 72]}
                        />
                    </>
                ))}
        </>
    );
}

/**
 * The card of an article in the list.
 * @param props The article.
 * @returns The card.
 */
function NewsCard({ article }: { readonly article: NewsListItem }) {
    const { t } = useLingui();
    return (
        <Link className={styles.card} to={`/news/${article.slug}`}>
            <div className={styles.media}>
                {article.coverImageUrl ? (
                    <img
                        className={styles.cover}
                        src={article.coverThumbnailUrl ?? article.coverImageUrl}
                        alt=""
                        loading="lazy"
                        decoding="async"
                    />
                ) : (
                    <div className={styles.placeholder}>
                        <Icon icon={Newspaper} size="xl" />
                    </div>
                )}
            </div>
            <div className={styles.cardBody}>
                <div className={layout.cluster}>
                    <Badge>{t(newsCategoryNames[article.category])}</Badge>
                    {article.isPinned && (
                        <Badge tone="inverse" icon={Pin}>
                            {t`Закріплено`}
                        </Badge>
                    )}
                </div>
                <span className={styles.cardTitle}>{article.title}</span>
                <span className={styles.summary}>{article.summary}</span>
                <span className={[layout.textSm, layout.muted].join(" ")}>
                    {formatDate(article.publishedAt)}
                    {article.organization && ` · ${article.organization.name}`}
                </span>
                <span className={styles.footer}>
                    <PersonName name={article.authorName} photoId={article.authorPhotoId} />
                    <span className={styles.stats}>
                        <span title={t`Переглядів`}>
                            <Icon icon={Eye} size="sm" />
                            {article.viewCount}
                        </span>
                        <span title={t`Реакцій`}>
                            <Icon icon={SmilePlus} size="sm" />
                            {article.reactionCount}
                        </span>
                        <span title={t`Коментарів`}>
                            <Icon icon={MessageCircle} size="sm" />
                            {article.commentCount}
                        </span>
                    </span>
                </span>
            </div>
        </Link>
    );
}
