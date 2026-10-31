import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Eye, Newspaper, Pencil } from "lucide-react";
import { useParams } from "react-router";
import { describeError } from "@/shared/api/problem";
import { newsCategoryNames } from "@/shared/i18n/display";
import { formatDate } from "@/shared/i18n/format";
import { Badge } from "@/shared/ui/Badge";
import { ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { Spinner } from "@/shared/ui/Spinner";
import { articleQuery } from "../api";
import { ArticleBody } from "../components/ArticleBody";
import { Discussion } from "../components/Discussion";
import styles from "./news.module.css";

/**
 * An article: cover, lead, text with its images and galleries, reactions and the discussion.
 * @returns The page.
 */
export function ArticlePage() {
    const { t } = useLingui();
    const { slug = "" } = useParams();
    const article = useQuery(articleQuery(slug));

    if (article.isPending) {
        return <Spinner block />;
    }

    if (article.isError) {
        return (
            <>
                <PageHeader title={t`Новину не знайдено`} breadcrumbs={[{ text: t`Новини`, to: "/news" }, { text: t`Новина` }]} />
                <Card>
                    <EmptyState
                        icon={Newspaper}
                        title={t`Новину не знайдено`}
                        text={describeError(article.error, t`Можливо, її було видалено або знято з публікації.`)}
                    >
                        <ButtonLink to="/news" icon={ArrowLeft}>
                            {t`До новин`}
                        </ButtonLink>
                    </EmptyState>
                </Card>
            </>
        );
    }

    const data = article.data;
    const category = t(newsCategoryNames[data.category]);
    return (
        <article className={styles.article}>
            <PageHeader
                title={data.title}
                breadcrumbs={[
                    { text: t`Новини`, to: "/news" },
                    { text: category, to: `/news?category=${data.category}` },
                ]}
                actions={
                    data.canManage && (
                        <ButtonLink to={`/news/manage/${data.id}`} variant="secondary" icon={Pencil}>
                            {t`Редагувати`}
                        </ButtonLink>
                    )
                }
            >
                <div className={styles.meta}>
                    <Badge>{category}</Badge>
                    {!data.publishedAt && <Badge tone="dashed">{t`Чернетка`}</Badge>}
                    <span>{formatDate(data.publishedAt ?? data.updatedAt)}</span>
                    {data.organization && <span>· {data.organization.name}</span>}
                    <span title={t`Переглядів`}>
                        · <Icon icon={Eye} size="sm" />
                        {data.viewCount}
                    </span>
                </div>
            </PageHeader>
            {data.coverImageUrl && <img className={styles.articleCover} src={data.coverImageUrl} alt="" fetchPriority="high" />}
            <PersonName name={data.author.fullName} photoId={data.author.photoId} meta={t`Автор публікації`} size="md" />
            <p className={styles.lead}>{data.summary}</p>
            <ArticleBody html={data.bodyHtml} />
            <hr className={styles.divider} />
            <Discussion articleId={data.id} />
            <ButtonLink to="/news" variant="ghost" icon={ArrowLeft}>
                {t`Усі новини`}
            </ButtonLink>
        </article>
    );
}
