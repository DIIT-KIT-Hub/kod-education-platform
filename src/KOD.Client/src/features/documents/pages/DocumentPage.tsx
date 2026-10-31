import { zodResolver } from "@hookform/resolvers/zod";
import { Trans, useLingui } from "@lingui/react/macro";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Archive,
    ArrowLeft,
    BadgeCheck,
    Ban,
    Download,
    FileText,
    FileX,
    Pencil,
    Send,
    Signature,
    Trash2,
    Undo2,
    Users,
} from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router";
import { z } from "zod";
import { describeError } from "@/shared/api/problem";
import type { DocumentDetails, DocumentSignerItem } from "@/shared/api/schema";
import { documentKindNames, formatNames, packagingNames, signerStatusNames, signingModeNames } from "@/shared/i18n/display";
import { formatDateTime, formatFileSize } from "@/shared/i18n/format";
import { SigningWizard } from "@/shared/signing/SigningWizard";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Badge } from "@/shared/ui/Badge";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { Card } from "@/shared/ui/Card";
import { confirm } from "@/shared/ui/ConfirmHost";
import { DescriptionList } from "@/shared/ui/DescriptionList";
import { Dialog } from "@/shared/ui/Dialog";
import { EmptyState } from "@/shared/ui/EmptyState";
import { Field } from "@/shared/ui/Field";
import { Icon } from "@/shared/ui/Icon";
import { PageHeader } from "@/shared/ui/PageHeader";
import { PersonName } from "@/shared/ui/PersonName";
import { Spinner } from "@/shared/ui/Spinner";
import { Textarea } from "@/shared/ui/Textarea";
import { Timeline, TimelineItem, type TimelineState } from "@/shared/ui/Timeline";
import { toast } from "@/shared/ui/Toast";
import { documentKeys, documentQuery, documentsApi } from "../api";
import { DocumentStatusBadge } from "../components/DocumentStatusBadge";
import styles from "./documents.module.css";

/**
 * A document: its file and signed versions, details, signatures and the signing route, with the actions the user may
 * take (sign, reject, edit, submit, withdraw, archive, delete).
 * @returns The page.
 */
export function DocumentPage() {
    const { t } = useLingui();
    const { id = "" } = useParams();
    const document = useQuery(documentQuery(id));

    if (document.isPending) {
        return <Spinner block />;
    }

    if (document.isError) {
        return (
            <>
                <PageHeader title={t`Документ`} breadcrumbs={[{ text: t`Документи`, to: "/documents" }, { text: t`Документ` }]} />
                <Card>
                    <EmptyState
                        icon={FileX}
                        title={t`Документ не знайдено`}
                        text={describeError(document.error, t`Документ не існує або у вас немає до нього доступу.`)}
                    >
                        <ButtonLink to="/documents" icon={ArrowLeft}>
                            {t`До реєстру`}
                        </ButtonLink>
                    </EmptyState>
                </Card>
            </>
        );
    }

    return <DocumentView document={document.data} />;
}

/**
 * The loaded document.
 * @param props The document.
 * @returns The view.
 */
function DocumentView({ document }: { readonly document: DocumentDetails }) {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [signing, setSigning] = useState(false);
    const [signingBusy, setSigningBusy] = useState(false);
    const [signingRound, setSigningRound] = useState(0);
    const [rejecting, setRejecting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const actions = document.actions;
    const number = document.registrationNumber;

    /**
     * Runs a change after a confirmation, then reloads the document.
     * @param question The question.
     * @param change The call.
     * @param done The notification.
     */
    async function change(question: string, change: () => Promise<unknown>, done: string): Promise<void> {
        if (!(await confirm({ title: question }))) {
            return;
        }

        setError(null);
        try {
            await change();
            await queryClient.invalidateQueries({ queryKey: documentKeys.all });
            toast(done);
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
        }
    }

    /**
     * Deletes the document after a confirmation.
     */
    async function remove(): Promise<void> {
        if (!(await confirm({ title: t`Видалити документ остаточно?`, confirmText: t`Видалити`, danger: true }))) {
            return;
        }

        try {
            await documentsApi.remove(document.id);
            queryClient.removeQueries({ queryKey: documentKeys.details(document.id) });
            await queryClient.invalidateQueries({ queryKey: documentKeys.all });
            toast(t`Документ видалено`);
            await navigate("/documents?scope=Authored", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
        }
    }

    /**
     * The state of a signer on the timeline.
     * @param signer The signer.
     * @returns The state.
     */
    const stateOf = (signer: DocumentSignerItem): TimelineState =>
        signer.status === "Signed"
            ? "done"
            : signer.status === "Rejected"
              ? "failed"
              : document.status === "InSigning"
                ? "current"
                : "pending";

    return (
        <>
            <PageHeader
                title={document.title}
                breadcrumbs={[{ text: t`Документи`, to: "/documents" }, { text: t`№ ${number}` }]}
                actions={
                    <>
                        {actions.canSign && (
                            <Button icon={Signature} onClick={() => setSigning(true)}>
                                {t`Підписати`}
                            </Button>
                        )}
                        {actions.canReject && (
                            <Button variant="secondary" icon={Ban} onClick={() => setRejecting(true)}>
                                {t`Відмовити`}
                            </Button>
                        )}
                        {actions.canEdit && (
                            <ButtonLink to={`/documents/${document.id}/edit`} variant="secondary" icon={Pencil}>
                                {t`Редагувати`}
                            </ButtonLink>
                        )}
                        {actions.canSubmit && (
                            <Button
                                icon={Send}
                                onClick={() =>
                                    void change(
                                        t`Надіслати документ на підписання?`,
                                        () => documentsApi.setStatus(document.id, "InSigning"),
                                        t`Документ надіслано на підписання`,
                                    )
                                }
                            >
                                {t`На підписання`}
                            </Button>
                        )}
                        {actions.canWithdraw && (
                            <Button
                                variant="secondary"
                                icon={Undo2}
                                onClick={() =>
                                    void change(
                                        t`Відкликати документ у чернетки?`,
                                        () => documentsApi.setStatus(document.id, "Draft"),
                                        t`Документ повернуто в чернетки`,
                                    )
                                }
                            >
                                {t`Відкликати`}
                            </Button>
                        )}
                        {actions.canArchive && (
                            <Button
                                variant="secondary"
                                icon={Archive}
                                onClick={() =>
                                    void change(
                                        t`Перемістити документ в архів?`,
                                        () => documentsApi.setStatus(document.id, "Archived"),
                                        t`Документ переміщено в архів`,
                                    )
                                }
                            >
                                {t`В архів`}
                            </Button>
                        )}
                        {actions.canDelete && (
                            <Button variant="ghost" iconOnly icon={Trash2} onClick={() => void remove()}>
                                {t`Видалити`}
                            </Button>
                        )}
                    </>
                }
            >
                <div className={layout.cluster}>
                    <DocumentStatusBadge status={document.status} />
                    <Badge>{t(documentKindNames[document.kind])}</Badge>
                    <Badge tone="dashed">{t`№ ${number}`}</Badge>
                </div>
            </PageHeader>

            {error && <Alert tone="danger">{error}</Alert>}

            <div className={layout.split}>
                <div className={[layout.stackLg, layout.stagger].join(" ")}>
                    {actions.canSign && (
                        <Alert tone="inverse" icon={Signature} title={t`Документ очікує вашого підпису`}>
                            {t`Перевірте файл і натисніть «Підписати». Ключ і пароль використовуються лише у вашому браузері.`}
                        </Alert>
                    )}

                    {document.status === "Rejected" && actions.canEdit && (
                        <Alert tone="outline" icon={Ban} title={t`Документ відхилено`}>
                            {t`Причину вказано в маршруті підписання. Натисніть «Редагувати», щоб виправити файл, реквізити чи маршрут, або «На підписання», щоб надіслати документ повторно без змін. Ті, хто вже підписав поточний файл, не підписуватимуть його знову.`}
                        </Alert>
                    )}

                    <Card title={t`Файл документа`}>
                        <div className={styles.file}>
                            <span className={styles.fileIcon}>
                                <Icon icon={FileText} />
                            </span>
                            <span className={styles.fileText}>
                                <span className={styles.fileName}>{document.file.fileName}</span>
                                <span className={[layout.textSm, layout.muted].join(" ")}>
                                    {formatFileSize(document.file.size)} · {document.file.contentType}
                                </span>
                            </span>
                            <ButtonLink
                                href={`/api/v1/documents/${document.id}/file`}
                                download=""
                                variant="secondary"
                                size="sm"
                                icon={Download}
                            >
                                {t`Завантажити`}
                            </ButtonLink>
                        </div>
                        <p className={styles.hash}>
                            SHA-256: <span className={layout.mono}>{document.file.sha256}</span>
                        </p>
                        {document.combinedSignatureId ? (
                            <div className={[styles.file, styles.signed].join(" ")}>
                                <span className={styles.fileIcon}>
                                    <Icon icon={BadgeCheck} />
                                </span>
                                <span className={styles.fileText}>
                                    <span className={styles.fileName}>{t`Документ з усіма підписами`}</span>
                                    <span
                                        className={[layout.textSm, layout.muted].join(" ")}
                                    >{t`Один файл, що містить документ і всі накладені підписи.`}</span>
                                </span>
                                <ButtonLink
                                    href={`/api/v1/documents/${document.id}/signatures/${document.combinedSignatureId}/file`}
                                    download=""
                                    size="sm"
                                    icon={Download}
                                >
                                    {t`Завантажити`}
                                </ButtonLink>
                            </div>
                        ) : (
                            document.signatures.length > 0 && (
                                <p
                                    className={[layout.textSm, layout.muted].join(" ")}
                                >{t`Це оригінал без підписів. Підписані файли завантажуйте в розділі «Підписи».`}</p>
                            )
                        )}
                    </Card>

                    <Card title={t`Відомості`}>
                        <DescriptionList
                            items={[
                                [t`Реєстраційний номер`, number],
                                [t`Вид`, t(documentKindNames[document.kind])],
                                [
                                    t`Підрозділ`,
                                    <a key="unit" className={layout.link} href={`/structure/${document.unit.id}`}>
                                        {document.unit.name}
                                    </a>,
                                ],
                                [t`Автор`, <PersonName key="author" name={document.author.fullName} photoId={document.author.photoId} />],
                                [t`Порядок підписання`, t(signingModeNames[document.signingMode])],
                                [t`Створено`, formatDateTime(document.createdAt)],
                                [t`Надіслано на підпис`, formatDateTime(document.submittedAt)],
                                [t`Завершено`, formatDateTime(document.completedAt)],
                            ]}
                        />
                        {document.description && <p className={styles.description}>{document.description}</p>}
                    </Card>

                    <Card title={t`Підписи`} subtitle={t`Кваліфіковані електронні підписи, накладені на документ`}>
                        {document.signatures.length === 0 ? (
                            <EmptyState icon={Signature} title={t`Підписів ще немає`} />
                        ) : (
                            <>
                                <ul className={styles.route}>
                                    {document.signatures.map((signature) => (
                                        <li key={signature.id} className={styles.file}>
                                            <span className={styles.signatureIcon}>
                                                <Icon icon={BadgeCheck} />
                                            </span>
                                            <span className={styles.fileText}>
                                                <span className={styles.fileName}>{signature.certificate.subjectFullName}</span>
                                                <span className={[layout.textSm, layout.muted].join(" ")}>
                                                    {formatNames[signature.format]}
                                                    {signature.level ? ` ${signature.level}` : ""} ·{" "}
                                                    {t(packagingNames[signature.packaging])} · {formatDateTime(signature.signedAt)}
                                                </span>
                                                <span className={[layout.textSm, layout.muted].join(" ")}>
                                                    {signature.certificate.issuerName} · {t`сертифікат №`}{" "}
                                                    <span className={layout.mono}>{signature.certificate.serialNumber}</span>
                                                    {signature.certificate.subjectTaxId && ` · ${signature.certificate.subjectTaxId}`}
                                                </span>
                                            </span>
                                            <ButtonLink
                                                href={`/api/v1/documents/${document.id}/signatures/${signature.id}/file`}
                                                download=""
                                                variant="secondary"
                                                size="sm"
                                                icon={Download}
                                            >
                                                {document.combinedSignatureId
                                                    ? t`Версія після цього підпису`
                                                    : signature.format === "PAdES"
                                                      ? t`Підписаний PDF`
                                                      : t`Файл підпису`}
                                            </ButtonLink>
                                        </li>
                                    ))}
                                </ul>
                                <p className={[layout.textXs, layout.muted].join(" ")}>
                                    <Trans>
                                        Перевірити будь-який підпис можна на сторінці{" "}
                                        <Link className={layout.link} to="/signing/verify">
                                            «Перевірка підпису»
                                        </Link>
                                        .
                                    </Trans>
                                </p>
                            </>
                        )}
                    </Card>
                </div>

                <Card title={t`Маршрут підписання`} subtitle={t(signingModeNames[document.signingMode])}>
                    {document.signers.length === 0 ? (
                        <EmptyState icon={Users} title={t`Підписантів не додано`} />
                    ) : (
                        <Timeline>
                            {document.signers.map((signer) => (
                                <TimelineItem key={signer.userId} state={stateOf(signer)} marker={signer.order}>
                                    <PersonName
                                        name={signer.fullName}
                                        photoId={signer.photoId}
                                        meta={signer.position ?? t(signerStatusNames[signer.status])}
                                    />
                                    <span className={[layout.textSm, layout.muted].join(" ")}>
                                        {t(signerStatusNames[signer.status])}
                                        {signer.decidedAt && ` · ${formatDateTime(signer.decidedAt)}`}
                                    </span>
                                    {signer.comment && (
                                        <Alert tone="danger" icon={Ban}>
                                            {signer.comment}
                                        </Alert>
                                    )}
                                </TimelineItem>
                            ))}
                        </Timeline>
                    )}
                </Card>
            </div>

            {actions.canSign && (
                <Dialog
                    open={signing}
                    wide
                    locked={signingBusy}
                    onClose={() => setSigning(false)}
                    title={t`Підписання документа`}
                    description={`${t`№ ${number}`} · ${document.file.fileName}`}
                >
                    <SigningWizard
                        key={signingRound}
                        active={signing}
                        onBusyChange={setSigningBusy}
                        target={{
                            kind: "document",
                            documentId: document.id,
                            title: document.title,
                            fileName: document.file.fileName,
                            sha256: document.file.sha256,
                        }}
                        onSigned={() => {
                            toast(t`Документ підписано`);
                            void queryClient.invalidateQueries({ queryKey: documentKeys.all });
                            setTimeout(() => {
                                setSigning(false);
                                setSigningRound((value) => value + 1);
                            }, 1400);
                        }}
                    />
                </Dialog>
            )}

            {actions.canReject && (
                <RejectDialog
                    open={rejecting}
                    documentId={document.id}
                    onClose={() => setRejecting(false)}
                    onRejected={async () => {
                        setRejecting(false);
                        await queryClient.invalidateQueries({ queryKey: documentKeys.all });
                        toast(t`Відмову зафіксовано`);
                    }}
                />
            )}
        </>
    );
}

/**
 * Props of {@link RejectDialog}.
 */
interface RejectDialogProps {
    readonly open: boolean;
    readonly documentId: string;
    readonly onClose: () => void;
    readonly onRejected: () => Promise<void>;
}

/**
 * Asks for the reason of a refusal to sign.
 * @param props The props.
 * @returns The dialog.
 */
function RejectDialog({ open, documentId, onClose, onRejected }: RejectDialogProps) {
    const { t } = useLingui();
    const [error, setError] = useState<string | null>(null);
    const schema = z.object({
        reason: z
            .string()
            .trim()
            .min(1, t`Вкажіть причину відмови.`)
            .max(1000, t`Причина не може бути довшою за 1000 символів.`),
    });
    const { register, handleSubmit, formState, reset } = useForm({ resolver: zodResolver(schema), defaultValues: { reason: "" } });

    /**
     * Sends the refusal.
     * @param values The form values.
     */
    async function reject(values: z.infer<typeof schema>): Promise<void> {
        setError(null);
        try {
            await documentsApi.reject(documentId, values.reason);
            reset();
            await onRejected();
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося виконати дію. Спробуйте ще раз.`));
        }
    }

    return (
        <Dialog
            open={open}
            onClose={onClose}
            title={t`Відмова в підписанні`}
            description={t`Автор побачить причину й зможе виправити документ.`}
        >
            <form className={layout.stack} noValidate onSubmit={handleSubmit(reject)}>
                {error && <Alert tone="danger">{error}</Alert>}
                <Field label={t`Причина`} required error={formState.errors.reason?.message}>
                    {(control) => <Textarea {...control} {...register("reason")} maxLength={1000} />}
                </Field>
                <div className={layout.cluster}>
                    <Button variant="secondary" onClick={onClose}>
                        {t`Скасувати`}
                    </Button>
                    <Button type="submit" variant="danger" icon={Ban} busy={formState.isSubmitting}>
                        {t`Відмовити`}
                    </Button>
                </div>
            </form>
        </Dialog>
    );
}
