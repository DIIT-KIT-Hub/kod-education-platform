/**
 * Browser-side signing UI built with the KOD design system classes.
 *
 * Modes (data-mode on the host element rendered by SigningHost.razor):
 *  - "document": signs a stored document (bytes are downloaded, their SHA-256 is checked) and uploads the signature;
 *  - "sign":     signs local files and offers the result for download (nothing is uploaded);
 *  - "verify":   verifies a signed file locally.
 *
 * Signing is a three-step wizard (design system kod-wizard.js): 1) the data, 2) the signature parameters,
 * 3) the key and the signature itself. The key is read only on the last step, right before signing, so the private
 * key spends as little time in memory as possible; it is released after signing, when the user goes back, when the
 * signing dialog closes and when the page goes away.
 *
 * The key file and password are read into memory, transferred to the Web Worker and never sent to the server.
 * All dynamic text is inserted with textContent (no innerHTML), so certificate fields cannot inject markup.
 * Interface texts are Ukrainian keys translated into the page language with `t()` (kodDs.t, catalog js/i18n/en.signing.js).
 */

import { createWizard } from "/js/kod-wizard.js";
import { EUSignClient } from "./eusign-client.js";

/**
 * Translates a Ukrainian interface text into the page language (see kod-ds.js).
 * @param {string} text The Ukrainian text.
 * @param {...(string|number)} args The placeholder values.
 * @returns {string} The text.
 */
const t = (text, ...args) => (window.kodDs ? window.kodDs.t(text, ...args) : text);

/**
 * Returns the locale for dates and numbers of the page language.
 * @returns {string} The locale.
 */
const locale = () => (document.documentElement.lang === "en" ? "en-GB" : "uk-UA");

/**
 * Signature formats supported by euscp.ex.js (built on use, so the texts follow the page language).
 * @returns {{id: string, title: string, hint: string, packaging: boolean, multiple: boolean, pdf?: boolean}[]} The formats.
 */
const FORMATS = () => [
    { id: "CAdES", title: "CAdES", hint: t("*.p7s · будь-який файл"), packaging: true, multiple: false },
    { id: "XAdES", title: "XAdES", hint: t("*.xml · один або кілька файлів"), packaging: true, multiple: true },
    { id: "PAdES", title: "PAdES", hint: t("підписаний PDF"), packaging: false, multiple: false, pdf: true },
    { id: "AsicS", title: "ASiC-S", hint: t("контейнер з 1 файлом"), packaging: false, multiple: false },
    { id: "AsicE", title: "ASiC-E", hint: t("контейнер з кількох файлів"), packaging: false, multiple: true },
];

/**
 * Packaging options of CAdES and XAdES.
 * @returns {{id: string, title: string, hint: string}[]} The options.
 */
const PACKAGINGS = () => [
    { id: "Detached", title: t("Окремо від даних"), hint: t("файл підпису поруч із документом") },
    { id: "Enveloped", title: t("Разом з даними"), hint: t("дані всередині підпису") },
];

/**
 * Signature levels (ETSI baseline profiles).
 * @returns {{id: string, title: string}[]} The levels.
 */
const LEVELS = () => [
    { id: "B-B", title: t("B-B — базовий") },
    { id: "B-T", title: t("B-T — з позначкою часу") },
    { id: "B-LT", title: t("B-LT — з даними для довготривалої перевірки") },
    { id: "B-LTA", title: t("B-LTA — архівний") },
];

/** File key extensions. */
const KEY_ACCEPT = ".dat,.pfx,.p12,.pk8,.jks,.zs2,.key";

/** Maximum data size (matches EU_MAX_DATA_SIZE_MB in the worker). */
const MAX_DATA_BYTES = 25 * 1024 * 1024;

/**
 * Creates an element.
 * @param {string} tag The tag name.
 * @param {Record<string, any>} [attributes] Attributes; "class", "text" and "on*" handlers are special.
 * @param {...(Node|string|null|undefined|false)} children Child nodes.
 * @returns {HTMLElement} The element.
 */
function el(tag, attributes = {}, ...children) {
    const node = document.createElement(tag);
    for (const [key, value] of Object.entries(attributes)) {
        if (value === null || value === undefined || value === false) {
            continue;
        }

        if (key === "class") {
            node.className = value;
        } else if (key === "text") {
            node.textContent = value;
        } else if (key.startsWith("on") && typeof value === "function") {
            node.addEventListener(key.substring(2), value);
        } else {
            node.setAttribute(key, value === true ? "" : String(value));
        }
    }

    for (const child of children) {
        if (child !== null && child !== undefined && child !== false) {
            node.append(child);
        }
    }

    return node;
}

/**
 * Creates an icon from the design system sprite.
 * @param {string} name The icon name.
 * @param {string} [extraClass] Additional classes.
 * @returns {SVGElement} The icon.
 */
function icon(name, extraClass = "") {
    const ns = "http://www.w3.org/2000/svg";
    const svg = document.createElementNS(ns, "svg");
    svg.setAttribute("class", `ds-icon ${extraClass}`.trim());
    svg.setAttribute("aria-hidden", "true");
    const use = document.createElementNS(ns, "use");
    use.setAttribute("href", `/icons/sprite.svg#${name}`);
    svg.append(use);
    return svg;
}

/**
 * Creates a design system button.
 * @param {string} text The label.
 * @param {{variant?: string, icon?: string, type?: string, block?: boolean, onclick?: Function, disabled?: boolean}} [options] Options.
 * @returns {HTMLButtonElement} The button.
 */
function button(text, options = {}) {
    const classes = ["ds-btn"];
    if (options.variant) {
        classes.push(`ds-btn--${options.variant}`);
    }

    if (options.block) {
        classes.push("ds-btn--block");
    }

    return /** @type {HTMLButtonElement} */ (el(
        "button",
        { type: options.type || "button", class: classes.join(" "), onclick: options.onclick, disabled: options.disabled },
        options.icon ? icon(options.icon) : null,
        el("span", { text })));
}

/**
 * Creates an alert.
 * @param {"neutral"|"inverse"|"outline"|"danger"} tone The tone.
 * @param {string} text The message.
 * @param {string} [title] The title.
 * @returns {HTMLElement} The alert.
 */
function alert(tone, text, title) {
    const iconName = tone === "danger" ? "circle-alert" : tone === "inverse" ? "circle-check" : "info";
    return el(
        "div",
        { class: `ds-alert${tone === "neutral" ? "" : ` ds-alert--${tone}`}`, role: tone === "danger" ? "alert" : "status" },
        icon(iconName),
        el("div", { class: "ds-alert__body" }, title ? el("p", { class: "ds-alert__title", text: title }) : null, el("p", { text })));
}

/**
 * Creates a labelled field.
 * @param {string} label The label.
 * @param {HTMLElement} control The control, or a wrapper around it (the label points at the input inside).
 * @param {string} [hint] The hint.
 * @returns {HTMLElement} The field.
 */
function field(label, control, hint) {
    const target = control.matches("input, select, textarea") ? control : control.querySelector("input, select, textarea") || control;
    if (!target.id) {
        target.id = `sig-${Math.random().toString(36).slice(2, 9)}`;
    }

    return el("div", { class: "ds-field" }, el("label", { class: "ds-label", for: target.id, text: label }), control, hint ? el("span", { class: "ds-hint", text: hint }) : null);
}

/**
 * Creates a segmented radio group.
 * @param {string} name The group name.
 * @param {{id: string, title: string, hint?: string, disabled?: boolean}[]} options The options.
 * @param {string} value The selected value.
 * @param {(value: string) => void} onChange The change handler.
 * @returns {HTMLElement} The group.
 */
function segmented(name, options, value, onChange) {
    const group = el("div", { class: "ds-segmented", role: "radiogroup" });
    for (const option of options) {
        const input = el("input", { type: "radio", name, value: option.id, checked: option.id === value, disabled: option.disabled });
        input.addEventListener("change", () => onChange(option.id));
        group.append(el(
            "label",
            { class: "ds-segmented__option" },
            input,
            el("span", { class: "ds-segmented__label" }, option.title, option.hint ? el("small", { text: option.hint }) : null)));
    }

    return group;
}

/**
 * Creates a drop zone with a file input.
 * @param {{title: string, hint: string, accept?: string, multiple?: boolean, listed?: boolean, onFiles: (files: File[]) => void}} options
 * Options; `listed` means the caller lists the chosen files itself, so the title keeps its text.
 * @returns {{root: HTMLElement, clear: () => void}} The drop zone and a function that forgets the chosen files.
 */
function dropzone(options) {
    const input = /** @type {HTMLInputElement} */ (el("input", { type: "file", accept: options.accept, multiple: options.multiple }));
    const names = el("span", { class: "ds-dropzone__title", text: options.title });
    input.addEventListener("change", () => {
        const files = Array.from(input.files || []);
        names.textContent = files.length > 0 && !options.listed ? files.map((f) => f.name).join(", ") : options.title;
        options.onFiles(files);
    });

    const root = el(
        "label",
        { class: "ds-dropzone" },
        input,
        el("span", { class: "ds-empty__icon" }, icon("file-up", "ds-icon--lg")),
        names,
        el("span", { class: "ds-dropzone__hint", text: options.hint }));

    return {
        root,
        clear: () => {
            input.value = "";
            names.textContent = options.title;
            options.onFiles([]);
        },
    };
}

/**
 * Reports a file signing to the audit log (names and parameters only). A failure never affects the signature the user
 * already has, so it is not shown.
 * @param {any} owner The key owner.
 * @param {string[]} fileNames The signed file names.
 * @param {{format: string, level: string}} meta The signature parameters.
 */
function recordFilesSigned(owner, fileNames, meta) {
    const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute("content") || "";
    fetch("/api/v1/signing/journal", {
        method: "POST",
        credentials: "same-origin",
        headers: { "Content-Type": "application/json", "X-CSRF-TOKEN": token },
        body: JSON.stringify({
            fileNames,
            format: meta.format,
            level: meta.level || null,
            signerName: owner.fullName || owner.commonName || null,
            certificateSerial: owner.serial || null,
            issuer: owner.issuer || null,
        }),
    }).catch(() => { /* The log entry is best effort. */ });
}

/**
 * Formats an ISO date in Kyiv time.
 * @param {string|null} iso The ISO date.
 * @returns {string} The formatted date.
 */
function formatDate(iso) {
    if (!iso) {
        return "—";
    }

    return new Intl.DateTimeFormat(locale(), { dateStyle: "long", timeStyle: "short", timeZone: "Europe/Kyiv" }).format(new Date(iso));
}

/**
 * Formats a size in bytes.
 * @param {number} bytes The size.
 * @returns {string} The formatted size.
 */
function formatSize(bytes) {
    const units = [t("Б"), t("КБ"), t("МБ")];
    let value = bytes;
    let unit = 0;
    while (value >= 1024 && unit < units.length - 1) {
        value /= 1024;
        unit++;
    }

    return `${new Intl.NumberFormat(locale(), { maximumFractionDigits: unit === 0 ? 0 : 1 }).format(value)} ${units[unit]}`;
}

/**
 * Builds a description list.
 * @param {[string, string|null|undefined][]} rows Term/value pairs; empty values are skipped.
 * @returns {HTMLElement} The list.
 */
function descriptionList(rows) {
    const list = el("dl", { class: "ds-dl" });
    for (const [term, value] of rows) {
        if (value) {
            list.append(el("dt", { text: term }), el("dd", { text: value }));
        }
    }

    return list;
}

/**
 * Offers bytes as a file download.
 * @param {Uint8Array} bytes The content.
 * @param {string} fileName The file name.
 * @returns {HTMLAnchorElement} A download link.
 */
function downloadLink(bytes, fileName) {
    const url = URL.createObjectURL(new Blob([bytes], { type: "application/octet-stream" }));
    const link = /** @type {HTMLAnchorElement} */ (el("a", { class: "ds-btn", href: url, download: fileName }, icon("download"), el("span", { text: t("Завантажити {0}", fileName) })));
    window.setTimeout(() => URL.revokeObjectURL(url), 10 * 60 * 1000);
    return link;
}

/**
 * Computes the SHA-256 digest of bytes as lowercase hex.
 * @param {ArrayBuffer} buffer The data.
 * @returns {Promise<string>} The digest.
 */
async function sha256(buffer) {
    const digest = await crypto.subtle.digest("SHA-256", buffer);
    return Array.from(new Uint8Array(digest), (b) => b.toString(16).padStart(2, "0")).join("");
}

/**
 * Suggests the file name of a signature.
 * @param {string} format The format.
 * @param {string[]} names The signed file names.
 * @returns {string} The file name.
 */
function signatureFileName(format, names) {
    const first = names[0] || "document";
    const base = first.replace(/\.[^.]+$/, "");
    switch (format) {
        case "XAdES":
            return names.length === 1 ? `${first}.xml` : "signature.xml";
        case "PAdES":
            return `${base}_signed.pdf`;
        case "AsicS":
            return `${first}.asics`;
        case "AsicE":
            return `${base}.asice`;
        default:
            return `${first}.p7s`;
    }
}

/**
 * Reads the configuration from the host element.
 * @param {HTMLElement} host The host element.
 * @returns {import("./eusign-client.js").SigningConfig} The configuration.
 */
function readConfig(host) {
    const absolute = (path) => new URL(path, document.baseURI).href;
    return {
        worker: absolute(host.dataset.worker),
        scripts: [absolute(host.dataset.libTypes), absolute(host.dataset.libModule), absolute(host.dataset.libCore)],
        proxy: absolute(host.dataset.proxy),
        casUrl: absolute(host.dataset.cas),
        certificatesUrl: absolute(host.dataset.certificates),
    };
}

/**
 * Builds the key panel: key type, provider, key file, password and optional certificates; after the key is read,
 * the card of its owner.
 * @param {EUSignClient} client The worker client.
 * @param {Promise<{providers: string[]}>} ready Resolves when the library is loaded.
 * @param {(owner: any|null) => void} onKey Called when a key is read or released.
 * @returns {{root: HTMLElement, owner: () => any|null, forget: () => Promise<void>}} The panel.
 */
function keyPanel(client, ready, onKey) {
    /** @type {File|null} */
    let keyFile = null;
    /** @type {File[]} */
    let certificateFiles = [];
    let owner = null;

    const root = el("div", { class: "ds-stack" });
    const provider = /** @type {HTMLSelectElement} */ (el("select", { class: "ds-select" }, el("option", { value: "-1", text: t("Визначити автоматично") })));
    ready.then((info) => info.providers.forEach((name, index) => provider.append(el("option", { value: String(index), text: name })))).catch(() => { });

    const password = /** @type {HTMLInputElement} */ (el("input", { type: "password", class: "ds-input", autocomplete: "off", spellcheck: "false" }));
    const passwordField = el(
        "div",
        { class: "ds-password" },
        password,
        el(
            "button",
            { type: "button", class: "ds-password__toggle", "data-password-toggle": true, "aria-pressed": "false", "aria-label": t("Показати пароль"), title: t("Показати пароль") },
            icon("eye", "ds-password__show"),
            icon("eye-off", "ds-password__hide")));
    const status = el("div", { class: "ds-stack ds-stack--sm", "aria-live": "polite" });
    const readButton = button(t("Зчитати ключ"), { icon: "key-round", type: "submit", block: true });

    const keyDrop = dropzone({
        title: t("Оберіть файл ключа"),
        hint: "Key-6.dat, *.pfx, *.p12, *.pk8, *.jks, *.zs2",
        accept: KEY_ACCEPT,
        onFiles: (files) => {
            keyFile = files[0] || null;
        },
    });

    const certificateDrop = dropzone({
        title: t("Файли сертифікатів"),
        hint: "*.cer, *.crt, *.p7b",
        accept: ".cer,.crt,.der,.p7b",
        multiple: true,
        onFiles: (files) => {
            certificateFiles = files;
        },
    });

    const form = /** @type {HTMLFormElement} */ (el(
        "form",
        { class: "ds-stack" },
        segmented("key-type", [
            { id: "file", title: t("Файловий ключ"), hint: "Key-6.dat, PFX, JKS…" },
            { id: "token", title: t("Захищений носій"), hint: t("потребує агента ІІТ"), disabled: true },
            { id: "cloud", title: t("Хмарний / Дія.Підпис"), hint: t("потребує сервісу надавача"), disabled: true },
        ], "file", () => { }),
        field(t("Надавач електронних довірчих послуг"), provider),
        keyDrop.root,
        field(t("Пароль захисту ключа"), passwordField, t("Пароль не передається на сервер.")),
        el(
            "details",
            {},
            el("summary", { class: "ds-link ds-text-sm", text: t("Додати сертифікати вручну (якщо надавач не підтримує автоматичне завантаження)") }),
            el("div", { class: "ds-stack ds-stack--sm" }, certificateDrop.root)),
        status,
        readButton));

    const card = el("div", { class: "ds-stack" });
    card.hidden = true;
    root.append(form, card);

    /**
     * Releases the key in the worker and returns the panel to the empty form.
     * @returns {Promise<void>} Resolves when the key is released.
     */
    async function forget() {
        const hadKey = owner !== null;
        owner = null;
        password.value = "";
        window.kodDs?.togglePassword(passwordField.querySelector("[data-password-toggle]"), true);
        keyDrop.clear();
        certificateDrop.clear();
        status.replaceChildren();
        card.replaceChildren();
        card.hidden = true;
        form.hidden = false;
        if (hadKey) {
            onKey(null);
        }

        await client.resetKey().catch(() => { });
    }

    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        status.replaceChildren();
        if (!keyFile) {
            status.append(alert("danger", t("Оберіть файл ключа.")));
            return;
        }

        if (!password.value) {
            status.append(alert("danger", t("Введіть пароль.")));
            password.focus();
            return;
        }

        readButton.setAttribute("data-busy", "true");
        readButton.disabled = true;
        try {
            await ready;
            const keyBuffer = await keyFile.arrayBuffer();
            const certBuffers = await Promise.all(certificateFiles.map((f) => f.arrayBuffer()));
            const secret = password.value;
            password.value = "";
            owner = await client.readKey(keyFile.name, keyBuffer, secret, Number(provider.value), certBuffers);
            form.hidden = true;
            card.replaceChildren(ownerCard(owner, () => forget()));
            card.hidden = false;
            onKey(owner);
        } catch (error) {
            status.append(alert("danger", error.message || String(error)));
            password.focus();
        } finally {
            readButton.removeAttribute("data-busy");
            readButton.disabled = false;
        }
    });

    return { root, owner: () => owner, forget };
}

/**
 * Builds the card that shows the key owner.
 * @param {any} owner The key owner.
 * @param {() => void} onForget Called when the user wants another key.
 * @returns {HTMLElement} The card.
 */
function ownerCard(owner, onForget) {
    return el(
        "div",
        { class: "sign-owner" },
        el("div", { class: "ds-row ds-row--between" }, el("strong", { text: owner.fullName || owner.commonName }), button(t("Інший ключ"), { variant: "ghost", icon: "refresh-cw", onclick: onForget })),
        descriptionList([
            [t("Організація"), owner.organization],
            [t("Посада"), owner.title],
            [t("РНОКПП"), owner.drfo],
            [t("ЄДРПОУ"), owner.edrpou],
            [t("Надавач"), owner.issuer],
            [t("Сертифікат №"), owner.serial],
            [t("Дійсний до"), owner.validTo ? formatDate(owner.validTo) : null],
        ]));
}

/**
 * Builds the signature parameters (format, packaging, level).
 * @param {{pdfOnly: () => boolean, multiple: () => boolean}} context The data context.
 * @returns {{root: HTMLElement, refresh: () => void, value: () => {format: string, packaging: string, level: string}}} The control.
 */
function parametersBlock(context) {
    let format = "CAdES";
    let packaging = "Detached";
    let level = "B-LT";
    const root = el("div", { class: "ds-stack" });

    const levelSelect = /** @type {HTMLSelectElement} */ (el("select", { class: "ds-select" }, ...LEVELS().map((l) => el("option", { value: l.id, text: l.title, selected: l.id === level }))));
    levelSelect.addEventListener("change", () => {
        level = levelSelect.value;
    });

    const render = () => {
        const available = FORMATS().map((f) => ({
            id: f.id,
            title: f.title,
            hint: f.hint,
            disabled: (f.pdf && !context.pdfOnly()) || (!f.multiple && context.multiple()),
        }));
        if (available.find((f) => f.id === format)?.disabled) {
            format = available.find((f) => !f.disabled)?.id || "CAdES";
        }

        const meta = FORMATS().find((f) => f.id === format);
        // replaceChildren() would print a null as the text "null", so the optional packaging field is filtered out.
        root.replaceChildren(...[
            field(t("Формат підпису"), segmented("sign-format", available, format, (value) => {
                format = value;
                render();
            })),
            meta.packaging
                ? field(t("Тип підпису"), segmented("sign-packaging", PACKAGINGS(), packaging, (value) => {
                    packaging = value;
                }))
                : null,
            field(t("Рівень підпису"), levelSelect, t("B-LT містить усе для перевірки через роки (рекомендовано).")),
        ].filter(Boolean));
    };

    render();
    return {
        root,
        refresh: render,
        value: () => ({ format, packaging: FORMATS().find((f) => f.id === format).packaging ? packaging : "Enveloped", level }),
    };
}

/**
 * Describes the chosen parameters for the summary.
 * @param {{format: string, packaging: string, level: string}} value The parameters.
 * @returns {[string, string][]} Term/value pairs.
 */
function describeParameters(value) {
    const format = FORMATS().find((f) => f.id === value.format);
    return [
        [t("Формат"), format.packaging ? `${format.title} · ${PACKAGINGS().find((p) => p.id === value.packaging).title.toLowerCase()}` : format.title],
        [t("Рівень"), LEVELS().find((l) => l.id === value.level).title],
    ];
}

/**
 * Renders the signing wizard (document and local files modes).
 * @param {HTMLElement} host The host element.
 * @param {EUSignClient} client The worker client.
 * @param {Promise<void>} visible Resolves when the panel is first shown.
 * @param {Promise<{providers: string[]}>} ready Resolves when the library is loaded.
 */
function renderSigning(host, client, visible, ready) {
    const documentMode = host.dataset.mode === "document";
    /** @type {{name: string, size: number, file?: File, buffer?: ArrayBuffer}[]} */
    let files = [];
    let integrityOk = !documentMode;

    // Step 1: the data.
    const dataBody = el("div", { class: "ds-stack" });
    const dataStatus = el("div", { class: "ds-stack ds-stack--sm", "aria-live": "polite" });
    if (documentMode) {
        dataStatus.append(el("div", { class: "ds-progress ds-progress--indeterminate" }, el("div", { class: "ds-progress__bar" })));
        dataBody.append(
            el("div", { class: "doc-file" }, el("span", { class: "doc-file__icon" }, icon("file-text")), el("span", { class: "doc-file__name", text: host.dataset.fileName })),
            dataStatus);

        // The file is downloaded only when the signing panel is actually opened.
        visible
            .then(() => fetch(host.dataset.fileUrl, { credentials: "same-origin" }))
            .then((response) => {
                if (!response.ok) {
                    throw new Error(t("Не вдалося завантажити файл документа."));
                }

                return response.arrayBuffer();
            })
            .then(async (buffer) => {
                if (buffer.byteLength > MAX_DATA_BYTES) {
                    throw new Error(t("Файл завеликий для підписання у браузері (понад 25 МБ)."));
                }

                const actual = await sha256(buffer);
                if (actual !== (host.dataset.fileSha256 || "").toLowerCase()) {
                    throw new Error(t("Контрольна сума файлу не збігається з зареєстрованою. Підписання заборонено."));
                }

                files = [{ name: host.dataset.fileName, size: buffer.byteLength, buffer }];
                integrityOk = true;
                dataStatus.replaceChildren(alert("outline", t("SHA-256 файлу збігається з зареєстрованим у системі."), t("Цілісність підтверджено")));
                wizard.refresh();
            })
            .catch((error) => dataStatus.replaceChildren(alert("danger", error.message)));
    } else {
        const fileList = el("ul", { class: "sign-files" });
        dataBody.append(
            dropzone({
                title: t("Перетягніть файли або натисніть, щоб обрати"),
                hint: t("До 25 МБ кожен. Для XAdES та ASiC-E можна обрати кілька файлів; для PAdES — PDF."),
                multiple: true,
                listed: true,
                onFiles: (selected) => {
                    dataStatus.replaceChildren();
                    files = selected.map((file) => ({ name: file.name, size: file.size, file }));
                    if (selected.some((f) => f.size > MAX_DATA_BYTES)) {
                        files = [];
                        dataStatus.append(alert("danger", t("Файли понад 25 МБ не підтримуються.")));
                    }

                    fileList.replaceChildren(...files.map((f) => el(
                        "li",
                        { class: "doc-file" },
                        el("span", { class: "doc-file__icon" }, icon("file-text")),
                        el("span", { class: "doc-file__name", text: f.name }),
                        el("span", { class: "ds-text-sm ds-muted", text: formatSize(f.size) }))));
                    wizard.refresh();
                },
            }).root,
            fileList,
            dataStatus);
    }

    // Step 2: the parameters.
    const parameters = parametersBlock({
        pdfOnly: () => files.length > 0 && files.every((f) => /\.pdf$/i.test(f.name)),
        multiple: () => files.length > 1,
    });

    // Step 3: the summary, the key and the signature.
    const summary = el("div", { class: "sign-summary" });
    const signStatus = el("div", { class: "ds-stack ds-stack--sm", "aria-live": "polite" });
    const key = keyPanel(client, ready, () => wizard.refresh());
    const keyBody = el("div", { class: "ds-stack" }, summary, key.root, signStatus);

    const wizard = createWizard({
        label: documentMode ? t("Підписання документа") : t("Підписання файлів"),
        steps: [
            {
                id: "data",
                title: documentMode ? t("Документ") : t("Файли"),
                description: documentMode ? t("Перевіряємо, що файл не змінювався після реєстрації.") : t("Файли не залишають ваш пристрій."),
                body: dataBody,
                isComplete: () => integrityOk && files.length > 0,
            },
            {
                id: "parameters",
                title: t("Параметри"),
                description: t("Формат і рівень кваліфікованого електронного підпису."),
                body: parameters.root,
                onEnter: () => parameters.refresh(),
            },
            {
                id: "key",
                title: t("Ключ і підпис"),
                description: t("Ключ зчитується лише у вашому браузері; пароль не передається на сервер."),
                body: keyBody,
                isComplete: () => key.owner() !== null,
                onEnter: () => {
                    // The stepper can jump here from the data step, so the parameters are re-validated for the current files.
                    parameters.refresh();
                    signStatus.replaceChildren();
                    summary.replaceChildren(descriptionList([
                        documentMode ? [t("Документ"), host.dataset.documentTitle || host.dataset.fileName] : [t("Файли"), files.map((f) => f.name).join(", ")],
                        ...describeParameters(parameters.value()),
                    ]));
                },
                // The private key never outlives the step it is needed for.
                onLeave: () => {
                    key.forget();
                },
            },
        ],
        finish: {
            text: t("Підписати"),
            icon: "signature",
            run: () => sign(),
        },
    });

    /**
     * Signs the data with the key that was read on the last step.
     * @returns {Promise<void>} Resolves when done (errors are shown in the step).
     */
    async function sign() {
        signStatus.replaceChildren();
        const owner = key.owner();

        // Re-check every precondition here instead of trusting the state of the buttons.
        if (!owner || files.length === 0 || !integrityOk) {
            signStatus.append(alert("danger", t("Не всі кроки завершено.")));
            return;
        }

        const { format, packaging, level } = parameters.value();
        const fileName = signatureFileName(format, files.map((f) => f.name));
        let signed;
        try {
            const payload = await Promise.all(files.map(async (f) => ({ name: f.name, data: f.buffer ? f.buffer.slice(0) : await f.file.arrayBuffer() })));
            signed = await client.sign(format, packaging, level, payload);
            if (documentMode) {
                await uploadSignature(host, owner, signed, { format, packaging, level, fileName });
            }
        } catch (error) {
            signStatus.append(alert("danger", error.message || String(error)));
            return;
        }

        if (!documentMode) {
            // The files stay on this device; only their names and the signature parameters go to the audit log.
            recordFilesSigned(owner, files.map((f) => f.name), { format, level });
        }

        await key.forget();
        if (documentMode) {
            host.replaceChildren(successCard(t("Документ підписано"), t("Підпис збережено. Сторінка оновиться автоматично.")));
            window.setTimeout(() => window.location.reload(), 1400);
            return;
        }

        host.replaceChildren(successCard(
            t("Файл підписано"),
            `${describeParameters({ format, packaging, level }).map(([, value]) => value).join(" · ")} · ${formatDate(signed.signedAt)}`,
            downloadLink(signed.signature, fileName),
            button(t("Підписати інші файли"), { variant: "ghost", icon: "refresh-cw", onclick: () => renderSigning(host, client, visible, ready) })));
    }

    // Closing the signing dialog releases the key; the chosen data and parameters stay.
    const dialog = host.closest("dialog");
    if (dialog) {
        dialog.addEventListener("close", () => {
            if (!wizard.busy()) {
                key.forget();
            }
        });
    }

    host.replaceChildren(wizard.root);
}

/**
 * Builds the result card shown after a successful signature.
 * @param {string} title The title.
 * @param {string} text The details.
 * @param {...HTMLElement} actions Actions under the text.
 * @returns {HTMLElement} The card.
 */
function successCard(title, text, ...actions) {
    return el(
        "div",
        { class: "sign-result" },
        el("span", { class: "sign-result__icon" }, icon("badge-check", "ds-icon--xl")),
        el("h3", { text: title }),
        el("p", { class: "ds-muted", text }),
        actions.length > 0 ? el("div", { class: "sign-result__actions" }, ...actions) : null);
}

/**
 * Uploads a document signature to the REST API.
 * @param {HTMLElement} host The host element.
 * @param {any} owner The key owner.
 * @param {{signature: Uint8Array, signedAt: string}} signed The signature.
 * @param {{format: string, packaging: string, level: string, fileName: string}} meta The signature metadata.
 * @returns {Promise<void>} Resolves when stored.
 */
async function uploadSignature(host, owner, signed, meta) {
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
        form.append("SubjectTaxId", owner.drfo || owner.edrpou);
    }

    if (owner.validFrom) {
        form.append("ValidFrom", owner.validFrom);
    }

    if (owner.validTo) {
        form.append("ValidTo", owner.validTo);
    }

    form.append("Signature", new Blob([signed.signature], { type: "application/octet-stream" }), meta.fileName);

    const token = document.querySelector('meta[name="csrf-token"]')?.getAttribute("content") || "";
    const response = await fetch(host.dataset.uploadUrl, {
        method: "POST",
        body: form,
        credentials: "same-origin",
        headers: { "X-CSRF-TOKEN": token },
    });

    if (!response.ok) {
        let message = t("Не вдалося зберегти підпис.");
        try {
            const problem = await response.json();
            message = problem.detail || problem.title || message;
        } catch (e) {
            /* Not a problem details response. */
        }

        throw new Error(message);
    }
}

/**
 * Renders the verification flow.
 * @param {HTMLElement} host The host element.
 * @param {EUSignClient} client The worker client.
 * @param {Promise<{providers: string[]}>} ready Resolves when the library is loaded.
 */
function renderVerification(host, client, ready) {
    /** @type {File[]} */
    let files = [];
    const result = el("div", { class: "ds-stack", "aria-live": "polite" });
    const verifyButton = button(t("Перевірити"), { icon: "badge-check", block: true, disabled: true });

    verifyButton.addEventListener("click", async () => {
        result.replaceChildren();
        verifyButton.setAttribute("data-busy", "true");
        try {
            await ready;
            const payload = await Promise.all(files.map(async (f) => ({ name: f.name, data: await f.arrayBuffer() })));
            const report = await client.verify(payload);
            result.append(alert("inverse", t("{0} · {1} · підписів: {2}", report.format, report.container, report.signatures.length), t("Підписи дійсні")));
            report.signatures.forEach((signature, index) => {
                result.append(el(
                    "div",
                    { class: "sign-owner" },
                    el("strong", { text: `${index + 1}. ${signature.signer.fullName || signature.signer.commonName}` }),
                    descriptionList([
                        [t("Організація"), signature.signer.organization],
                        [t("РНОКПП"), signature.signer.drfo],
                        [t("ЄДРПОУ"), signature.signer.edrpou],
                        [t("Надавач"), signature.signer.issuer],
                        [t("Сертифікат №"), signature.signer.serial],
                        [
                            signature.isTimeStamp ? t("Позначка часу") : signature.isClaimed ? t("Час підпису (вказаний у документі)") : t("Час підпису"),
                            formatDate(signature.time),
                        ],
                    ])));
            });

            if (report.content) {
                result.append(downloadLink(report.content.data, report.content.name));
            }
        } catch (error) {
            result.append(alert("danger", error.message || String(error), t("Перевірку не пройдено")));
        } finally {
            verifyButton.removeAttribute("data-busy");
        }
    });

    host.replaceChildren(
        dropzone({
            title: t("Перетягніть підписаний файл (і за потреби оригінал)"),
            hint: t("*.p7s, *.pdf, *.xml, *.asics, *.asice. Для зовнішнього підпису додайте також підписаний файл."),
            multiple: true,
            onFiles: (selected) => {
                files = selected;
                verifyButton.disabled = files.length === 0;
                result.replaceChildren();
            },
        }).root,
        result,
        verifyButton);
}

/**
 * Mounts the signing UI into a host element rendered by SigningHost.razor.
 * @param {HTMLElement} host The host element.
 */
export function mountSigningPanel(host) {
    const client = new EUSignClient(readConfig(host));

    // The library (~3 MB compressed) and the document are loaded only when the panel becomes visible
    // (e.g. the signing dialog opens); by the last wizard step the library is ready.
    const visible = new Promise((resolve) => {
        const observer = new IntersectionObserver((entries) => {
            if (entries.some((entry) => entry.isIntersecting)) {
                observer.disconnect();
                resolve();
            }
        });
        observer.observe(host);
    });
    const ready = visible.then(() => client.init());
    ready.catch((error) => host.prepend(alert("danger", error.message || t("Не вдалося завантажити бібліотеку підпису."))));

    if (host.dataset.mode === "verify") {
        renderVerification(host, client, ready);
    } else {
        renderSigning(host, client, visible, ready);
    }

    // Wipe the key when the page goes away (full or enhanced navigation).
    const dispose = () => {
        if (!host.isConnected) {
            client.dispose();
        }
    };
    window.addEventListener("pagehide", () => client.dispose(), { once: true });
    if (window.Blazor) {
        window.Blazor.addEventListener("enhancedload", dispose);
    }
}
