import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Eye, ImageUp, Save, Trash2 } from "lucide-react";
import { useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { useNavigate, useParams } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { NewsArticleDto, NewsCategory } from "@/shared/api/schema";
import { newsCategoryNames } from "@/shared/i18n/display";
import { imageAccept } from "@/shared/lib/image";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { Checkbox } from "@/shared/ui/Checkbox";
import { confirm } from "@/shared/ui/ConfirmHost";
import { Field } from "@/shared/ui/Field";
import { FileDrop } from "@/shared/ui/FileDrop";
import { PageHeader } from "@/shared/ui/PageHeader";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { Textarea } from "@/shared/ui/Textarea";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { articleForEditQuery, newsApi, newsKeys, newsOrganizationsQuery } from "../api";
import { RichTextEditor } from "../components/RichTextEditor";
import styles from "./news.module.css";

/** The categories, in the order of the list. */
const categories = Object.keys(newsCategoryNames) as NewsCategory[];

/** The largest cover image (matches FileRules.MaxImageSize). */
const maxImageBytes = 15 * 1024 * 1024;

/**
 * Creates or changes an article: text, publication settings and the cover.
 * @returns The page.
 */
export function NewsEditorPage() {
    const { id } = useParams();
    const organizations = useQuery(newsOrganizationsQuery);
    const existing = useQuery({ ...articleForEditQuery(id ?? ""), enabled: id !== undefined });

    if (organizations.isPending || (id !== undefined && existing.isPending)) {
        return <Spinner block />;
    }

    return (
        <ArticleEditor
            key={id ?? "new"}
            existing={existing.data ?? null}
            organizations={organizations.data?.organizations ?? []}
            cabinetWide={organizations.data?.cabinetWide ?? false}
        />
    );
}

/**
 * Props of {@link ArticleEditor}.
 */
interface ArticleEditorProps {
    readonly existing: NewsArticleDto | null;
    readonly organizations: readonly { readonly id: string; readonly name: string }[];
    readonly cabinetWide: boolean;
}

/**
 * The editor form.
 * @param props The props.
 * @returns The form.
 */
function ArticleEditor({ existing, organizations, cabinetWide }: ArticleEditorProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [cover, setCover] = useState<File | null>(null);
    const [uploading, setUploading] = useState(0);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    const schema = z.object({
        title: z
            .string()
            .trim()
            .min(1, t`Вкажіть заголовок.`)
            .max(200, t`Заголовок не може бути довшим за 200 символів.`),
        summary: z
            .string()
            .trim()
            .min(1, t`Вкажіть анонс.`)
            .max(500, t`Анонс не може бути довшим за 500 символів.`),
        body: z.string().min(1, t`Напишіть текст новини.`),
        category: z.string(),
        organizationId: z.string(),
        publish: z.boolean(),
        isPinned: z.boolean(),
        allowComments: z.boolean(),
        removeCover: z.boolean(),
    });

    const { register, control, handleSubmit, formState, setValue } = useForm({
        resolver: zodResolver(schema),
        defaultValues: {
            title: existing?.title ?? "",
            summary: existing?.summary ?? "",
            body: existing?.bodyHtml ?? "",
            category: existing?.category ?? "News",
            organizationId: existing
                ? (existing.organization?.id ?? "")
                : cabinetWide || organizations.length === 0
                  ? ""
                  : (organizations[0]?.id ?? ""),
            publish: existing ? existing.publishedAt !== null : true,
            isPinned: existing?.isPinned ?? false,
            allowComments: existing?.allowComments ?? true,
            removeCover: false,
        },
    });
    const removeCover = useWatch({ control, name: "removeCover" });

    /**
     * Saves the article and its cover.
     * @param values The form values.
     */
    async function save(values: z.infer<typeof schema>): Promise<void> {
        if (uploading > 0) {
            setError(t`Дочекайтеся, поки завантажаться фото.`);
            return;
        }

        setBusy(true);
        setError(null);
        const form = {
            title: values.title,
            summary: values.summary,
            body: values.body,
            category: values.category as NewsCategory,
            isPinned: values.isPinned,
            publish: values.publish,
            removeCover: values.removeCover,
            organizationId: values.organizationId || null,
            allowComments: values.allowComments,
        };
        try {
            const id = existing ? (await newsApi.update(existing.id, form), existing.id) : await newsApi.create(form);
            if (cover) {
                await newsApi.cover(id, cover);
            }

            await queryClient.invalidateQueries({ queryKey: newsKeys.all });
            toast(t`Публікацію збережено`);
            await navigate("/news/manage", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зберегти публікацію. Спробуйте ще раз.`));
        } finally {
            setBusy(false);
        }
    }

    /**
     * Deletes the article after a confirmation.
     */
    async function remove(): Promise<void> {
        if (!existing || !(await confirm({ title: t`Видалити публікацію остаточно?`, confirmText: t`Видалити`, danger: true }))) {
            return;
        }

        try {
            await newsApi.remove(existing.id);
            await queryClient.invalidateQueries({ queryKey: newsKeys.all });
            toast(t`Публікацію видалено`);
            await navigate("/news/manage", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
        }
    }

    return (
        <>
            <PageHeader
                title={existing ? t`Редагування публікації` : t`Нова публікація`}
                breadcrumbs={[
                    { text: t`Новини`, to: "/news" },
                    { text: t`Керування`, to: "/news/manage" },
                    { text: existing ? t`Редагування` : t`Нова` },
                ]}
                actions={
                    existing?.publishedAt && (
                        <ButtonLink to={`/news/${existing.slug}`} variant="secondary" icon={Eye}>
                            {t`Переглянути`}
                        </ButtonLink>
                    )
                }
            />

            <form className={layout.split} noValidate onSubmit={handleSubmit(save)}>
                <div className={layout.stackLg}>
                    {error && <Alert tone="danger">{error}</Alert>}
                    <Card>
                        <div className={layout.stack}>
                            <Field label={t`Заголовок`} required error={formState.errors.title?.message}>
                                {(ids) => <TextInput {...ids} {...register("title")} maxLength={200} />}
                            </Field>
                            <Field
                                label={t`Анонс`}
                                required
                                hint={t`1–2 речення для картки новини.`}
                                error={formState.errors.summary?.message}
                            >
                                {(ids) => <Textarea {...ids} {...register("summary")} maxLength={500} rows={3} />}
                            </Field>
                            <Field
                                label={t`Текст`}
                                required
                                hint={t`Заголовки, списки, цитати, таблиці, посилання, фото в тексті та галереї. Фото можна вставити з буфера обміну або перетягнути; вони стискаються до 10–100 КБ.`}
                                error={formState.errors.body?.message}
                            >
                                {() => (
                                    <RichTextEditor
                                        initialHtml={existing?.bodyHtml ?? ""}
                                        label={t`Текст новини`}
                                        placeholder={t`Почніть писати новину…`}
                                        invalid={formState.errors.body !== undefined}
                                        onUploadingChange={setUploading}
                                        onChange={(html) => setValue("body", html, { shouldValidate: formState.isSubmitted })}
                                    />
                                )}
                            </Field>
                        </div>
                    </Card>
                </div>

                <div className={layout.stackLg}>
                    <Card title={t`Публікація`}>
                        <div className={layout.stack}>
                            <Field label={t`Організація`} hint={t`Редагувати новину можуть фахівці з комунікацій цієї організації.`}>
                                {(ids) => (
                                    <Controller
                                        control={control}
                                        name="organizationId"
                                        render={({ field }) => (
                                            <Select
                                                {...ids}
                                                ref={field.ref}
                                                value={field.value}
                                                onChange={field.onChange}
                                                onBlur={field.onBlur}
                                                options={[
                                                    ...(cabinetWide ? [{ value: "", label: t`Увесь кабінет (усі організації)` }] : []),
                                                    ...organizations.map((organization) => ({
                                                        value: organization.id,
                                                        label: organization.name,
                                                    })),
                                                ]}
                                            />
                                        )}
                                    />
                                )}
                            </Field>
                            <Field label={t`Категорія`}>
                                {(ids) => (
                                    <Controller
                                        control={control}
                                        name="category"
                                        render={({ field }) => (
                                            <Select
                                                {...ids}
                                                ref={field.ref}
                                                value={field.value}
                                                onChange={field.onChange}
                                                onBlur={field.onBlur}
                                                options={categories.map((category) => ({
                                                    value: category,
                                                    label: t(newsCategoryNames[category]),
                                                }))}
                                            />
                                        )}
                                    />
                                )}
                            </Field>
                            <Checkbox {...register("publish")} hint={t`Новину бачать усі працівники.`}>
                                {t`Опубліковано`}
                            </Checkbox>
                            <Checkbox {...register("isPinned")}>{t`Закріпити зверху`}</Checkbox>
                            <Checkbox {...register("allowComments")} hint={t`Реакції доступні завжди.`}>
                                {t`Дозволити коментарі`}
                            </Checkbox>
                        </div>
                    </Card>

                    <Card title={t`Обкладинка`}>
                        {existing?.coverImageUrl && !removeCover && !cover && (
                            <>
                                <img className={styles.editorCover} src={existing.coverImageUrl} alt="" />
                                <Checkbox {...register("removeCover")}>{t`Прибрати обкладинку`}</Checkbox>
                            </>
                        )}
                        <FileDrop
                            icon={ImageUp}
                            accept={imageAccept}
                            title={cover?.name ?? t`Оберіть зображення`}
                            hint={t`JPEG, PNG, WebP, AVIF або GIF до 15 МБ, бажано 16:9. Буде стиснуто в AVIF.`}
                            onFiles={(files) => {
                                const file = files[0] ?? null;
                                if (file && file.size > maxImageBytes) {
                                    setError(t`Зображення не може бути більшим за 15 МБ.`);
                                    return;
                                }

                                setError(null);
                                setCover(file);
                            }}
                        />
                    </Card>

                    <Card variant="muted">
                        <div className={layout.stack}>
                            <Button type="submit" block icon={Save} busy={busy}>
                                {t`Зберегти`}
                            </Button>
                            {existing && (
                                <Button variant="danger" block icon={Trash2} disabled={busy} onClick={() => void remove()}>
                                    {t`Видалити`}
                                </Button>
                            )}
                        </div>
                    </Card>
                </div>
            </form>
        </>
    );
}
