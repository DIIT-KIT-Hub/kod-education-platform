/**
 * Classic Web Worker that hosts the official IIT EUSignCP-JS library ("Користувач ЦСК", euscp.ex.js).
 *
 * Why a worker:
 *  - the library performs synchronous XHR to OCSP/TSP/CMP servers; in a worker this never freezes the page;
 *  - the private key lives only inside this worker's memory and is released on "reset" or when the worker ends.
 *
 * Security: the key file and its password arrive from the page via postMessage and are never sent anywhere.
 * Network traffic goes only to the same-origin relay (/api/v1/eusign/proxy) that forwards public protocol
 * messages (certificate status, time-stamp requests, certificate lookups) to qualified trust service providers.
 *
 * Protocol: page → { id, cmd, params }; worker → { id, result } or { id, error: { code, message, args? } }.
 * Commands: init, readKey, sign, verify, reset.
 *
 * Localization: the worker has no access to the page's translations, so its own messages stay Ukrainian (marked with the
 * `t` function, with `{0}` placeholders filled from `error.args`) and are translated by eusign-client.js on the page.
 * Library messages follow the `language` passed to "init".
 */

/* global EUSignCP, EndUserReference, EU_UA_LANG, EU_EN_LANG, EU_SIGN_TYPE_PARAMETER, EU_SIGN_TYPE_CADES_BES, EU_SIGN_TYPE_CADES_T,
   EU_SIGN_TYPE_CADES_X_LONG, EU_SIGN_TYPE_CADES_X_LONG_TRUSTED, EU_XADES_TYPE_DETACHED, EU_XADES_TYPE_ENVELOPING,
   EU_XADES_SIGN_LEVEL_B_B, EU_XADES_SIGN_LEVEL_B_T, EU_XADES_SIGN_LEVEL_B_LT, EU_XADES_SIGN_LEVEL_B_LTA,
   EU_PADES_SIGN_LEVEL_B_B, EU_PADES_SIGN_LEVEL_B_T, EU_PADES_SIGN_LEVEL_B_LT, EU_PADES_SIGN_LEVEL_B_LTA,
   EU_ASIC_TYPE_S, EU_ASIC_TYPE_E, EU_ASIC_SIGN_TYPE_CADES, EU_ASIC_SIGN_TYPE_XADES, EU_CERT_KEY_TYPE_DSTU4145,
   EU_CERT_KEY_TYPE_RSA, EU_CERT_KEY_TYPE_ECDSA, EU_KEY_USAGE_DIGITAL_SIGNATURE, EU_CTX_SIGN_DSTU4145_WITH_GOST34311,
   EU_CTX_SIGN_DSTU4145_WITH_DSTU7564, EU_CTX_SIGN_RSA_WITH_SHA, EU_CTX_SIGN_ECDSA_WITH_SHA */

"use strict";

/** Maximum size of data the library accepts (MB); the heap is 128 + 8 × this value. */
self.EU_MAX_DATA_SIZE_MB = 25;

/** The library is initialized explicitly after loading. */
self.EU_MODULE_INITIALIZE_ON_LOAD = false;

/**
 * Marks a Ukrainian message for translation; the page translates it (eusign-client.js) when the error arrives.
 * @param {string} text The Ukrainian text.
 * @returns {string} The same text.
 */
const t = (text) => text;

/**
 * Creates an error whose message has `{0}`… placeholders, filled (after translation) on the page.
 * @param {string} message The Ukrainian message.
 * @param {...(string|number)} args The placeholder values.
 * @returns {Error} The error.
 */
function formatError(message, ...args) {
    const error = new Error(message);
    error.args = args;
    return error;
}

/** Error code of "end of enumeration". */
const END_OF_ENUM = 0x0007;

/** Error code of "certificate not found". */
const CERT_NOT_FOUND = 0x0033;

/** @type {any} The EUSignCP instance. */
let euSign = null;

/** @type {Array<any>} The qualified trust service providers (CAs.json). */
let providers = [];

/** @type {any} The context that owns the private key. */
let keyContext = null;

/** @type {any} The private key context. */
let privateKey = null;

/** @type {number} The signing algorithm matching the key certificate. */
let signAlgorithm = 0;

/** @type {any} A context used for verification. */
let verifyContext = null;

/** @type {Promise<void>|null} Resolves when the core has been loaded. */
let loaded = null;

/**
 * Loads the library scripts and waits for the asm.js core to start.
 * @param {string[]} scripts Absolute URLs: types, module, core.
 * @returns {Promise<void>} Resolves when the core is ready.
 */
function loadLibrary(scripts) {
    if (loaded) {
        return loaded;
    }

    loaded = new Promise((resolve, reject) => {
        // Called by the library (euscpm.js postRun) once the core is running.
        self.EUSignCPModuleLoaded = () => resolve();
        try {
            self.importScripts(...scripts);
        } catch (error) {
            reject(error);
        }
    });

    return loaded;
}

/**
 * Converts any thrown value into a plain, transferable error.
 * @param {any} error The error.
 * @returns {{code: number, message: string, args?: string[]}} The normalized error.
 */
function normalizeError(error) {
    const code = error && typeof error.errorCode === "number" ? error.errorCode : -1;
    const known = {
        0x0005: t("Немає зв'язку з сервером надавача (ЦСК). Перевірте з'єднання та спробуйте ще раз."),
        0x0018: t("Неправильний пароль або пошкоджений файл ключа."),
        0x0033: t("Не вдалося знайти сертифікат ключа. Оберіть надавача вручну або додайте файли сертифікатів."),
        0x0034: t("Строк дії сертифіката ключа закінчився або ще не настав."),
        0x0035: t("Сертифікат ключа скасовано (у списку відкликаних)."),
        0x0041: t("Не вдалося отримати позначку часу. Спробуйте рівень підпису B-B або повторіть пізніше."),
        0x0051: t("Не вдалося перевірити статус сертифіката (OCSP). Спробуйте пізніше."),
        0x0053: t("Сертифікат ключа визнано недійсним за відповіддю OCSP."),
    };
    const message = known[code]
        || (error && (error.message || error.toString && error.toString()))
        || t("Невідома помилка бібліотеки підпису.");
    const args = !known[code] && error && Array.isArray(error.args) ? error.args.map(String) : undefined;
    return { code, message: String(message), args };
}

/**
 * Fetches a same-origin resource.
 * @param {string} url The URL.
 * @param {"json"|"bytes"} kind The expected content.
 * @returns {Promise<any>} The parsed JSON or a byte array.
 */
async function fetchResource(url, kind) {
    const response = await fetch(url, { credentials: "same-origin" });
    if (!response.ok) {
        throw formatError(t("Не вдалося завантажити {0} ({1})."), url, response.status);
    }

    return kind === "json" ? response.json() : new Uint8Array(await response.arrayBuffer());
}

/**
 * Applies the base settings: in-memory file store, no outgoing proxy, online mode, OCSP access points of every provider.
 * @param {string} proxy The relay URL.
 */
function applyBaseSettings(proxy) {
    euSign.SetXMLHTTPProxyService(proxy);

    let settings = euSign.CreateFileStoreSettings();
    settings.SetPath("");
    settings.SetSaveLoadedCerts(true);
    euSign.SetFileStoreSettings(settings);

    euSign.SetProxySettings(euSign.CreateProxySettings());

    settings = euSign.CreateModeSettings();
    settings.SetOfflineMode(false);
    euSign.SetModeSettings(settings);

    euSign.SetTSPSettings(euSign.CreateTSPSettings());

    settings = euSign.CreateOCSPSettings();
    settings.SetUseOCSP(true);
    settings.SetBeforeStore(true);
    settings.SetAddress("");
    settings.SetPort("80");
    euSign.SetOCSPSettings(settings);

    euSign.SetCMPSettings(euSign.CreateCMPSettings());
    euSign.SetLDAPSettings(euSign.CreateLDAPSettings());

    settings = euSign.CreateOCSPAccessInfoModeSettings();
    settings.SetEnabled(true);
    euSign.SetOCSPAccessInfoModeSettings(settings);

    settings = euSign.CreateOCSPAccessInfoSettings();
    for (const provider of providers) {
        settings.SetAddress(provider.ocspAccessPointAddress);
        settings.SetPort(provider.ocspAccessPointPort);
        for (const issuer of provider.issuerCNs) {
            settings.SetIssuerCN(issuer);
            euSign.SetOCSPAccessInfoSettings(settings);
        }
    }
}

/**
 * Applies the TSP, OCSP and CMP settings of a provider.
 * @param {any|null} provider The provider (CAs.json entry) or null for offline mode.
 */
function applyProviderSettings(provider) {
    const offline = !provider || provider.address === "";
    const useCmp = !offline && provider.cmpAddress !== "";

    let settings = euSign.CreateTSPSettings();
    if (!offline) {
        settings.SetGetStamps(true);
        settings.SetAddress(provider.tspAddress);
        settings.SetPort(provider.tspAddressPort || "80");
    }

    euSign.SetTSPSettings(settings);

    settings = euSign.CreateOCSPSettings();
    if (!offline) {
        settings.SetUseOCSP(true);
        settings.SetBeforeStore(true);
        settings.SetAddress(provider.ocspAccessPointAddress);
        settings.SetPort(provider.ocspAccessPointPort || "80");
    }

    euSign.SetOCSPSettings(settings);

    settings = euSign.CreateCMPSettings();
    settings.SetUseCMP(useCmp);
    if (useCmp) {
        settings.SetAddress(provider.cmpAddress);
        settings.SetPort("80");
    }

    euSign.SetCMPSettings(settings);
    euSign.SetLDAPSettings(euSign.CreateLDAPSettings());
}

/**
 * Frees the loaded private key.
 */
function releaseKey() {
    if (privateKey) {
        try {
            euSign.CtxFreePrivateKey(privateKey);
        } catch (e) {
            /* Already released. */
        }
    }

    if (keyContext) {
        try {
            euSign.CtxFree(keyContext);
        } catch (e) {
            /* Already released. */
        }
    }

    privateKey = null;
    keyContext = null;
    signAlgorithm = 0;
}

/**
 * Picks the signing certificate of the key and the matching signing algorithm.
 * @returns {{info: any, algorithm: number}} The certificate info and algorithm.
 */
function resolveSigningCertificate() {
    const candidates = [
        [EU_CERT_KEY_TYPE_DSTU4145, null],
        [EU_CERT_KEY_TYPE_RSA, EU_CTX_SIGN_RSA_WITH_SHA],
        [EU_CERT_KEY_TYPE_ECDSA, EU_CTX_SIGN_ECDSA_WITH_SHA],
    ];

    for (const [keyType, algorithm] of candidates) {
        try {
            const certificate = euSign.CtxGetOwnCertificate(privateKey, keyType, EU_KEY_USAGE_DIGITAL_SIGNATURE);
            const info = certificate.GetInfoEx();
            if (algorithm !== null) {
                return { info, algorithm };
            }

            // DSTU 7564 (Kupyna) hash types are 7, 8 and 9; otherwise GOST 34.311.
            const kupyna = [7, 8, 9].includes(info.certHashType);
            return { info, algorithm: kupyna ? EU_CTX_SIGN_DSTU4145_WITH_DSTU7564 : EU_CTX_SIGN_DSTU4145_WITH_GOST34311 };
        } catch (e) {
            /* Try the next key type. */
        }
    }

    throw { errorCode: CERT_NOT_FOUND };
}

/**
 * Reads the key, downloading certificates through CMP when the key container has none.
 * @param {Uint8Array} keyBytes The key container.
 * @param {string} password The password.
 * @param {any|null} provider The chosen provider, or null for automatic detection.
 * @returns {any} The private key context.
 */
function readPrivateKey(keyBytes, password, provider) {
    keyContext = euSign.CtxCreate();
    try {
        return euSign.CtxReadPrivateKeyBinary(keyContext, keyBytes, password);
    } catch (error) {
        if (!error || error.errorCode !== CERT_NOT_FOUND) {
            throw error;
        }
    }

    // Key-6.dat usually carries no certificates: ask the CMP servers of the chosen (or every) provider.
    const servers = (provider ? [provider] : providers)
        .filter((p) => p.cmpAddress)
        .map((p) => `${p.cmpAddress}:80`);
    const keyInfo = euSign.GetKeyInfoBinary(keyBytes, password);
    const certificates = euSign.GetCertificatesByKeyInfo(keyInfo, servers);
    euSign.SaveCertificates(certificates);
    return euSign.CtxReadPrivateKeyBinary(keyContext, keyBytes, password);
}

/**
 * Converts owner and certificate data into a plain object.
 * @param {any} owner The owner info.
 * @param {any|null} certificate The certificate info.
 * @returns {object} The summary.
 */
function describeOwner(owner, certificate) {
    return {
        fullName: owner.subjFullName || owner.subjCN || "",
        commonName: owner.subjCN || "",
        organization: owner.subjOrg || null,
        title: owner.subjTitle || null,
        drfo: owner.subjDRFOCode || null,
        edrpou: owner.subjEDRPOUCode || null,
        issuer: owner.issuerCN || owner.issuer || "",
        serial: owner.serial || "",
        validFrom: certificate && certificate.certBeginTime ? new Date(certificate.certBeginTime).toISOString() : null,
        validTo: certificate && certificate.certEndTime ? new Date(certificate.certEndTime).toISOString() : null,
    };
}

/**
 * Converts a library time value into an ISO string.
 * @param {Date|string|number|null|undefined} value The time.
 * @returns {string|null} The ISO time, or null when absent or invalid.
 */
function isoTime(value) {
    if (!value) {
        return null;
    }

    const date = value instanceof Date ? value : new Date(value);
    return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

/**
 * Reads the signing times that PDF signature dictionaries claim (/M), in the order of the signatures in the file.
 * PAdES forbids the signing-time attribute, so without a signature time-stamp this is the only time a PDF carries.
 * @param {Uint8Array} bytes The PDF.
 * @returns {(string|null)[]} The ISO times (null where a signature has none).
 */
function pdfClaimedTimes(bytes) {
    const text = new TextDecoder("latin1").decode(bytes);
    const times = [];
    const byteRange = /\/ByteRange\s*\[/g;
    let match;
    while ((match = byteRange.exec(text)) !== null) {
        const start = Math.max(0, text.lastIndexOf(" obj", match.index));
        const end = text.indexOf("endobj", match.index);
        const dictionary = text.slice(start, end === -1 ? undefined : end);
        const m = /\/M\s*\(D:(\d{4})(\d{2})?(\d{2})?(\d{2})?(\d{2})?(\d{2})?([Z+-])?(\d{2})?'?(\d{2})?'?\)/.exec(dictionary);
        if (!m) {
            times.push(null);
            continue;
        }

        const [, year, month = "01", day = "01", hour = "00", minute = "00", second = "00", sign, offsetHours = "00", offsetMinutes = "00"] = m;
        const zone = !sign || sign === "Z" ? "Z" : `${sign}${offsetHours}:${offsetMinutes}`;
        times.push(isoTime(`${year}-${month}-${day}T${hour}:${minute}:${second}${zone}`));
    }

    return times;
}

/**
 * Describes a verified signature. The time is the signing-time attribute, otherwise the signature time-stamp,
 * otherwise (PDF only) the time the signature dictionary claims.
 * @param {any} signInfo The library sign info.
 * @param {string|null} [claimedTime] The time claimed by the container (PDF /M).
 * @returns {object} The summary.
 */
function describeSignature(signInfo, claimedTime) {
    const owner = signInfo.ownerInfo || {};
    const time = signInfo.timeInfo || {};
    const signed = time.isTimeAvail ? isoTime(time.time) : null;
    const stamp = time.isSignTimeStampAvail ? isoTime(time.signTimeStamp) : null;
    return {
        signer: describeOwner(owner, null),
        time: signed || stamp || claimedTime || null,
        isTimeStamp: signed ? Boolean(time.isTimeStamp) : Boolean(stamp),
        isClaimed: !signed && !stamp && Boolean(claimedTime),
    };
}

/** Command handlers. */
const handlers = {
    /**
     * Loads and configures the library.
     * @param {{scripts: string[], proxy: string, casUrl: string, certificatesUrl: string, language?: string}} params The parameters
     * (language: "uk" or "en", for the library's own error messages).
     * @returns {Promise<{providers: string[]}>} The provider names.
     */
    async init(params) {
        if (euSign) {
            return { providers: providers.map((p) => p.issuerCNs[0]) };
        }

        await loadLibrary(params.scripts);
        euSign = EUSignCP();
        euSign.Initialize();
        euSign.SetErrorMessageLanguage(params.language === "en" ? EU_EN_LANG : EU_UA_LANG);

        providers = await fetchResource(params.casUrl, "json");
        applyBaseSettings(params.proxy);
        euSign.SaveCertificates(await fetchResource(params.certificatesUrl, "bytes"));
        verifyContext = euSign.CtxCreate();

        return { providers: providers.map((p) => p.issuerCNs[0]) };
    },

    /**
     * Reads a file key (Key-6.dat, PFX/P12, PK8, JKS, ZS2).
     * @param {{fileName: string, key: ArrayBuffer, password: string, provider: number, certificates: ArrayBuffer[]}} params The parameters.
     * @returns {object} The key owner.
     */
    readKey(params) {
        releaseKey();
        let keyBytes = new Uint8Array(params.key);
        const provider = params.provider >= 0 ? providers[params.provider] : null;

        try {
            for (const certificate of params.certificates || []) {
                euSign.SaveCertificates(new Uint8Array(certificate));
            }

            if (/\.jks$/i.test(params.fileName)) {
                const alias = euSign.EnumJKSPrivateKeys(keyBytes, 0);
                const jks = euSign.GetJKSPrivateKey(keyBytes, alias);
                for (let i = 0; i < jks.GetCertificatesCount(); i++) {
                    euSign.SaveCertificate(jks.GetCertificate(i));
                }

                keyBytes = jks.GetPrivateKey();
            }

            if (provider) {
                applyProviderSettings(provider);
            }

            privateKey = readPrivateKey(keyBytes, params.password, provider);
            const owner = privateKey.GetOwnerInfo();
            const detected = provider || providers.find((p) => p.issuerCNs.includes(owner.issuerCN)) || null;
            applyProviderSettings(detected);

            const { info, algorithm } = resolveSigningCertificate();
            signAlgorithm = algorithm;
            return describeOwner(owner, info);
        } catch (error) {
            releaseKey();
            if (error && error.errorCode === END_OF_ENUM) {
                throw { errorCode: 0x0018 };
            }

            throw error;
        } finally {
            keyBytes.fill(0);
        }
    },

    /**
     * Signs data with the loaded key.
     * @param {{format: string, packaging: string, level: string, files: {name: string, data: ArrayBuffer}[]}} params The parameters.
     * @returns {{signature: Uint8Array, signedAt: string}} The signature.
     */
    sign(params) {
        if (!privateKey) {
            throw new Error(t("Спочатку зчитайте ключ."));
        }

        const files = params.files.map((f) => ({ name: f.name, data: new Uint8Array(f.data) }));
        const references = () => files.map((f) => new EndUserReference(f.name, f.data));
        const detached = params.packaging === "Detached";
        const cades = {
            "B-B": EU_SIGN_TYPE_CADES_BES,
            "B-T": EU_SIGN_TYPE_CADES_T,
            "B-LT": EU_SIGN_TYPE_CADES_X_LONG,
            "B-LTA": EU_SIGN_TYPE_CADES_X_LONG | EU_SIGN_TYPE_CADES_X_LONG_TRUSTED,
        }[params.level];
        const xades = { "B-B": EU_XADES_SIGN_LEVEL_B_B, "B-T": EU_XADES_SIGN_LEVEL_B_T, "B-LT": EU_XADES_SIGN_LEVEL_B_LT, "B-LTA": EU_XADES_SIGN_LEVEL_B_LTA }[params.level];
        const pades = { "B-B": EU_PADES_SIGN_LEVEL_B_B, "B-T": EU_PADES_SIGN_LEVEL_B_T, "B-LT": EU_PADES_SIGN_LEVEL_B_LT, "B-LTA": EU_PADES_SIGN_LEVEL_B_LTA }[params.level];

        let signature;
        switch (params.format) {
            case "CAdES":
                euSign.SetRuntimeParameter(EU_SIGN_TYPE_PARAMETER, cades);
                signature = euSign.CtxSignData(privateKey, signAlgorithm, files[0].data, detached, true, false);
                break;
            case "XAdES":
                signature = euSign.CtxXAdESSignData(privateKey, signAlgorithm, detached ? EU_XADES_TYPE_DETACHED : EU_XADES_TYPE_ENVELOPING, xades, references(), false);
                break;
            case "PAdES":
                signature = euSign.CtxPDFSignData(privateKey, signAlgorithm, files[0].data, pades, false);
                break;
            case "AsicS":
                euSign.SetRuntimeParameter(EU_SIGN_TYPE_PARAMETER, cades);
                signature = euSign.CtxASiCSignData(privateKey, signAlgorithm, EU_ASIC_TYPE_S, EU_ASIC_SIGN_TYPE_CADES, cades, references().slice(0, 1), false);
                break;
            case "AsicE":
                signature = euSign.CtxASiCSignData(privateKey, signAlgorithm, EU_ASIC_TYPE_E, EU_ASIC_SIGN_TYPE_XADES, xades, references(), false);
                break;
            default:
                throw new Error(t("Невідомий формат підпису."));
        }

        return { signature, signedAt: new Date().toISOString() };
    },

    /**
     * Verifies a signature container (CAdES, XAdES, PAdES or ASiC) with optional detached data.
     * @param {{files: {name: string, data: ArrayBuffer}[]}} params The files.
     * @returns {object} The verification result.
     */
    verify(params) {
        const files = params.files.map((f) => ({ name: f.name, data: new Uint8Array(f.data) }));
        const container = files.find((f) => /\.(p7s|pdf|xml|asics|asice|scs|sce|zip)$/i.test(f.name)) || files[0];
        const data = files.filter((f) => f !== container);
        const signatures = [];
        let format = "CAdES";
        let content = null;

        if (/\.pdf$/i.test(container.name)) {
            format = "PAdES";
            const claimed = pdfClaimedTimes(container.data);
            if (claimed.length === 0) {
                // The library reports a PDF without signatures as damaged data; say what actually happened.
                throw new Error(t("У цьому PDF немає підписів. Якщо файл завантажено з картки документа, візьміть підписаний PDF у розділі «Підписи», а не оригінал документа."));
            }

            const count = euSign.PDFGetSignsCount(container.data);
            for (let i = 0; i < count; i++) {
                signatures.push(describeSignature(euSign.PDFVerifyData(i, container.data), claimed[i] || null));
            }
        } else if (/\.(asics|asice|scs|sce|zip)$/i.test(container.name)) {
            format = "ASiC";
            const count = euSign.ASiCGetSignsCount(container.data);
            for (let i = 0; i < count; i++) {
                signatures.push(describeSignature(euSign.ASiCVerifyData(i, container.data)));
            }
        } else if (/\.xml$/i.test(container.name)) {
            format = "XAdES";
            const references = data.length > 0 ? data.map((f) => new EndUserReference(f.name, f.data)) : null;
            const count = euSign.XAdESGetSignsCount(container.data);
            for (let i = 0; i < count; i++) {
                signatures.push(describeSignature(euSign.XAdESVerifyData(references, i, container.data)));
            }
        } else {
            const internal = euSign.IsDataInSignedDataAvailable(container.data);
            if (!internal && data.length === 0) {
                throw new Error(t("Це зовнішній підпис: додайте також файл, який було підписано."));
            }

            const count = euSign.CtxGetSignsCount(verifyContext, container.data);
            for (let i = 0; i < count; i++) {
                const info = internal
                    ? euSign.VerifyDataInternalOnTimeEx(i, container.data, null, false, false)
                    : euSign.VerifyDataOnTimeEx(data[0].data, i, container.data, null, false, false);
                signatures.push(describeSignature(info));
            }

            if (internal) {
                content = {
                    name: container.name.replace(/\.p7s$/i, "") || "data.bin",
                    data: euSign.GetDataFromSignedData(container.data),
                };
            }
        }

        if (signatures.length === 0) {
            throw new Error(t("У файлі не знайдено підписів."));
        }

        return { format, container: container.name, signatures, content };
    },

    /**
     * Releases the private key.
     * @returns {boolean} Always true.
     */
    reset() {
        releaseKey();
        return true;
    },
};

self.onmessage = async (event) => {
    const { id, cmd, params } = event.data || {};
    try {
        const handler = handlers[cmd];
        if (!handler) {
            throw new Error(`Unknown command ${cmd}`);
        }

        const result = await handler(params || {});
        const transfer = [];
        if (result && result.signature instanceof Uint8Array) {
            transfer.push(result.signature.buffer);
        }

        if (result && result.content && result.content.data instanceof Uint8Array) {
            transfer.push(result.content.data.buffer);
        }

        self.postMessage({ id, result }, transfer);
    } catch (error) {
        self.postMessage({ id, error: normalizeError(error) });
    }
};
