import { useLingui } from "@lingui/react/macro";
import { useQuery } from "@tanstack/react-query";
import { ArrowRight, BadgeCheck, ChevronRight, CircleCheck, FilePlus, Hourglass, Newspaper, Pencil, Pin, Signature } from "lucide-react";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import { documentKindNames, newsCategoryNames } from "@/shared/i18n/display";
import { formatDate, formatNumber, kyivHour } from "@/shared/i18n/format";
import { hasPermission, Permissions, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Avatar } from "@/shared/ui/Avatar";
import { ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { DocumentTitle } from "@/shared/ui/DocumentTitle";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Icon } from "@/shared/ui/Icon";
import { List, ListItem } from "@/shared/ui/List";
import { Spinner } from "@/shared/ui/Spinner";
import { StatCard } from "@/shared/ui/StatCard";
import styles from "./HomePage.module.css";

/**
 * The home page data.
 */
const dashboardQuery = {
    queryKey: ["dashboard"],
    queryFn: () => unwrap(api.GET("/api/v1/dashboard")),
} as const;

/**
 * The home page: greeting, key figures, documents awaiting the user's signature and the latest news.
 * @returns The page.
 */
export function HomePage() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const summary = useQuery(dashboardQuery);
    const canDocuments = hasPermission(user, Permissions.documentsView);
    const canNews = hasPermission(user, Permissions.newsView);
    const parts = user.fullName.split(" ").filter(Boolean);
    const firstName = parts.length > 1 ? parts[1] : user.fullName;
    const hour = kyivHour(new Date());
    const greeting = hour < 5 ? t`Доброї ночі,` : hour < 12 ? t`Доброго ранку,` : hour < 18 ? t`Доброго дня,` : t`Доброго вечора,`;

    return (
        <div className={layout.stackXl}>
            <DocumentTitle>{t`Головна`}</DocumentTitle>
            <section className={styles.hero} data-inverse>
                <p className={[layout.textSm, layout.strong].join(" ")}>{greeting}</p>
                <h1 tabIndex={-1}>{firstName}</h1>
                <p
                    className={styles.lead}
                >{t`Документи, які очікують на ваш підпис, новини організації та швидкий доступ до сервісів — в одному місці.`}</p>
                <div className={layout.cluster}>
                    {hasPermission(user, Permissions.documentsCreate) && (
                        <ButtonLink href="/documents/new" icon={FilePlus}>
                            {t`Новий документ`}
                        </ButtonLink>
                    )}
                    {hasPermission(user, Permissions.signingUse) && (
                        <ButtonLink href="/signing" variant="secondary" icon={Signature}>
                            {t`Підписати файл`}
                        </ButtonLink>
                    )}
                </div>
            </section>

            {summary.isPending && <Spinner block />}

            {summary.data && (
                <>
                    {canDocuments && (
                        <section className={[layout.colsStats, layout.stagger].join(" ")} aria-label={t`Показники`}>
                            <StatCard
                                label={t`Очікують мого підпису`}
                                value={formatNumber(summary.data.awaitingMyDecision)}
                                icon={Signature}
                                href="/documents?scope=AwaitingMe"
                                inverse={summary.data.awaitingMyDecision > 0}
                            />
                            <StatCard
                                label={t`Мої чернетки`}
                                value={formatNumber(summary.data.myDrafts)}
                                icon={Pencil}
                                href="/documents?scope=Authored&status=Draft"
                            />
                            <StatCard
                                label={t`Мої на підписанні`}
                                value={formatNumber(summary.data.myInSigning)}
                                icon={Hourglass}
                                href="/documents?scope=Authored&status=InSigning"
                            />
                            <StatCard
                                label={t`Підписано мною цього місяця`}
                                value={formatNumber(summary.data.signedThisMonth)}
                                icon={BadgeCheck}
                                href="/documents?scope=SignedByMe"
                            />
                        </section>
                    )}

                    <div className={layout.split}>
                        {canDocuments && (
                            <Card
                                title={t`Очікують мого підпису`}
                                subtitle={t`Документи, де зараз ваша черга`}
                                actions={
                                    <ButtonLink href="/documents?scope=AwaitingMe" variant="ghost" size="sm" iconEnd={ArrowRight}>
                                        {t`Усі`}
                                    </ButtonLink>
                                }
                            >
                                {summary.data.awaiting.length === 0 ? (
                                    <EmptyState icon={CircleCheck} title={t`Усе підписано`} text={t`Нових документів на підпис немає.`} />
                                ) : (
                                    <List>
                                        {summary.data.awaiting.map((document) => (
                                            <ListItem
                                                key={document.id}
                                                href={`/documents/${document.id}`}
                                                start={<Avatar name={document.authorName} photoId={document.authorPhotoId} />}
                                                title={document.title}
                                                meta={`${t`№ ${document.registrationNumber}`} · ${t(documentKindNames[document.kind])} · ${document.authorName}`}
                                                end={<Icon icon={ChevronRight} className={layout.muted} />}
                                            />
                                        ))}
                                    </List>
                                )}
                            </Card>
                        )}

                        {canNews && (
                            <Card
                                title={t`Новини`}
                                subtitle={t`Останні оголошення`}
                                actions={
                                    <ButtonLink href="/news" variant="ghost" size="sm" iconEnd={ArrowRight}>
                                        {t`Усі`}
                                    </ButtonLink>
                                }
                            >
                                {summary.data.latestNews.length === 0 ? (
                                    <EmptyState icon={Newspaper} title={t`Новин поки немає`} />
                                ) : (
                                    <List>
                                        {summary.data.latestNews.map((article) => (
                                            <ListItem
                                                key={article.id}
                                                href={`/news/${article.slug}`}
                                                title={article.title}
                                                meta={`${t(newsCategoryNames[article.category])} · ${formatDate(article.publishedAt)}`}
                                                end={article.isPinned ? <Icon icon={Pin} size="sm" /> : undefined}
                                            />
                                        ))}
                                    </List>
                                )}
                            </Card>
                        )}
                    </div>
                </>
            )}
        </div>
    );
}
