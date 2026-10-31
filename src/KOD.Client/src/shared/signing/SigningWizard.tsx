import { useLingui } from "@lingui/react/macro";
import { BadgeCheck, Download, FileText, RefreshCw, Signature } from "lucide-react";
import { useEffect, useEffectEvent, useRef, useState, type ReactNode } from "react";
import { api } from "@/shared/api/client";
import { ApiError, describeError, unwrap } from "@/shared/api/problem";
import type { SigningSession } from "@/shared/api/schema";
import { formatDateTime, formatFileSize } from "@/shared/i18n/format";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { DescriptionList } from "@/shared/ui/DescriptionList";
import { Field } from "@/shared/ui/Field";
import { FileDrop } from "@/shared/ui/FileDrop";
import { Icon } from "@/shared/ui/Icon";
import { Progress } from "@/shared/ui/Progress";
import { Segmented } from "@/shared/ui/Segmented";
import { Select } from "@/shared/ui/Select";
import { Wizard } from "@/shared/ui/Wizard";
import { SigningClient, type KeyOwner, type SignFormat, type SignLevel, type SignPackaging, type SignResult } from "./client";
import {
    availableFormats,
    effectivePackaging,
    formats,
    levels,
    maxDataBytes,
    packagings,
    sha256,
    signatureFileName,
    type FixedParameters,
} from "./formats";
import { KeyPanel, OwnerCard } from "./KeyPanel";
import styles from "./Signing.module.css";

/**
 * What is signed: a stored document (its file is downloaded and checked, the signature is uploaded) or local files
 * (nothing leaves the device; the result is offered for download).
 */
export type SigningTarget =
    | { readonly kind: "files" }
    | {
          readonly kind: "document";
          readonly documentId: string;
          readonly title: string;
          readonly fileName: string;
          /** The SHA-256 the server registered for the file. */
          readonly sha256: string;
      };

/**
 * Props of {@link SigningWizard}.
 */
export interface SigningWizardProps {
    /** What is signed. */
    readonly target: SigningTarget;
    /** Whether the panel is shown (the signing dialog is open): the turn to sign is held only while it is. */
    readonly active: boolean;
    /** Called while the signature is being made and uploaded (the dialog must not close then). */
    readonly onBusyChange?: (busy: boolean) => void;
    /** Called after a document signature was stored. */
    readonly onSigned?: () => void;
}

/**
 * A file to sign.
 */
interface DataFile {
    readonly name: string;
    readonly size: number;
    /** A local file (read when signing). */
    readonly file?: File;
    /** The downloaded document. */
    readonly buffer?: ArrayBuffer;
}

/**
 * The state of the data step of a document.
 */
type DataState =
    | { readonly kind: "idle" }
    | { readonly kind: "loading" }
    | { readonly kind: "waiting"; readonly detail: string }
    | { readonly kind: "ready"; readonly signatures: number | null }
    | { readonly kind: "error"; readonly message: string };

/**
 * A signature of local files, offered for download.
 */
interface FilesResult {
    readonly signed: SignResult;
    readonly fileName: string;
    readonly summary: string;
}

/**
 * Waits.
 * @param ms Milliseconds.
 * @returns Resolves after the delay.
 */
function delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Downloads a document file or one of its signed versions.
 * @param url The API address.
 * @returns The bytes.
 */
async function download(url: string): Promise<ArrayBuffer> {
    const response = await fetch(url, { credentials: "same-origin" });
    if (!response.ok) {
        throw new ApiError(response.status, String(response.status), "");
    }

    return response.arrayBuffer();
}

/**
 * The signing wizard: 1) the data, 2) the signature parameters, 3) the key and the signature. The key is read only on
 * the last step, right before signing, so the private key spends as little time in memory as possible; it is released
 * after signing, when the user goes back, when the panel is hidden and when the page goes away. The key file and the
 * password go only to the Web Worker.
 *
 * For a document the browser checks the SHA-256 of the file against the registered one, takes the turn to sign
 * (waiting while another signer of a parallel route holds it), adds the signature to the newest signed version in the
 * cumulative formats and renews the turn right before signing.
 * @param props The props.
 * @returns The wizard.
 */
export function SigningWizard({ target, active, onBusyChange, onSigned }: SigningWizardProps) {
    const { t } = useLingui();
    const [client] = useState(() => new SigningClient());
    const documentMode = target.kind === "document";
    const [round, setRound] = useState(0);
    const [files, setFiles] = useState<readonly DataFile[]>([]);
    const [filesError, setFilesError] = useState<string | null>(null);
    const [data, setData] = useState<DataState>({ kind: documentMode ? "loading" : "idle" });
    const [session, setSession] = useState<SigningSession | null>(null);
    const previous = useRef<ArrayBuffer | null>(null);
    const attempt = useRef(0);
    const shown = useRef(active);
    const [format, setFormat] = useState<SignFormat>("CAdES");
    const [packaging, setPackaging] = useState<SignPackaging>("Detached");
    const [level, setLevel] = useState<SignLevel>("B-LT");
    const [owner, setOwner] = useState<KeyOwner | null>(null);
    const [keyRound, setKeyRound] = useState(0);
    const [signError, setSignError] = useState<string | null>(null);
    const [libraryError, setLibraryError] = useState<string | null>(null);
    const [filesResult, setFilesResult] = useState<FilesResult | null>(null);
    const [documentSigned, setDocumentSigned] = useState(false);

    const names = files.map((file) => file.name);
    const fixed: FixedParameters | null =
        documentMode && session?.format ? { format: session.format, packaging: session.packaging ?? "Enveloped" } : null;
    const available = availableFormats(names, fixed);
    const chosenFormat = available.includes(format) ? format : (available[0] ?? "CAdES");
    const chosenPackaging = fixed ? fixed.packaging : effectivePackaging(chosenFormat, packaging);
    const formatInfo = formats.find((info) => info.id === chosenFormat);
    const integrityOk = !documentMode || data.kind === "ready";
    const packagingTitle = packagings.find((item) => item.id === chosenPackaging)?.title;
    const summaryFormat =
        formatInfo?.packaging && packagingTitle
            ? `${formatInfo.title} · ${t(packagingTitle).toLowerCase()}`
            : (formatInfo?.title ?? chosenFormat);
    const levelTitle = levels.find((item) => item.id === level)?.title;
    const summaryLevel = levelTitle ? t(levelTitle) : level;

    /**
     * Releases the key in the worker and clears the key form.
     */
    function forget(): void {
        setOwner(null);
        setKeyRound((value) => value + 1);
        client.resetKey().catch(() => undefined);
    }

    /**
     * Takes the turn to sign and loads the version to add the signature to. While another signer holds the turn, waits
     * and asks again every few seconds (only while the panel is shown).
     */
    async function prepareSession(): Promise<void> {
        if (target.kind !== "document") {
            return;
        }

        const current = ++attempt.current;
        setSession(null);
        setData({ kind: "loading" });
        let fresh: SigningSession;
        for (;;) {
            try {
                fresh = await unwrap(api.POST("/api/v1/documents/{id}/signing-session", { params: { path: { id: target.documentId } } }));
                break;
            } catch (failure) {
                if (current !== attempt.current) {
                    return;
                }

                if (failure instanceof ApiError && failure.code === "signing_turn") {
                    setData({ kind: "waiting", detail: failure.detail });
                    await delay(5000);
                    if (current !== attempt.current || !shown.current) {
                        return;
                    }

                    continue;
                }

                throw failure;
            }
        }

        if (current !== attempt.current) {
            return;
        }

        previous.current = null;
        if (fresh.baseSignatureId) {
            const buffer = await download(`/api/v1/documents/${target.documentId}/signatures/${fresh.baseSignatureId}/file`).catch(() => {
                throw new Error(t`Не вдалося завантажити останню підписану версію документа.`);
            });
            if ((await sha256(buffer)) !== (fresh.baseSha256 ?? "").toLowerCase()) {
                throw new Error(t`Контрольна сума останньої підписаної версії не збігається з зареєстрованою. Підписання заборонено.`);
            }

            previous.current = buffer;
        }

        if (current === attempt.current) {
            setSession(fresh);
            setData({ kind: "ready", signatures: fresh.baseSignatureId ? fresh.signatureCount : null });
        }
    }

    /**
     * Gives up the turn to sign (the panel was hidden without signing).
     */
    function endSession(): void {
        attempt.current++;
        if (target.kind === "document" && session) {
            setSession(null);
            void api
                .DELETE("/api/v1/documents/{id}/signing-session", { params: { path: { id: target.documentId } }, keepalive: true })
                .catch(() => undefined);
        }
    }

    /**
     * Shows the panel: loads the library and, for a document, its file (once) and the turn to sign (every time).
     */
    const activate = useEffectEvent(() => {
        shown.current = true;
        client.init().catch((failure: unknown) => setLibraryError(describeError(failure, t`Не вдалося завантажити бібліотеку підпису.`)));
        if (target.kind !== "document" || documentSigned) {
            return;
        }

        const fail = (failure: unknown) =>
            setData({
                kind: "error",
                message: failure instanceof Error && failure.message ? failure.message : t`Не вдалося розпочати підписання.`,
            });
        if (files.length > 0) {
            prepareSession().catch(fail);
            return;
        }

        download(`/api/v1/documents/${target.documentId}/file`)
            .catch(() => {
                throw new Error(t`Не вдалося завантажити файл документа.`);
            })
            .then(async (buffer) => {
                if (buffer.byteLength > maxDataBytes) {
                    throw new Error(t`Файл завеликий для підписання у браузері (понад 25 МБ).`);
                }

                if ((await sha256(buffer)) !== target.sha256.toLowerCase()) {
                    throw new Error(t`Контрольна сума файлу не збігається з зареєстрованою. Підписання заборонено.`);
                }

                setFiles([{ name: target.fileName, size: buffer.byteLength, buffer }]);
                await prepareSession();
            })
            .catch(fail);
    });

    /**
     * Hides the panel: the key and the turn to sign are released; the data and the parameters stay.
     */
    const deactivate = useEffectEvent(() => {
        shown.current = false;
        if (owner) {
            forget();
        }

        endSession();
    });

    useEffect(() => {
        if (!active) {
            return;
        }

        // Started as a task, not in the effect body: activation updates state (loading, the turn to sign).
        const timer = setTimeout(activate, 0);
        window.addEventListener("pagehide", deactivate);
        return () => {
            clearTimeout(timer);
            window.removeEventListener("pagehide", deactivate);
            deactivate();
        };
    }, [active]);

    // Leaving the page wipes every key the worker held.
    useEffect(() => () => client.dispose(), [client]);

    /**
     * Signs with the key read on the last step and, for a document, uploads the signature.
     */
    async function sign(): Promise<void> {
        setSignError(null);

        // Every precondition is checked again here instead of trusting the state of the buttons.
        if (!owner || files.length === 0 || !integrityOk) {
            setSignError(t`Не всі кроки завершено.`);
            return;
        }

        const fileName = signatureFileName(chosenFormat, names);
        let signed: SignResult;
        try {
            const payload = await Promise.all(
                files.map(async (file) => ({
                    name: file.name,
                    data: file.buffer?.slice(0) ?? (await file.file?.arrayBuffer()) ?? new ArrayBuffer(0),
                })),
            );
            let base: { data: ArrayBuffer; signatures: number } | null = null;
            if (target.kind === "document") {
                // Renew the turn and make sure nobody added a signature since the panel opened.
                const fresh = await unwrap(
                    api.POST("/api/v1/documents/{id}/signing-session", { params: { path: { id: target.documentId } } }),
                );
                if (fresh.baseSignatureId !== (session?.baseSignatureId ?? null)) {
                    await prepareSession();
                    throw new Error(
                        t`Поки ви підписували, документ підписав інший підписант. Завантажено останню версію — натисніть «Підписати» ще раз.`,
                    );
                }

                base =
                    previous.current && chosenFormat !== "XAdES"
                        ? { data: previous.current.slice(0), signatures: fresh.signatureCount }
                        : null;
            }

            await client.init();
            signed = await client.sign(chosenFormat, chosenPackaging, level, payload, base);
            if (target.kind === "document") {
                await uploadSignature(target.documentId, owner, signed, {
                    format: chosenFormat,
                    packaging: chosenPackaging,
                    level,
                    fileName,
                    baseSignatureId: base ? (session?.baseSignatureId ?? null) : null,
                });
            }
        } catch (failure) {
            setSignError(
                describeError(failure, failure instanceof Error && failure.message ? failure.message : t`Не вдалося зберегти підпис.`),
            );
            return;
        }

        if (target.kind === "files") {
            // The files stay on this device; only their names and the signature parameters go to the audit log.
            void api
                .POST("/api/v1/signing/journal", {
                    body: {
                        fileNames: names,
                        format: chosenFormat,
                        level,
                        signerName: owner.fullName || owner.commonName || null,
                        certificateSerial: owner.serial || null,
                        issuer: owner.issuer || null,
                    },
                })
                .catch(() => undefined);
        }

        forget();
        if (target.kind === "document") {
            attempt.current++;
            setSession(null);
            setDocumentSigned(true);
            onSigned?.();
            return;
        }

        setFilesResult({ signed, fileName, summary: `${summaryFormat} · ${summaryLevel} · ${formatDateTime(signed.signedAt)}` });
    }

    if (documentSigned) {
        return <SigningResult title={t`Документ підписано`} text={t`Підпис збережено.`} />;
    }

    if (filesResult) {
        return (
            <SigningResult title={t`Файл підписано`} text={filesResult.summary}>
                <DownloadButton bytes={filesResult.signed.signature} fileName={filesResult.fileName} />
                <Button
                    variant="ghost"
                    icon={RefreshCw}
                    onClick={() => {
                        setFilesResult(null);
                        setFiles([]);
                        setRound((value) => value + 1);
                    }}
                >
                    {t`Підписати інші файли`}
                </Button>
            </SigningResult>
        );
    }

    const dataStep = documentMode ? (
        <div className={layout.stack}>
            <ul className={styles.files}>
                <li className={styles.file}>
                    <span className={styles.fileIcon}>
                        <Icon icon={FileText} />
                    </span>
                    <span className={styles.fileName}>{target.kind === "document" ? target.fileName : ""}</span>
                </li>
            </ul>
            <div className={layout.stackSm} aria-live="polite">
                {data.kind === "loading" && <Progress />}
                {data.kind === "error" && <Alert tone="danger">{data.message}</Alert>}
                {data.kind === "waiting" && (
                    <>
                        <Alert tone="outline" title={t`Зачекайте своєї черги`}>
                            {data.detail}
                        </Alert>
                        <Progress />
                    </>
                )}
                {data.kind === "ready" && (
                    <>
                        <Alert tone="outline" title={t`Цілісність підтверджено`}>
                            {t`SHA-256 файлу збігається з зареєстрованим у системі.`}
                        </Alert>
                        {data.signatures !== null && (
                            <Alert tone="outline" title={t`Підпис додається до попередніх`}>
                                {t`Документ уже має підписів: ${data.signatures}. Ваш підпис буде додано до того самого файлу, тож у ньому будуть усі підписи.`}
                            </Alert>
                        )}
                    </>
                )}
            </div>
        </div>
    ) : (
        <div className={layout.stack}>
            <FileDrop
                icon={FileText}
                multiple
                title={t`Перетягніть файли або натисніть, щоб обрати`}
                hint={t`До 25 МБ кожен. Для XAdES та ASiC-E можна обрати кілька файлів; для PAdES — PDF.`}
                onFiles={(selected) => {
                    setFilesError(null);
                    if (selected.some((file) => file.size > maxDataBytes)) {
                        setFiles([]);
                        setFilesError(t`Файли понад 25 МБ не підтримуються.`);
                        return;
                    }

                    setFiles(selected.map((file) => ({ name: file.name, size: file.size, file })));
                }}
            />
            {files.length > 0 && (
                <ul className={styles.files}>
                    {files.map((file) => (
                        <li key={file.name} className={styles.file}>
                            <span className={styles.fileIcon}>
                                <Icon icon={FileText} />
                            </span>
                            <span className={styles.fileName}>{file.name}</span>
                            <span className={styles.fileSize}>{formatFileSize(file.size)}</span>
                        </li>
                    ))}
                </ul>
            )}
            {filesError && <Alert tone="danger">{filesError}</Alert>}
        </div>
    );

    const parametersStep = (
        <div className={layout.stack}>
            <Field label={t`Формат підпису`}>
                {() => (
                    <Segmented
                        label={t`Формат підпису`}
                        value={chosenFormat}
                        onChange={setFormat}
                        options={formats.map((info) => ({
                            value: info.id,
                            label: info.title,
                            hint: t(info.hint),
                            disabled: !available.includes(info.id),
                        }))}
                    />
                )}
            </Field>
            {formatInfo?.packaging && (
                <Field label={t`Тип підпису`}>
                    {() => (
                        <Segmented
                            label={t`Тип підпису`}
                            value={chosenPackaging}
                            onChange={setPackaging}
                            options={packagings.map((item) => ({
                                value: item.id,
                                label: t(item.title),
                                hint: t(item.hint),
                                disabled: fixed !== null && item.id !== fixed.packaging,
                            }))}
                        />
                    )}
                </Field>
            )}
            {fixed && (
                <p
                    className={layout.muted}
                >{t`Формат і тип підпису визначив перший підписант: ваш підпис буде додано до того самого файлу.`}</p>
            )}
            <Field label={t`Рівень підпису`} hint={t`B-LT містить усе для перевірки через роки (рекомендовано).`}>
                {(control) => (
                    <Select
                        {...control}
                        value={level}
                        onChange={(value) => setLevel(value as SignLevel)}
                        options={levels.map((item) => ({ value: item.id, label: t(item.title) }))}
                    />
                )}
            </Field>
        </div>
    );

    const keyStep = (
        <div className={layout.stack}>
            <div className={styles.summary}>
                <DescriptionList
                    items={[
                        target.kind === "document" ? [t`Документ`, target.title || target.fileName] : [t`Файли`, names.join(", ")],
                        [t`Формат`, summaryFormat],
                        [t`Рівень`, summaryLevel],
                    ]}
                />
            </div>
            {owner ? <OwnerCard owner={owner} onForget={forget} /> : <KeyPanel key={keyRound} client={client} onRead={setOwner} />}
            {signError && <Alert tone="danger">{signError}</Alert>}
        </div>
    );

    return (
        <div className={layout.stack}>
            {libraryError && <Alert tone="danger">{libraryError}</Alert>}
            <Wizard
                key={round}
                label={documentMode ? t`Підписання документа` : t`Підписання файлів`}
                onBusyChange={onBusyChange}
                finish={{ text: t`Підписати`, icon: Signature, run: sign }}
                steps={[
                    {
                        id: "data",
                        title: documentMode ? t`Документ` : t`Файли`,
                        description: documentMode
                            ? t`Перевіряємо, що файл не змінювався після реєстрації.`
                            : t`Файли не залишають ваш пристрій.`,
                        content: dataStep,
                        complete: integrityOk && files.length > 0,
                    },
                    {
                        id: "parameters",
                        title: t`Параметри`,
                        description: t`Формат і рівень кваліфікованого електронного підпису.`,
                        content: parametersStep,
                    },
                    {
                        id: "key",
                        title: t`Ключ і підпис`,
                        description: t`Ключ зчитується лише у вашому браузері; пароль не передається на сервер.`,
                        content: keyStep,
                        complete: owner !== null,
                        onEnter: () => setSignError(null),
                        // The private key never outlives the step it is needed for.
                        onLeave: () => forget(),
                    },
                ]}
            />
        </div>
    );
}

/**
 * Uploads a document signature with the certificate data.
 * @param documentId The document.
 * @param owner The key owner.
 * @param signed The signature.
 * @param meta The parameters, the file name and the signed version the signature was added to.
 * @returns Resolves when stored.
 */
async function uploadSignature(
    documentId: string,
    owner: KeyOwner,
    signed: SignResult,
    meta: { format: SignFormat; packaging: SignPackaging; level: SignLevel; fileName: string; baseSignatureId: string | null },
): Promise<void> {
    const form = new FormData();
    form.append("Format", meta.format);
    form.append("Packaging", meta.packaging);
    form.append("Level", meta.level);
    form.append("KeyMedia", "File");
    form.append("SignedAt", signed.signedAt);
    form.append("SubjectFullName", owner.fullName || owner.commonName);
    form.append("IssuerName", owner.issuer);
    form.append("SerialNumber", owner.serial);
    if (owner.organization) {
        form.append("SubjectOrganization", owner.organization);
    }

    if (owner.drfo || owner.edrpou) {
        form.append("SubjectTaxId", owner.drfo || owner.edrpou || "");
    }

    if (owner.validFrom) {
        form.append("ValidFrom", owner.validFrom);
    }

    if (owner.validTo) {
        form.append("ValidTo", owner.validTo);
    }

    form.append("Signature", new Blob([signed.signature], { type: "application/octet-stream" }), meta.fileName);
    if (meta.baseSignatureId) {
        form.append("BaseSignatureId", meta.baseSignatureId);
    }

    await unwrap(
        api.POST("/api/v1/documents/{id}/signatures", {
            params: { path: { id: documentId } },
            body: {} as never,
            bodySerializer: () => form,
        }),
    );
}

/**
 * Props of {@link SigningResult}.
 */
interface SigningResultProps {
    readonly title: string;
    readonly text: string;
    readonly children?: ReactNode;
}

/**
 * The result shown after a successful signature.
 * @param props The props.
 * @returns The result.
 */
export function SigningResult({ title, text, children }: SigningResultProps) {
    return (
        <div className={styles.result} role="status">
            <span className={styles.resultIcon}>
                <Icon icon={BadgeCheck} size="xl" />
            </span>
            <h3>{title}</h3>
            <p className={styles.resultText}>{text}</p>
            {children && <div className={styles.actions}>{children}</div>}
        </div>
    );
}

/**
 * Props of {@link DownloadButton}.
 */
export interface DownloadButtonProps {
    /** The content. */
    readonly bytes: Uint8Array<ArrayBuffer>;
    /** The file name. */
    readonly fileName: string;
}

/**
 * Offers bytes made in the browser as a file download (the object URL lives ten minutes).
 * @param props The props.
 * @returns The link.
 */
export function DownloadButton({ bytes, fileName }: DownloadButtonProps) {
    const { t } = useLingui();
    const [url] = useState(() => URL.createObjectURL(new Blob([bytes], { type: "application/octet-stream" })));
    useEffect(() => {
        const timer = setTimeout(() => URL.revokeObjectURL(url), 10 * 60 * 1000);
        return () => clearTimeout(timer);
    }, [url]);

    return (
        <ButtonLink href={url} download={fileName} icon={Download}>
            {t`Завантажити ${fileName}`}
        </ButtonLink>
    );
}
