import { useLingui } from "@lingui/react/macro";
import { BadgeCheck, FileSignature } from "lucide-react";
import { useEffect, useState } from "react";
import { formatDateTime } from "@/shared/i18n/format";
import { SigningClient, type VerifyResult } from "@/shared/signing/client";
import { OwnerCard } from "@/shared/signing/KeyPanel";
import { DownloadButton } from "@/shared/signing/SigningWizard";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { FileDrop } from "@/shared/ui/FileDrop";

/**
 * Verifies a signed file in the browser: who signed, the certificate, the time and the integrity of the data. Nothing
 * is uploaded.
 * @returns The panel.
 */
export function VerifyPanel() {
    const { t } = useLingui();
    const [client] = useState(() => new SigningClient());
    const [files, setFiles] = useState<readonly File[]>([]);
    const [result, setResult] = useState<VerifyResult | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    // The library loads in the background while the user picks the files; leaving the page ends the worker.
    useEffect(() => {
        client.init().catch((failure: unknown) => setError(failure instanceof Error ? failure.message : String(failure)));
        return () => client.dispose();
    }, [client]);

    /**
     * Verifies the chosen files.
     */
    async function verify(): Promise<void> {
        setBusy(true);
        setError(null);
        setResult(null);
        try {
            await client.init();
            const payload = await Promise.all(files.map(async (file) => ({ name: file.name, data: await file.arrayBuffer() })));
            setResult(await client.verify(payload));
        } catch (failure) {
            setError(failure instanceof Error ? failure.message : String(failure));
        } finally {
            setBusy(false);
        }
    }

    const format = result?.format ?? "";
    const container = result?.container ?? "";
    const count = result?.signatures.length ?? 0;

    return (
        <div className={layout.stack}>
            <FileDrop
                icon={FileSignature}
                multiple
                title={
                    files.length > 0 ? files.map((file) => file.name).join(", ") : t`Перетягніть підписаний файл (і за потреби оригінал)`
                }
                hint={t`*.p7s, *.pdf, *.xml, *.asics, *.asice. Для зовнішнього підпису додайте також підписаний файл.`}
                onFiles={(selected) => {
                    setFiles(selected);
                    setResult(null);
                    setError(null);
                }}
            />
            <div className={layout.stack} aria-live="polite">
                {error && (
                    <Alert tone="danger" title={t`Перевірку не пройдено`}>
                        {error}
                    </Alert>
                )}
                {result && (
                    <>
                        <Alert tone="inverse" icon={BadgeCheck} title={t`Підписи дійсні`}>
                            {t`${format} · ${container} · підписів: ${count}`}
                        </Alert>
                        {result.signatures.map((signature, index) => (
                            <OwnerCard
                                key={index}
                                owner={signature.signer}
                                title={`${index + 1}. ${signature.signer.fullName || signature.signer.commonName}`}
                                extra={[
                                    [
                                        signature.isTimeStamp
                                            ? t`Позначка часу`
                                            : signature.isClaimed
                                              ? t`Час підпису (вказаний у документі)`
                                              : t`Час підпису`,
                                        formatDateTime(signature.time),
                                    ],
                                ]}
                            />
                        ))}
                        {result.content && <DownloadButton bytes={result.content.data} fileName={result.content.name} />}
                    </>
                )}
            </div>
            <Button icon={BadgeCheck} block busy={busy} disabled={files.length === 0} onClick={() => void verify()}>
                {t`Перевірити`}
            </Button>
        </div>
    );
}
