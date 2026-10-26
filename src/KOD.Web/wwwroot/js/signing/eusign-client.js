/**
 * Promise-based client for the EUSignCP Web Worker (eusign.worker.js).
 * One worker per page; it is created lazily and terminated by {@link EUSignClient#dispose}.
 * The worker reports its own errors in Ukrainian; they are translated here into the page language.
 */

/**
 * Translates a Ukrainian interface text into the page language (see kod-ds.js).
 * @param {string} text The Ukrainian text.
 * @param {...(string|number)} args The placeholder values.
 * @returns {string} The text.
 */
const t = (text, ...args) => (window.kodDs ? window.kodDs.t(text, ...args) : text);

/**
 * @typedef {object} SigningConfig
 * @property {string} worker URL of eusign.worker.js.
 * @property {string[]} scripts Absolute URLs of euscpt.js, euscpm.js and euscp.ex.js (in this order).
 * @property {string} proxy URL of the CA relay.
 * @property {string} casUrl URL of CAs.json.
 * @property {string} certificatesUrl URL of CACertificates.p7b.
 */

/**
 * @typedef {object} KeyOwner
 * @property {string} fullName Full name from the certificate.
 * @property {string} commonName Common name from the certificate.
 * @property {string|null} organization Organization.
 * @property {string|null} title Position.
 * @property {string|null} drfo Taxpayer number.
 * @property {string|null} edrpou EDRPOU code.
 * @property {string} issuer Issuer (QTSP).
 * @property {string} serial Certificate serial number.
 * @property {string|null} validFrom Certificate validity start (ISO).
 * @property {string|null} validTo Certificate validity end (ISO).
 */

/** Wraps the signing worker. */
export class EUSignClient {
    /**
     * Creates a client.
     * @param {SigningConfig} config The configuration.
     */
    constructor(config) {
        /** @private */
        this.config = config;
        /** @private @type {Worker|null} */
        this.worker = null;
        /** @private */
        this.nextId = 1;
        /** @private @type {Map<number, {resolve: Function, reject: Function}>} */
        this.pending = new Map();
        /** @private @type {Promise<{providers: string[]}>|null} */
        this.ready = null;
    }

    /**
     * Starts the worker and loads the library (about 3 MB compressed, once per page).
     * @returns {Promise<{providers: string[]}>} The QTSP names.
     */
    init() {
        if (!this.ready) {
            this.worker = new Worker(this.config.worker);
            this.worker.onmessage = (event) => this.onMessage(event.data);
            this.worker.onerror = (event) => {
                event.preventDefault();
                this.failAll(new SigningError(-1, t("Не вдалося завантажити бібліотеку підпису.")));
            };
            this.ready = this.call("init", {
                scripts: this.config.scripts,
                proxy: this.config.proxy,
                casUrl: this.config.casUrl,
                certificatesUrl: this.config.certificatesUrl,
                language: document.documentElement.lang === "en" ? "en" : "uk",
            });
            this.ready.catch(() => {
                this.ready = null;
            });
        }

        return this.ready;
    }

    /**
     * Reads a file key inside the worker. The buffer is transferred (the page loses access to it).
     * @param {string} fileName The key file name.
     * @param {ArrayBuffer} key The key container.
     * @param {string} password The password.
     * @param {number} provider Index of the provider, or -1 to detect automatically.
     * @param {ArrayBuffer[]} certificates Optional certificate files.
     * @returns {Promise<KeyOwner>} The key owner.
     */
    readKey(fileName, key, password, provider, certificates) {
        return this.call("readKey", { fileName, key, password, provider, certificates }, [key, ...certificates]);
    }

    /**
     * Signs files.
     * @param {"CAdES"|"XAdES"|"PAdES"|"AsicS"|"AsicE"} format The format.
     * @param {"Detached"|"Enveloped"} packaging The packaging.
     * @param {"B-B"|"B-T"|"B-LT"|"B-LTA"} level The level.
     * @param {{name: string, data: ArrayBuffer}[]} files The files.
     * @returns {Promise<{signature: Uint8Array, signedAt: string}>} The signature.
     */
    sign(format, packaging, level, files) {
        return this.call("sign", { format, packaging, level, files });
    }

    /**
     * Verifies a signature container.
     * @param {{name: string, data: ArrayBuffer}[]} files The container and optional data files.
     * @returns {Promise<any>} The verification result.
     */
    verify(files) {
        return this.call("verify", { files }, files.map((f) => f.data));
    }

    /**
     * Releases the private key in the worker.
     * @returns {Promise<boolean>} Resolves when released.
     */
    resetKey() {
        return this.worker ? this.call("reset", {}) : Promise.resolve(true);
    }

    /**
     * Terminates the worker and wipes every key it held.
     */
    dispose() {
        if (this.worker) {
            this.worker.terminate();
            this.worker = null;
        }

        this.failAll(new SigningError(-1, t("Скасовано.")));
        this.ready = null;
    }

    /**
     * Sends a command.
     * @private
     * @param {string} cmd The command.
     * @param {object} params The parameters.
     * @param {Transferable[]} [transfer] Buffers to transfer.
     * @returns {Promise<any>} The result.
     */
    call(cmd, params, transfer) {
        if (!this.worker) {
            return Promise.reject(new SigningError(-1, t("Бібліотеку підпису не запущено.")));
        }

        const id = this.nextId++;
        return new Promise((resolve, reject) => {
            this.pending.set(id, { resolve, reject });
            this.worker.postMessage({ id, cmd, params }, transfer || []);
        });
    }

    /**
     * Handles a worker reply.
     * @private
     * @param {{id: number, result?: any, error?: {code: number, message: string, args?: string[]}}} message The reply.
     */
    onMessage(message) {
        const pending = this.pending.get(message.id);
        if (!pending) {
            return;
        }

        this.pending.delete(message.id);
        if (message.error) {
            pending.reject(new SigningError(message.error.code, t(message.error.message, ...(message.error.args || []))));
        } else {
            pending.resolve(message.result);
        }
    }

    /**
     * Rejects every pending call.
     * @private
     * @param {Error} error The error.
     */
    failAll(error) {
        this.pending.forEach((p) => p.reject(error));
        this.pending.clear();
    }
}

/** An error reported by the signing library. */
export class SigningError extends Error {
    /**
     * Creates an error.
     * @param {number} code The library error code.
     * @param {string} message The user-facing message.
     */
    constructor(code, message) {
        super(message);
        this.name = "SigningError";
        /** The library error code. */
        this.code = code;
    }
}
