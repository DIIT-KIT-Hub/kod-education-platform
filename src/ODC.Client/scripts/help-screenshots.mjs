// Takes the screenshots of the manual (src/features/help/images/<language>/<name>.webp) from a running cabinet with
// demo data, in both languages, at 1440 × 900 in the light theme. It drives a headless Microsoft Edge (or Chrome) over
// the DevTools protocol, so it needs no packages.
//
// Run it against your own verification instance (never the production one), with an account that holds every
// permission:
//   ODC_URL=https://localhost:7181 ODC_EMAIL=admin@odc.local ODC_PASSWORD=… ODC_TOTP_KEY="abcd efgh …" \
//     node scripts/help-screenshots.mjs [name …]
// Names limit the run to some screenshots. The account's language is set back to Ukrainian at the end.
import { spawn } from "node:child_process";
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const base = (process.env.ODC_URL ?? "https://localhost:7181").replace(/\/$/, "");
const email = process.env.ODC_EMAIL ?? "";
const password = process.env.ODC_PASSWORD ?? "";
const totpKey = (process.env.ODC_TOTP_KEY ?? "").replace(/\s/g, "").toUpperCase();
const only = new Set(process.argv.slice(2));
const output = join(dirname(fileURLToPath(import.meta.url)), "..", "src", "features", "help", "images");
const width = 1440;
const height = 900;

if (!email || !password || !totpKey) {
    console.error("Set ODC_EMAIL, ODC_PASSWORD and ODC_TOTP_KEY (see the comment at the top of the script).");
    process.exit(1);
}

/**
 * Computes the current authenticator code (RFC 6238, SHA-1, 30 s, 6 digits).
 * @param offset Steps to add (1 = the next code).
 * @returns The code.
 */
function totp(offset = 0) {
    const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    const bits = [...totpKey].map((c) => alphabet.indexOf(c).toString(2).padStart(5, "0")).join("");
    const key = Buffer.from(bits.match(/.{8}/g).map((b) => parseInt(b, 2)));
    const counter = Buffer.alloc(8);
    counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30_000) + offset));
    const hmac = createHmac("sha1", key).update(counter).digest();
    const at = hmac[hmac.length - 1] & 15;
    return ((hmac.readUInt32BE(at) & 0x7fffffff) % 1_000_000).toString().padStart(6, "0");
}

/**
 * Waits a while.
 * @param ms Milliseconds.
 * @returns A promise resolved after the delay.
 */
function sleep(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Finds an installed Chromium browser.
 * @returns The executable.
 */
function browserPath() {
    const candidates = [
        process.env.ODC_BROWSER,
        "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
        "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
        "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
        "/usr/bin/google-chrome",
        "/usr/bin/chromium",
    ].filter(Boolean);
    const found = candidates.find((path) => existsSync(path));
    if (!found) {
        throw new Error("No Edge or Chrome found: set ODC_BROWSER to its executable.");
    }

    return found;
}

/**
 * A DevTools protocol session with one page.
 */
class Page {
    /**
     * Opens the connection.
     * @param socket The browser's WebSocket.
     * @param sessionId The page session.
     */
    constructor(socket, sessionId) {
        this.socket = socket;
        this.sessionId = sessionId;
    }

    /**
     * Sends a command to the page.
     * @param method The protocol method.
     * @param params Its parameters.
     * @returns The result.
     */
    send(method, params = {}) {
        return this.socket.send(method, params, this.sessionId);
    }

    /**
     * Evaluates an expression in the page (awaiting a promise).
     * @param expression The JavaScript expression.
     * @returns Its value.
     */
    async evaluate(expression) {
        const { result, exceptionDetails } = await this.send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
        if (exceptionDetails) {
            throw new Error(`${exceptionDetails.text}: ${exceptionDetails.exception?.description ?? expression}`);
        }

        return result.value;
    }

    /**
     * Waits until an expression is true.
     * @param expression The condition.
     * @param timeout How long to wait in milliseconds.
     * @returns A promise resolved when the condition holds.
     */
    async waitFor(expression, timeout = 20_000) {
        const until = Date.now() + timeout;
        while (Date.now() < until) {
            if (await this.evaluate(`Boolean(${expression})`).catch(() => false)) {
                return;
            }

            await sleep(150);
        }

        throw new Error(`Timed out waiting for ${expression}`);
    }

    /**
     * Opens an address and waits for a text (and for the skeletons to go).
     * @param path The address of the cabinet.
     * @param text A text the page shows when ready.
     * @returns A promise resolved when the page is ready.
     */
    async open(path, text) {
        await this.send("Page.navigate", { url: base + path });
        await this.waitFor(`document.readyState === "complete" && document.body.innerText.includes(${JSON.stringify(text)})`);
        await this.settle();
    }

    /**
     * Waits for the loading placeholders and animations to finish.
     * @returns A promise resolved when the page is still.
     */
    async settle() {
        await this.waitFor(`!document.querySelector('[class*="skeleton" i], [aria-busy="true"]')`, 4000).catch(() => undefined);
        await sleep(900);
    }

    /**
     * Saves a screenshot of the viewport or of an element.
     * @param file The file.
     * @param selector An element to crop to (with a margin), or null for the viewport.
     * @returns A promise resolved when saved.
     */
    async capture(file, selector) {
        let clip;
        if (selector) {
            const box = await this.evaluate(
                `(() => { const r = document.querySelector(${JSON.stringify(selector)})?.getBoundingClientRect(); return r && { x: r.x, y: r.y, width: r.width, height: r.height }; })()`,
            );
            if (!box) {
                throw new Error(`No element ${selector}`);
            }

            const margin = 16;
            const x = Math.max(0, box.x - margin);
            const y = Math.max(0, box.y - margin);
            clip = {
                x,
                y,
                width: Math.min(width - x, box.width + 2 * margin),
                height: Math.min(height - y, box.height + 2 * margin),
                scale: 1,
            };
        }

        const { data } = await this.send("Page.captureScreenshot", { format: "webp", quality: 80, ...(clip ? { clip } : {}) });
        mkdirSync(dirname(file), { recursive: true });
        writeFileSync(file, Buffer.from(data, "base64"));
    }

    /**
     * Fills an input the way a person does (React sees the change).
     * @param selector The input.
     * @param value The text.
     * @returns A promise resolved when typed.
     */
    async fill(selector, value) {
        await this.waitFor(`document.querySelector(${JSON.stringify(selector)})`);
        await this.evaluate(`(() => {
            const input = document.querySelector(${JSON.stringify(selector)});
            const setter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(input), "value").set;
            input.focus();
            setter.call(input, ${JSON.stringify(value)});
            input.dispatchEvent(new Event("input", { bubbles: true }));
        })()`);
    }

    /**
     * Clicks the first button or link whose text contains a value.
     * @param text The text.
     * @returns A promise resolved after the click.
     */
    async click(text) {
        await this.waitFor(`[...document.querySelectorAll("button, a")].some((e) => e.innerText.includes(${JSON.stringify(text)}))`);
        await this.evaluate(
            `(() => { const all = [...document.querySelectorAll("button, a")]; (all.find((e) => e.innerText.trim() === ${JSON.stringify(text)}) ?? all.find((e) => e.innerText.includes(${JSON.stringify(text)}))).click(); })()`,
        );
    }

    /**
     * Calls the API of the cabinet from the page (with the antiforgery token).
     * @param method The HTTP method.
     * @param path The API path.
     * @param body A JSON body or a FormData expression (as source text, prefixed with "form:").
     * @returns The JSON answer, or null.
     */
    async api(method, path, body) {
        const payload =
            typeof body === "string" && body.startsWith("form:")
                ? body.slice(5)
                : body === undefined
                  ? "undefined"
                  : `JSON.stringify(${JSON.stringify(body)})`;
        return this.evaluate(`(async () => {
            const context = await (await fetch("/api/v1/auth/context")).json();
            const body = ${payload};
            const headers = { "X-CSRF-TOKEN": context.antiforgeryToken };
            if (typeof body === "string") headers["Content-Type"] = "application/json";
            const response = await fetch(${JSON.stringify(path)}, { method: ${JSON.stringify(method)}, headers, body });
            const text = await response.text();
            if (!response.ok) throw new Error(${JSON.stringify(method + " " + path)} + " → " + response.status + " " + text.slice(0, 300));
            return text ? JSON.parse(text) : null;
        })()`);
    }
}

/**
 * Connects to the browser's DevTools WebSocket.
 * @param url The WebSocket address.
 * @returns The connection with a promise-based send.
 */
async function connect(url) {
    const ws = new WebSocket(url);
    await new Promise((resolve, reject) => {
        ws.onopen = resolve;
        ws.onerror = reject;
    });
    let id = 0;
    const pending = new Map();
    ws.onmessage = (event) => {
        const message = JSON.parse(event.data);
        if (message.id && pending.has(message.id)) {
            const { resolve, reject } = pending.get(message.id);
            pending.delete(message.id);
            if (message.error) {
                reject(new Error(message.error.message));
            } else {
                resolve(message.result);
            }
        }
    };

    return {
        send(method, params, sessionId) {
            const messageId = ++id;
            ws.send(JSON.stringify({ id: messageId, method, params, ...(sessionId ? { sessionId } : {}) }));
            return new Promise((resolve, reject) => pending.set(messageId, { resolve, reject }));
        },
        close() {
            ws.close();
        },
    };
}

/**
 * Signs the page in with the account of the script.
 * @param page The page.
 * @returns A promise resolved on the home page.
 */
async function signIn(page) {
    await page.open("/account/login", "КОД");
    await page.evaluate(`localStorage.setItem("odc-theme", "light"); localStorage.setItem("odc-sidebar", "expanded")`);
    await page.fill('input[type="email"]', email);
    await page.fill('input[type="password"]', password);
    await page.evaluate(`document.querySelector('form button[type="submit"]').click()`);
    await page.waitFor(`location.pathname.includes("login-2fa")`);
    // A code used in the last half minute is refused (replay protection): wait for a fresh one when needed.
    if (30 - (Math.floor(Date.now() / 1000) % 30) < 5) {
        await sleep(6000);
    }

    await page.fill("input[autocomplete='one-time-code'], input[inputmode='numeric']", totp());
    await page.waitFor(`location.pathname === "/"`, 30_000);
}

/**
 * Makes sure the demo data the screenshots show exists (a document waiting for the account's signature, a chat).
 * @param page The signed-in page.
 * @returns The identifiers the screenshots open.
 */
async function prepare(page) {
    const me = (await page.api("GET", "/api/v1/auth/context")).user;
    const documents = await page.api("GET", "/api/v1/documents?scope=AwaitingMe&page=1&pageSize=5");
    let documentId = documents.items[0]?.id;
    if (!documentId) {
        const units = await page.api("GET", "/api/v1/documents/author-units");
        const colleague = (await page.api("GET", "/api/v1/documents/signers?term=" + encodeURIComponent("Бон")))[0];
        const pdf =
            "%PDF-1.4\\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj 2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj 3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 595 842]>>endobj\\ntrailer<</Root 1 0 R>>\\n%%EOF";
        const created = await page.api(
            "POST",
            "/api/v1/documents",
            `form:(() => { const f = new FormData();
                f.append("Title", "Наказ про затвердження графіка відпусток на 2027 рік");
                f.append("Description", "Прошу погодити графік відпусток працівників на наступний рік.");
                f.append("Kind", "Order");
                f.append("OrganizationUnitId", ${JSON.stringify(units[0].id)});
                f.append("SigningMode", "Parallel");
                f.append("SignerIds", ${JSON.stringify(me.id)});
                ${colleague ? `f.append("SignerIds", ${JSON.stringify(colleague.id)});` : ""}
                f.append("Submit", "true");
                f.append("File", new File(["${pdf}"], "nakaz-vidpustky-2027.pdf", { type: "application/pdf" }));
                return f; })()`,
        );
        documentId = created.id;
    }

    const people = await page.api("GET", "/api/v1/chats/people?term=" + encodeURIComponent("Кова"));
    const chat = await page.api("POST", "/api/v1/chats/direct", { userId: people[0].id });
    const thread = await page.api("GET", `/api/v1/chats/${chat.id}/messages?limit=5`);
    if (thread.messages.length < 3) {
        for (const body of [
            "<p>Доброго дня! Надіслав Вам наказ про відпустки на підпис.</p>",
            "<p>Перевірте, будь ласка, дати в додатку — там є зміни для відділу кадрів.</p>",
            "<p><strong>Дякую!</strong> Якщо будуть питання — пишіть сюди.</p>",
        ]) {
            await page.api(
                "POST",
                `/api/v1/chats/${chat.id}/messages`,
                `form:(() => { const f = new FormData(); f.append("ClientId", crypto.randomUUID()); f.append("Body", ${JSON.stringify(body)}); return f; })()`,
            );
        }
    }

    const tickets = await page.api("GET", "/api/v1/tickets?page=1&pageSize=5");
    let ticketId = tickets.items[0]?.id;
    if (!ticketId) {
        ticketId = (
            await page.api(
                "POST",
                "/api/v1/tickets",
                `form:(() => { const f = new FormData(); f.append("Category", "Question"); f.append("Priority", "Normal"); f.append("Subject", "Як додати заступника до маршруту підписання?"); f.append("Description", "Потрібно, щоб документи міг підписувати заступник під час моєї відпустки."); return f; })()`,
            )
        ).id;
    }

    const articles = await page.api("GET", "/api/v1/news?page=1&pageSize=5");
    const units = await page.api("GET", "/api/v1/organization-units/list?page=1&pageSize=5");
    const users = await page.api("GET", "/api/v1/users?page=1&pageSize=5");
    const roles = await page.api("GET", "/api/v1/roles?Page=1&PageSize=20");
    return {
        documentId,
        chatId: chat.id,
        ticketId,
        articleSlug: articles.items[0]?.slug,
        unitId: (units.items ?? units)[0]?.id,
        userId: users.items.find((user) => user.id !== me.id)?.id ?? me.id,
        roleId: (roles.items ?? roles)[0]?.id,
    };
}

/**
 * The screenshots: a name, the page, the text it shows when ready, an optional element to crop to and an optional
 * step before the capture. Texts are given per language.
 * @param ids The identifiers of the demo data.
 * @returns The list.
 */
function shots(ids) {
    return [
        { name: "home", path: "/", ready: { uk: "Очікують мого підпису", en: "Awaiting my signature" } },
        { name: "sidebar", path: "/", ready: { uk: "Головна", en: "Home" }, crop: "#app-sidebar" },
        { name: "documents-list", path: "/documents", ready: { uk: "Документи", en: "Documents" } },
        { name: "documents-new", path: "/documents/new", ready: { uk: "Новий документ", en: "New document" } },
        { name: "documents-card", path: `/documents/${ids.documentId}`, ready: { uk: "Маршрут", en: "route" } },
        {
            name: "documents-sign",
            path: `/documents/${ids.documentId}`,
            ready: { uk: "Підписати", en: "Sign" },
            before: async (page, language) => {
                await page.click(language === "uk" ? "Підписати" : "Sign");
                await page.waitFor(`document.querySelector("dialog[open]")`);
                await page.settle();
            },
        },
        {
            name: "documents-reject",
            path: `/documents/${ids.documentId}`,
            ready: { uk: "Відмовити", en: "Decline" },
            before: async (page, language) => {
                await page.click(language === "uk" ? "Відмовити" : "Decline");
                await page.waitFor(`document.querySelector("dialog[open]")`);
                await page.settle();
            },
        },
        { name: "signing-files", path: "/signing", ready: { uk: "Підписання", en: "Sign" } },
        { name: "signing-verify", path: "/signing/verify", ready: { uk: "Перевір", en: "Verif" } },
        { name: "news-list", path: "/news", ready: { uk: "Новини", en: "News" } },
        { name: "news-article", path: `/news/${ids.articleSlug}`, ready: { uk: "Коментарі", en: "Comments" } },
        { name: "news-manage", path: "/news/manage", ready: { uk: "Новини", en: "News" } },
        { name: "news-editor", path: "/news/manage/new", ready: { uk: "Заголовок", en: "Title" } },
        { name: "chats", path: `/chats/${ids.chatId}`, ready: { uk: "Дякую", en: "Дякую" } },
        { name: "requests-new", path: "/requests/new", ready: { uk: "Нове звернення", en: "New request" } },
        { name: "requests-list", path: "/requests", ready: { uk: "Звернення", en: "Requests" } },
        { name: "requests-card", path: `/requests/${ids.ticketId}`, ready: { uk: "Опис звернення", en: "Description" } },
        { name: "requests-admin", path: "/admin/requests", ready: { uk: "Звернення", en: "Requests" } },
        {
            name: "notifications-panel",
            path: "/",
            ready: { uk: "Головна", en: "Home" },
            before: async (page, language) => {
                await page.evaluate(
                    `document.querySelector('button[aria-label^="${language === "uk" ? "Сповіщення" : "Notifications"}"]')?.click()`,
                );
                await page.settle();
            },
        },
        { name: "notifications-page", path: "/notifications", ready: { uk: "Сповіщення", en: "Notifications" } },
        { name: "structure", path: "/structure", ready: { uk: "Структура", en: "Structure" } },
        { name: "structure-unit", path: `/structure/${ids.unitId}`, ready: { uk: "Працівники", en: "Staff" } },
        { name: "profile", path: "/profile", ready: { uk: "Мій профіль", en: "My profile" } },
        { name: "profile-security", path: "/profile/security", ready: { uk: "Безпека", en: "Security" } },
        { name: "admin", path: "/admin", ready: { uk: "Адміністрування", en: "Administration" } },
        { name: "admin-users", path: "/admin/users", ready: { uk: "Користувачі", en: "Users" } },
        { name: "admin-user-new", path: "/admin/users/new", ready: { uk: "Новий користувач", en: "New user" } },
        { name: "admin-user-edit", path: `/admin/users/${ids.userId}`, ready: { uk: "Посади", en: "Positions" } },
        { name: "admin-import", path: "/admin/users/import", ready: { uk: "Імпорт", en: "Import" } },
        { name: "admin-roles", path: "/admin/roles", ready: { uk: "Ролі", en: "Roles" } },
        { name: "admin-role-edit", path: `/admin/roles/${ids.roleId}`, ready: { uk: "Дозволи", en: "Permissions" } },
        { name: "admin-units", path: "/admin/units", ready: { uk: "Підрозділи", en: "Units" } },
        { name: "admin-unit-types", path: "/admin/unit-types", ready: { uk: "Типи підрозділів", en: "Unit types" } },
        { name: "admin-audit", path: "/admin/audit", ready: { uk: "Журнал аудиту", en: "Audit log" } },
        { name: "admin-organizations", path: "/admin/organizations", ready: { uk: "odc_", en: "odc_" } },
        { name: "organization-bar", path: "/", ready: { uk: "Усі організації", en: "All organizations" }, crop: "#main > div > section" },
    ];
}

/**
 * The pages of a visitor: the sign-in and the activation.
 */
const visitorShots = [
    { name: "login", path: "/account/login", ready: { uk: "Вхід до кабінету", en: "Sign in" } },
    { name: "activation", path: "/account/activate", ready: { uk: "Активація", en: "activation" } },
];

/**
 * Takes the screenshots of one language.
 * @param page The page.
 * @param language "uk" or "en".
 * @param list The screenshots.
 * @returns The names that failed.
 */
async function takeAll(page, language, list) {
    const failed = [];
    for (const shot of list) {
        if (only.size > 0 && !only.has(shot.name)) {
            continue;
        }

        try {
            await page.open(shot.path, shot.ready[language]);
            await shot.before?.(page, language);
            await page.capture(join(output, language, `${shot.name}.webp`), shot.crop ?? null);
            console.log(`  ${language}/${shot.name}`);
        } catch (error) {
            failed.push(`${language}/${shot.name}: ${error.message}`);
            await page.evaluate(`document.querySelector("dialog[open]")?.close()`).catch(() => undefined);
        }
    }

    return failed;
}

/**
 * Takes every screenshot.
 * @returns A promise resolved when done.
 */
async function main() {
    const profile = mkdtempSync(join(tmpdir(), "odc-shots-"));
    const port = 9333;
    const browser = spawn(
        browserPath(),
        [
            "--headless=new",
            `--remote-debugging-port=${port}`,
            `--user-data-dir=${profile}`,
            "--ignore-certificate-errors",
            "--no-first-run",
            "--no-default-browser-check",
            "--hide-scrollbars",
            "--force-device-scale-factor=1",
            `--window-size=${width},${height}`,
            "about:blank",
        ],
        { stdio: "ignore" },
    );

    const failed = [];
    let socket;
    try {
        let version;
        for (let attempt = 0; attempt < 50 && !version; attempt++) {
            await sleep(200);
            version = await fetch(`http://127.0.0.1:${port}/json/version`)
                .then((r) => r.json())
                .catch(() => undefined);
        }

        socket = await connect(version.webSocketDebuggerUrl);
        const { targetId } = await socket.send("Target.createTarget", { url: "about:blank" });
        const { sessionId } = await socket.send("Target.attachToTarget", { targetId, flatten: true });
        const page = new Page(socket, sessionId);
        await page.send("Page.enable");
        await page.send("Runtime.enable");
        await page.send("Emulation.setDeviceMetricsOverride", { width, height, deviceScaleFactor: 1, mobile: false });
        await page.send("Emulation.setEmulatedMedia", {
            features: [
                { name: "prefers-color-scheme", value: "light" },
                { name: "prefers-reduced-motion", value: "reduce" },
            ],
        });

        // A visitor first, then the signed-in pages, in both languages (a visitor's language is a cookie).
        await page.open("/account/login", "КОД");
        await page.evaluate(`localStorage.setItem("odc-theme", "light")`);
        for (const language of ["uk", "en"]) {
            await page.api("PUT", "/api/v1/auth/language", { language });
            failed.push(...(await takeAll(page, language, visitorShots)));
        }

        await page.api("PUT", "/api/v1/auth/language", { language: "uk" });

        await signIn(page);
        const ids = await prepare(page);
        for (const language of ["uk", "en"]) {
            await page.api("PUT", "/api/v1/auth/language", { language });
            failed.push(...(await takeAll(page, language, shots(ids))));
        }

        await page.api("PUT", "/api/v1/auth/language", { language: "uk" });
    } finally {
        socket?.close();
        browser.kill();
        await sleep(500);
        rmSync(profile, { recursive: true, force: true });
    }

    if (failed.length > 0) {
        console.error(`Failed:\n${failed.join("\n")}`);
        process.exitCode = 1;
    }
}

await main();
