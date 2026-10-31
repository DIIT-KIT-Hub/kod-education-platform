import { i18n, type MessageDescriptor } from "@lingui/core";
import { msg } from "@lingui/core/macro";

/**
 * The owner of a key or of a verified signature, from the certificate.
 */
export interface KeyOwner {
    readonly fullName: string;
    readonly commonName: string;
    readonly organization: string | null;
    readonly title: string | null;
    /** The taxpayer number. */
    readonly drfo: string | null;
    readonly edrpou: string | null;
    /** The qualified trust service provider. */
    readonly issuer: string;
    readonly serial: string;
    readonly validFrom: string | null;
    readonly validTo: string | null;
}

/**
 * Signature formats of the library.
 */
export type SignFormat = "CAdES" | "XAdES" | "PAdES" | "AsicS" | "AsicE";

/**
 * Packagings of CAdES and XAdES.
 */
export type SignPackaging = "Detached" | "Enveloped";

/**
 * Signature levels (ETSI baseline profiles).
 */
export type SignLevel = "B-B" | "B-T" | "B-LT" | "B-LTA";

/**
 * A file to sign or verify.
 */
export interface SignFile {
    readonly name: string;
    readonly data: ArrayBuffer;
}

/**
 * A signature made by the worker.
 */
export interface SignResult {
    readonly signature: Uint8Array<ArrayBuffer>;
    readonly signedAt: string;
}

/**
 * A verified signature.
 */
export interface VerifiedSignature {
    readonly signer: KeyOwner;
    /** The signing time, the signature time-stamp or the time claimed by the document. */
    readonly time: string | null;
    readonly isTimeStamp: boolean;
    readonly isClaimed: boolean;
}

/**
 * The result of a verification.
 */
export interface VerifyResult {
    readonly format: string;
    readonly container: string;
    readonly signatures: readonly VerifiedSignature[];
    /** The signed data of an enveloped signature. */
    readonly content: { readonly name: string; readonly data: Uint8Array<ArrayBuffer> } | null;
}

/**
 * The library and the trust list: the IIT library is served by the server (compressed static assets), the providers
 * and CA certificates come from the server's trust list, and the library talks to the providers only through the
 * same-origin relay.
 */
const config = {
    scripts: ["/lib/eusign/euscpt.js", "/lib/eusign/euscpm.js", "/lib/eusign/euscp.ex.js"],
    proxy: "/api/v1/eusign/proxy",
    casUrl: "/api/v1/eusign/providers",
    certificatesUrl: "/api/v1/eusign/certificates",
};

/**
 * The worker's own messages (it has no translations, so it reports Ukrainian texts and the page translates them).
 * Messages of the library itself already follow the language passed to `init`.
 */
const workerMessages: Record<string, MessageDescriptor> = {
    "Не вдалося завантажити {0} ({1}).": msg`Не вдалося завантажити {0} ({1}).`,
    "Не вдалося знайти сертифікат ключа. Оберіть надавача вручну або додайте файли сертифікатів.": msg`Не вдалося знайти сертифікат ключа. Оберіть надавача вручну або додайте файли сертифікатів.`,
    "Не вдалося отримати позначку часу. Спробуйте рівень підпису B-B або повторіть пізніше.": msg`Не вдалося отримати позначку часу. Спробуйте рівень підпису B-B або повторіть пізніше.`,
    "Не вдалося перевірити статус сертифіката (OCSP). Спробуйте пізніше.": msg`Не вдалося перевірити статус сертифіката (OCSP). Спробуйте пізніше.`,
    "Невідома помилка бібліотеки підпису.": msg`Невідома помилка бібліотеки підпису.`,
    "Невідомий формат підпису.": msg`Невідомий формат підпису.`,
    "Немає зв'язку з сервером надавача (ЦСК). Перевірте з'єднання та спробуйте ще раз.": msg`Немає зв'язку з сервером надавача (ЦСК). Перевірте з'єднання та спробуйте ще раз.`,
    "Неправильний пароль або пошкоджений файл ключа.": msg`Неправильний пароль або пошкоджений файл ключа.`,
    "Остання підписана версія документа не збігається з даними системи. Оновіть сторінку й спробуйте ще раз.": msg`Остання підписана версія документа не збігається з даними системи. Оновіть сторінку й спробуйте ще раз.`,
    "Остання підписана версія містить інший документ. Підписання зупинено.": msg`Остання підписана версія містить інший документ. Підписання зупинено.`,
    "Сертифікат ключа визнано недійсним за відповіддю OCSP.": msg`Сертифікат ключа визнано недійсним за відповіддю OCSP.`,
    "Сертифікат ключа скасовано (у списку відкликаних).": msg`Сертифікат ключа скасовано (у списку відкликаних).`,
    "Спочатку зчитайте ключ.": msg`Спочатку зчитайте ключ.`,
    "Строк дії сертифіката ключа закінчився або ще не настав.": msg`Строк дії сертифіката ключа закінчився або ще не настав.`,
    "У файлі не знайдено підписів.": msg`У файлі не знайдено підписів.`,
    "У цьому PDF немає підписів. Якщо файл завантажено з картки документа, візьміть підписаний PDF у розділі «Підписи», а не оригінал документа.": msg`У цьому PDF немає підписів. Якщо файл завантажено з картки документа, візьміть підписаний PDF у розділі «Підписи», а не оригінал документа.`,
    "Це зовнішній підпис: додайте також файл, який було підписано.": msg`Це зовнішній підпис: додайте також файл, який було підписано.`,
};

/**
 * Translates a worker message; library messages and unknown texts are shown as they are.
 * @param message The message.
 * @param args The values of its `{0}`… placeholders.
 * @returns The text in the interface language.
 */
function translate(message: string, args: readonly (string | number)[]): string {
    const descriptor = workerMessages[message];
    const values = Object.fromEntries(args.map((value, index) => [String(index), value]));
    return descriptor ? i18n._({ ...descriptor, values }) : message;
}

/**
 * An error reported by the signing library or the worker.
 */
export class SigningError extends Error {
    /**
     * Creates an error.
     * @param code The library error code (-1 for errors of the page).
     * @param message The message for the user.
     */
    constructor(
        readonly code: number,
        message: string,
    ) {
        super(message);
        this.name = "SigningError";
    }
}

/**
 * A reply of the worker.
 */
interface WorkerReply {
    readonly id: number;
    readonly result?: unknown;
    readonly error?: { readonly code: number; readonly message: string; readonly args?: readonly (string | number)[] };
}

/**
 * A promise-based client of the signing Web Worker. The worker hosts the official IIT EUSignCP-JS library; the key file
 * and its password are transferred to it and never leave the browser. One client per signing panel: {@link dispose}
 * terminates the worker and wipes every key it held.
 */
export class SigningClient {
    private worker: Worker | null = null;
    private nextId = 1;
    private readonly pending = new Map<number, { resolve: (value: unknown) => void; reject: (error: Error) => void }>();
    private ready: Promise<{ providers: string[] }> | null = null;

    /**
     * Starts the worker and loads the library (about 3 MB compressed, once per worker).
     * @returns The names of the qualified trust service providers.
     */
    init(): Promise<{ providers: string[] }> {
        if (!this.ready) {
            this.worker = new Worker(new URL("./eusign.worker.js", import.meta.url), { name: "eusign" });
            this.worker.onmessage = (event: MessageEvent<WorkerReply>) => this.receive(event.data);
            this.worker.onerror = (event) => {
                event.preventDefault();
                this.failAll(new SigningError(-1, i18n._(msg`Не вдалося завантажити бібліотеку підпису.`)));
            };
            const absolute = (path: string) => new URL(path, window.location.origin).href;
            this.ready = this.call<{ providers: string[] }>("init", {
                scripts: config.scripts.map(absolute),
                proxy: absolute(config.proxy),
                casUrl: absolute(config.casUrl),
                certificatesUrl: absolute(config.certificatesUrl),
                language: i18n.locale === "en" ? "en" : "uk",
            });
            this.ready.catch(() => {
                this.ready = null;
            });
        }

        return this.ready;
    }

    /**
     * Reads a file key inside the worker. The buffers are transferred: the page loses access to them.
     * @param fileName The key file name.
     * @param key The key container.
     * @param password The password.
     * @param provider The index of the provider, or -1 to detect it.
     * @param certificates Optional certificate files.
     * @returns The key owner.
     */
    readKey(fileName: string, key: ArrayBuffer, password: string, provider: number, certificates: ArrayBuffer[]): Promise<KeyOwner> {
        return this.call("readKey", { fileName, key, password, provider, certificates }, [key, ...certificates]);
    }

    /**
     * Signs files with the key read before.
     * @param format The format.
     * @param packaging The packaging.
     * @param level The level.
     * @param files The files.
     * @param previous The signed version to add the signature to (a document already signed in a cumulative format)
     * and the number of signatures it must hold.
     * @returns The signature.
     */
    sign(
        format: SignFormat,
        packaging: SignPackaging,
        level: SignLevel,
        files: readonly SignFile[],
        previous: { readonly data: ArrayBuffer; readonly signatures: number } | null,
    ): Promise<SignResult> {
        return this.call("sign", { format, packaging, level, files, previous });
    }

    /**
     * Verifies a signature container.
     * @param files The container and, for a detached signature, the signed file.
     * @returns The verification result.
     */
    verify(files: SignFile[]): Promise<VerifyResult> {
        return this.call(
            "verify",
            { files },
            files.map((file) => file.data),
        );
    }

    /**
     * Releases the private key in the worker.
     * @returns Resolves when released.
     */
    async resetKey(): Promise<void> {
        if (this.worker) {
            await this.call("reset", {});
        }
    }

    /**
     * Terminates the worker and wipes every key it held.
     */
    dispose(): void {
        this.worker?.terminate();
        this.worker = null;
        this.ready = null;
        this.failAll(new SigningError(-1, i18n._(msg`Скасовано.`)));
    }

    /**
     * Sends a command to the worker.
     * @param command The command.
     * @param params The parameters.
     * @param transfer The buffers to transfer.
     * @returns The result.
     */
    private call<T>(command: string, params: object, transfer: Transferable[] = []): Promise<T> {
        const worker = this.worker;
        if (!worker) {
            return Promise.reject(new SigningError(-1, i18n._(msg`Бібліотеку підпису не запущено.`)));
        }

        const id = this.nextId++;
        return new Promise<T>((resolve, reject) => {
            this.pending.set(id, { resolve: resolve as (value: unknown) => void, reject });
            worker.postMessage({ id, cmd: command, params }, transfer);
        });
    }

    /**
     * Settles the call a reply belongs to.
     * @param reply The reply.
     */
    private receive(reply: WorkerReply): void {
        const pending = this.pending.get(reply.id);
        if (!pending) {
            return;
        }

        this.pending.delete(reply.id);
        if (reply.error) {
            pending.reject(new SigningError(reply.error.code, translate(reply.error.message, reply.error.args ?? [])));
        } else {
            pending.resolve(reply.result);
        }
    }

    /**
     * Rejects every pending call.
     * @param error The error.
     */
    private failAll(error: Error): void {
        this.pending.forEach((pending) => pending.reject(error));
        this.pending.clear();
    }
}
