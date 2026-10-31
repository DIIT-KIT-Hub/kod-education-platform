import { useLingui } from "@lingui/react/macro";
import { FileKey, FileBadge, KeyRound, RefreshCw } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { formatDateTime } from "@/shared/i18n/format";
import layout from "@/shared/styles/layout.module.css";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { DescriptionList } from "@/shared/ui/DescriptionList";
import { Field } from "@/shared/ui/Field";
import { FileDrop } from "@/shared/ui/FileDrop";
import { PasswordInput } from "@/shared/ui/PasswordInput";
import { Segmented } from "@/shared/ui/Segmented";
import { Select } from "@/shared/ui/Select";
import type { KeyOwner, SigningClient } from "./client";
import { keyAccept } from "./formats";
import styles from "./Signing.module.css";

/**
 * Props of {@link KeyPanel}.
 */
export interface KeyPanelProps {
    /** The worker client. */
    readonly client: SigningClient;
    /** Called with the owner once the key is read. */
    readonly onRead: (owner: KeyOwner) => void;
}

/**
 * Reads a file key: provider, key file, password and optional certificates. The key file and the password go only to
 * the Web Worker; the password field is cleared as soon as it is handed over. Remount the panel (a new `key`) to clear
 * it after the key is released.
 * @param props The props.
 * @returns The panel.
 */
export function KeyPanel({ client, onRead }: KeyPanelProps) {
    const { t } = useLingui();
    const [providers, setProviders] = useState<readonly string[] | null>(null);
    const [provider, setProvider] = useState("-1");
    const [keyFile, setKeyFile] = useState<File | null>(null);
    const [certificates, setCertificates] = useState<readonly File[]>([]);
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    // The provider list arrives with the library.
    useEffect(() => {
        let current = true;
        client.init().then(
            (info) => current && setProviders(info.providers),
            () => current && setProviders([]),
        );
        return () => {
            current = false;
        };
    }, [client]);

    /**
     * Hands the key and the password to the worker.
     * @param event The submit event.
     */
    async function read(event: FormEvent): Promise<void> {
        event.preventDefault();
        setError(null);
        if (!keyFile) {
            setError(t`Оберіть файл ключа.`);
            return;
        }

        if (!password) {
            setError(t`Введіть пароль.`);
            return;
        }

        setBusy(true);
        const secret = password;
        setPassword("");
        try {
            await client.init();
            const keyBuffer = await keyFile.arrayBuffer();
            const certificateBuffers = await Promise.all(certificates.map((file) => file.arrayBuffer()));
            onRead(await client.readKey(keyFile.name, keyBuffer, secret, Number(provider), certificateBuffers));
        } catch (failure) {
            setError(failure instanceof Error ? failure.message : String(failure));
        } finally {
            setBusy(false);
        }
    }

    return (
        <form className={layout.stack} onSubmit={(event) => void read(event)}>
            <Segmented
                label={t`Тип ключа`}
                value="file"
                onChange={() => undefined}
                options={[
                    { value: "file", label: t`Файловий ключ`, hint: "Key-6.dat, PFX, JKS…" },
                    { value: "token", label: t`Захищений носій`, hint: t`потребує агента ІІТ`, disabled: true },
                    { value: "cloud", label: t`Хмарний / Дія.Підпис`, hint: t`потребує сервісу надавача`, disabled: true },
                ]}
            />
            <Field label={t`Надавач електронних довірчих послуг`}>
                {(control) => (
                    <Select
                        {...control}
                        value={provider}
                        onChange={setProvider}
                        options={[
                            { value: "-1", label: t`Визначити автоматично` },
                            ...(providers ?? []).map((name, index) => ({ value: String(index), label: name })),
                        ]}
                    />
                )}
            </Field>
            <FileDrop
                icon={FileKey}
                accept={keyAccept}
                title={keyFile?.name ?? t`Оберіть файл ключа`}
                hint="Key-6.dat, *.pfx, *.p12, *.pk8, *.jks, *.zs2"
                onFiles={(files) => setKeyFile(files[0] ?? null)}
            />
            <Field label={t`Пароль захисту ключа`} hint={t`Пароль не передається на сервер.`}>
                {(control) => (
                    <PasswordInput
                        {...control}
                        value={password}
                        onChange={(event) => setPassword(event.target.value)}
                        autoComplete="off"
                        spellCheck={false}
                    />
                )}
            </Field>
            <details className={styles.details}>
                <summary>{t`Додати сертифікати вручну (якщо надавач не підтримує автоматичне завантаження)`}</summary>
                <FileDrop
                    icon={FileBadge}
                    accept=".cer,.crt,.der,.p7b"
                    multiple
                    title={certificates.length > 0 ? certificates.map((file) => file.name).join(", ") : t`Файли сертифікатів`}
                    hint="*.cer, *.crt, *.p7b"
                    onFiles={setCertificates}
                />
            </details>
            {error && <Alert tone="danger">{error}</Alert>}
            <Button type="submit" icon={KeyRound} block busy={busy}>
                {t`Зчитати ключ`}
            </Button>
        </form>
    );
}

/**
 * Props of {@link OwnerCard}.
 */
export interface OwnerCardProps {
    /** The key owner. */
    readonly owner: KeyOwner;
    /** Releases the key so another one can be read. */
    readonly onForget?: () => void;
    /** A heading instead of the owner's name (verification results). */
    readonly title?: string;
    /** Extra rows (e.g. the signing time). */
    readonly extra?: readonly (readonly [string, string | null])[];
}

/**
 * The owner of a key or a signature, from the certificate.
 * @param props The props.
 * @returns The card.
 */
export function OwnerCard({ owner, onForget, title, extra = [] }: OwnerCardProps) {
    const { t } = useLingui();
    return (
        <div className={styles.owner} data-inverse>
            <div className={layout.rowBetween}>
                <strong>{title ?? (owner.fullName || owner.commonName)}</strong>
                {onForget && (
                    <Button variant="ghost" size="sm" icon={RefreshCw} onClick={onForget}>
                        {t`Інший ключ`}
                    </Button>
                )}
            </div>
            <DescriptionList
                inverse
                items={[
                    [t`Організація`, owner.organization],
                    [t`Посада`, owner.title],
                    [t`РНОКПП`, owner.drfo],
                    [t`ЄДРПОУ`, owner.edrpou],
                    [t`Надавач`, owner.issuer],
                    [t`Сертифікат №`, owner.serial],
                    [t`Дійсний до`, owner.validTo ? formatDateTime(owner.validTo) : null],
                    ...extra,
                ]}
            />
        </div>
    );
}
