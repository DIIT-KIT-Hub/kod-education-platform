import { zodResolver } from "@hookform/resolvers/zod";
import { useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    ArrowLeft,
    BadgeCheck,
    ChevronDown,
    ChevronUp,
    FileText,
    FileUp,
    FileX,
    Info,
    Save,
    Send,
    TriangleAlert,
    Users,
    X,
} from "lucide-react";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { DocumentEditModel, DocumentKind, SigningMode, UnitOption, UserLookupItem } from "@/shared/api/schema";
import { documentKindNames, signingModeNames } from "@/shared/i18n/display";
import { formatFileSize } from "@/shared/i18n/format";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Avatar } from "@/shared/ui/Avatar";
import { Badge } from "@/shared/ui/Badge";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Field } from "@/shared/ui/Field";
import { FileDrop } from "@/shared/ui/FileDrop";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { Progress } from "@/shared/ui/Progress";
import { SearchInput } from "@/shared/ui/SearchInput";
import { Segmented } from "@/shared/ui/Segmented";
import { Select } from "@/shared/ui/Select";
import { Spinner } from "@/shared/ui/Spinner";
import { Textarea } from "@/shared/ui/Textarea";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { authorUnitsQuery, documentEditQuery, documentKeys, documentsApi, signerSearchQuery } from "../api";
import styles from "./documents.module.css";

/** The largest document file (matches FileRules.MaxDocumentSize). */
const maxFileBytes = 25 * 1024 * 1024;

/** The accepted document types (matches FileRules.DocumentTypes). */
const accept = ".doc,.docx,.jpeg,.jpg,.odt,.ods,.pdf,.png,.ppt,.pptx,.rtf,.txt,.xls,.xlsx,.xml,.zip";

/** The kinds, in the order of the list. */
const kinds = Object.keys(documentKindNames) as DocumentKind[];

/**
 * Creates a document or changes a draft / rejected one: file, details and the signing route.
 * @returns The page.
 */
export function DocumentEditorPage() {
    const { t } = useLingui();
    const { id } = useParams();
    const units = useQuery(authorUnitsQuery);
    const existing = useQuery({ ...documentEditQuery(id ?? ""), enabled: id !== undefined });

    if (units.isPending || (id !== undefined && existing.isPending)) {
        return <Spinner block />;
    }

    if (id !== undefined && existing.isError) {
        return (
            <>
                <PageHeader
                    title={t`Редагування документа`}
                    breadcrumbs={[{ text: t`Документи`, to: "/documents" }, { text: t`Редагування` }]}
                />
                <Card>
                    <EmptyState
                        icon={FileX}
                        title={t`Документ не можна змінити`}
                        text={describeError(existing.error, t`Документ не існує або у вас немає до нього доступу.`)}
                    >
                        <ButtonLink to={`/documents/${id}`} variant="secondary" icon={ArrowLeft}>
                            {t`До документа`}
                        </ButtonLink>
                    </EmptyState>
                </Card>
            </>
        );
    }

    return <DocumentForm key={id ?? "new"} existing={existing.data ?? null} units={units.data ?? []} />;
}

/**
 * Props of {@link DocumentForm}.
 */
interface DocumentFormProps {
    /** The document being changed, or null for a new one. */
    readonly existing: DocumentEditModel | null;
    /** The units the user may register documents in. */
    readonly units: readonly UnitOption[];
}

/**
 * The editor form.
 * @param props The props.
 * @returns The form.
 */
function DocumentForm({ existing, units }: DocumentFormProps) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [file, setFile] = useState<File | null>(null);
    const [signers, setSigners] = useState<readonly UserLookupItem[]>(existing?.signers ?? []);
    const [term, setTerm] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState<"submit" | "draft" | null>(null);
    const suggestions = useQuery(signerSearchQuery(term));

    // A unit the author no longer belongs to stays available for the document that already has it.
    const unitOptions = existing && !units.some((unit) => unit.id === existing.unit.id) ? [existing.unit, ...units] : units;

    const schema = z.object({
        title: z
            .string()
            .trim()
            .min(1, t`Вкажіть назву документа.`)
            .max(300, t`Назва не може бути довшою за 300 символів.`),
        description: z.string().max(4000, t`Опис не може бути довшим за 4000 символів.`),
        kind: z.string(),
        unitId: z.string().min(1, t`Оберіть підрозділ.`),
        mode: z.enum(["Sequential", "Parallel"]),
    });

    const { register, control, handleSubmit, formState, getValues, setValue } = useForm({
        resolver: zodResolver(schema),
        defaultValues: {
            title: existing?.title ?? "",
            description: existing?.description ?? "",
            kind: existing?.kind ?? "Memo",
            unitId: existing?.unit.id ?? (units.length === 1 ? (units[0]?.id ?? "") : ""),
            mode: (existing?.signingMode ?? "Sequential") as SigningMode,
        },
    });

    const signedBy = new Set(existing?.signedBy ?? []);
    const kept = file === null && existing ? signers.filter((signer) => signedBy.has(signer.id)).map((signer) => signer.id) : [];
    const toRemove = existing
        ? file !== null
            ? existing.signedBy.length
            : existing.signedBy.filter((userId) => !signers.some((s) => s.id === userId)).length
        : 0;

    /**
     * Takes a chosen file after checking its type and size.
     * @param files The chosen files.
     */
    function choose(files: readonly File[]): void {
        setError(null);
        const chosen = files[0];
        if (!chosen) {
            return;
        }

        const extension = chosen.name.slice(chosen.name.lastIndexOf(".")).toLowerCase();
        if (!accept.split(",").includes(extension)) {
            setError(t`Цей тип файлу не підтримується.`);
            setFile(null);
            return;
        }

        if (chosen.size > maxFileBytes) {
            setError(t`Розмір файлу не може перевищувати 25 МБ.`);
            setFile(null);
            return;
        }

        setFile(chosen);
        if (!getValues("title").trim()) {
            setValue("title", chosen.name.replace(/\.[^.]+$/, ""), { shouldValidate: true });
        }
    }

    /**
     * Moves a signer up or down the route.
     * @param index The signer.
     * @param offset -1 or 1.
     */
    function move(index: number, offset: -1 | 1): void {
        const target = index + offset;
        const next = [...signers];
        const [a, b] = [next[index], next[target]];
        if (a && b) {
            next[index] = b;
            next[target] = a;
            setSigners(next);
        }
    }

    /**
     * Saves the document, optionally sending it to signing.
     * @param values The form values.
     * @param submit Whether to send it to signing.
     */
    async function save(values: z.infer<typeof schema>, submit: boolean): Promise<void> {
        if (!existing && !file) {
            setError(t`Оберіть файл документа.`);
            return;
        }

        setSubmitting(submit ? "submit" : "draft");
        setError(null);
        const form = {
            title: values.title,
            description: values.description,
            kind: values.kind as DocumentKind,
            unitId: values.unitId,
            mode: values.mode,
            signerIds: signers.map((signer) => signer.id),
            submit: submit && signers.length > 0,
            file,
        };
        try {
            const documentId = existing ? (await documentsApi.update(existing.id, form), existing.id) : await documentsApi.create(form);
            await queryClient.invalidateQueries({ queryKey: documentKeys.all });
            toast(
                existing
                    ? form.submit
                        ? t`Зміни збережено, документ надіслано на підписання`
                        : t`Зміни збережено`
                    : form.submit
                      ? t`Документ створено й надіслано на підпис`
                      : t`Чернетку збережено`,
            );
            await navigate(`/documents/${documentId}`, { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зберегти документ. Спробуйте ще раз.`));
        } finally {
            setSubmitting(null);
        }
    }

    const number = existing?.registrationNumber ?? "";
    const busy = submitting !== null;

    return (
        <>
            {existing ? (
                <PageHeader
                    title={t`Редагування документа`}
                    subtitle={t`Змініть файл, реквізити або маршрут і збережіть документ або надішліть його на підпис.`}
                    breadcrumbs={[
                        { text: t`Документи`, to: "/documents" },
                        { text: t`№ ${number}`, to: `/documents/${existing.id}` },
                        { text: t`Редагування` },
                    ]}
                />
            ) : (
                <PageHeader
                    title={t`Новий документ`}
                    subtitle={t`Завантажте файл, вкажіть реквізити та сформуйте маршрут підписання.`}
                    breadcrumbs={[{ text: t`Документи`, to: "/documents" }, { text: t`Новий документ` }]}
                />
            )}

            <form className={layout.split} noValidate onSubmit={(event) => event.preventDefault()}>
                <div className={layout.stackLg}>
                    {existing?.status === "Rejected" && (
                        <Alert tone="outline" icon={Info} title={t`Документ відхилено`}>
                            {t`Виправте його та надішліть на підпис повторно. Підписи, накладені на поточний файл, збережуться — ці підписанти не підписуватимуть знову, якщо ви не заміните файл і не приберете їх із маршруту.`}
                        </Alert>
                    )}

                    {error && <Alert tone="danger">{error}</Alert>}

                    <Card title={t`Файл`}>
                        {existing && (
                            <div className={styles.file}>
                                <span className={styles.fileIcon}>
                                    <Icon icon={FileText} />
                                </span>
                                <span className={styles.fileText}>
                                    <span className={styles.fileName}>{file?.name ?? existing.file.fileName}</span>
                                    <span className={[layout.textSm, layout.muted].join(" ")}>
                                        {file
                                            ? t`Новий файл · ${formatFileSize(file.size)}`
                                            : t`Поточний файл · ${formatFileSize(existing.file.size)}`}
                                    </span>
                                </span>
                            </div>
                        )}
                        <FileDrop
                            icon={FileUp}
                            accept={accept}
                            title={existing ? t`Замінити файл` : (file?.name ?? t`Перетягніть файл або натисніть, щоб обрати`)}
                            hint={
                                file && !existing
                                    ? formatFileSize(file.size)
                                    : t`PDF, DOCX, XLSX, ODT та інші · до 25 МБ. Для PAdES оберіть PDF.`
                            }
                            onFiles={choose}
                        />
                        {toRemove > 0 && (
                            <Alert tone="danger" icon={TriangleAlert} title={t`Частину підписів буде анульовано`}>
                                {file
                                    ? t`Підписи накладено на попередній файл. Після збереження всі ${toRemove} підписи буде видалено, а підписанти підписуватимуть новий файл.`
                                    : t`Підписи тих, кого ви прибрали з маршруту (${toRemove}), буде видалено після збереження.`}
                            </Alert>
                        )}
                    </Card>

                    <Card title={t`Реквізити`}>
                        <div className={styles.formGrid}>
                            <Field label={t`Назва`} required error={formState.errors.title?.message} className={styles.full}>
                                {(ids) => (
                                    <TextInput
                                        {...ids}
                                        {...register("title")}
                                        maxLength={300}
                                        placeholder={t`Наприклад: Про затвердження графіка відпусток`}
                                    />
                                )}
                            </Field>
                            <Field label={t`Вид документа`} required>
                                {(ids) => (
                                    <Controller
                                        control={control}
                                        name="kind"
                                        render={({ field }) => (
                                            <Select
                                                {...ids}
                                                ref={field.ref}
                                                value={field.value}
                                                onChange={field.onChange}
                                                onBlur={field.onBlur}
                                                options={kinds.map((kind) => ({ value: kind, label: t(documentKindNames[kind]) }))}
                                            />
                                        )}
                                    />
                                )}
                            </Field>
                            <Field label={t`Підрозділ`} required error={formState.errors.unitId?.message}>
                                {(ids) => (
                                    <Controller
                                        control={control}
                                        name="unitId"
                                        render={({ field }) => (
                                            <Select
                                                {...ids}
                                                ref={field.ref}
                                                value={field.value}
                                                onChange={field.onChange}
                                                onBlur={field.onBlur}
                                                placeholder={t`Оберіть підрозділ`}
                                                options={unitOptions.map((unit) => ({
                                                    value: unit.id,
                                                    label: unit.name,
                                                    depth: unit.depth,
                                                }))}
                                            />
                                        )}
                                    />
                                )}
                            </Field>
                            <Field
                                label={t`Опис`}
                                hint={t`Короткий зміст або коментар для підписантів.`}
                                error={formState.errors.description?.message}
                                className={styles.full}
                            >
                                {(ids) => <Textarea {...ids} {...register("description")} maxLength={4000} />}
                            </Field>
                        </div>
                    </Card>
                </div>

                <div className={layout.stackLg}>
                    <Card title={t`Маршрут підписання`} subtitle={t`Додайте підписантів у потрібному порядку.`}>
                        <Controller
                            control={control}
                            name="mode"
                            render={({ field }) => (
                                <Segmented
                                    label={t`Порядок підписання`}
                                    value={field.value}
                                    onChange={field.onChange}
                                    options={[
                                        { value: "Sequential", label: t(signingModeNames.Sequential), hint: t`один за одним` },
                                        { value: "Parallel", label: t(signingModeNames.Parallel), hint: t`у будь-якому порядку` },
                                    ]}
                                />
                            )}
                        />

                        <Field label={t`Знайти працівника`}>
                            {(ids) => (
                                <SearchInput id={ids.id} value={term} placeholder={t`Прізвище, ім'я або e-mail`} onSearch={setTerm} />
                            )}
                        </Field>
                        {term.length >= 2 && suggestions.data && (
                            <ul className={styles.results}>
                                {suggestions.data
                                    .filter((user) => !signers.some((signer) => signer.id === user.id))
                                    .map((user) => (
                                        <li key={user.id}>
                                            <button
                                                type="button"
                                                onClick={() => {
                                                    setSigners([...signers, user]);
                                                    setTerm("");
                                                }}
                                            >
                                                <PersonName
                                                    name={user.fullName}
                                                    photoId={user.photoId}
                                                    meta={user.position ?? user.email}
                                                />
                                            </button>
                                        </li>
                                    ))}
                            </ul>
                        )}

                        {signers.length === 0 ? (
                            <EmptyState
                                icon={Users}
                                title={t`Підписантів ще немає`}
                                text={t`Документ без підписантів можна зберегти лише як чернетку.`}
                            />
                        ) : (
                            <ol className={styles.route}>
                                {signers.map((signer, index) => (
                                    <li key={signer.id} className={styles.routeItem}>
                                        <span className={styles.routeOrder}>{index + 1}</span>
                                        <Avatar name={signer.fullName} photoId={signer.photoId} size="sm" decorative />
                                        <span className={styles.routeBody}>
                                            <span className={layout.strong}>{signer.fullName}</span>
                                            <span className={[layout.textSm, layout.muted].join(" ")}>
                                                {signer.position ?? signer.email}
                                            </span>
                                            {kept.includes(signer.id) && (
                                                <Badge tone="inverse" icon={BadgeCheck}>
                                                    {t`Підпис збережеться`}
                                                </Badge>
                                            )}
                                        </span>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            iconOnly
                                            icon={ChevronUp}
                                            disabled={index === 0}
                                            onClick={() => move(index, -1)}
                                        >
                                            {t`Вище`}
                                        </Button>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            iconOnly
                                            icon={ChevronDown}
                                            disabled={index === signers.length - 1}
                                            onClick={() => move(index, 1)}
                                        >
                                            {t`Нижче`}
                                        </Button>
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            iconOnly
                                            icon={X}
                                            onClick={() => setSigners(signers.filter((item) => item.id !== signer.id))}
                                        >
                                            {t`Прибрати`}
                                        </Button>
                                    </li>
                                ))}
                            </ol>
                        )}
                    </Card>

                    <Card variant="muted">
                        <div className={layout.stack}>
                            <Button
                                block
                                icon={Send}
                                busy={submitting === "submit"}
                                disabled={busy || signers.length === 0}
                                onClick={handleSubmit((values) => save(values, true))}
                            >
                                {existing ? t`Зберегти й надіслати на підпис` : t`Створити й надіслати на підпис`}
                            </Button>
                            <Button
                                variant="secondary"
                                block
                                icon={Save}
                                busy={submitting === "draft"}
                                disabled={busy}
                                onClick={handleSubmit((values) => save(values, false))}
                            >
                                {existing ? t`Зберегти зміни` : t`Зберегти чернетку`}
                            </Button>
                            {existing && (
                                <ButtonLink to={`/documents/${existing.id}`} variant="ghost" block icon={ArrowLeft}>
                                    {t`Скасувати`}
                                </ButtonLink>
                            )}
                            {busy && <Progress />}
                        </div>
                    </Card>
                </div>
            </form>
        </>
    );
}
