# CLAUDE.md — KOD (Кабінет організаційних документів)

Electronic document management for any organization: documents with signing routes and a **qualified electronic
signature (QES) made in the browser**, several organizations with arbitrary unit trees, roles and permissions per
position, news, profile, admin panel, user manual. The UI is Ukrainian with a full English translation.
`README.md` (Ukrainian) is the user/operator documentation; this file is the working guide for changing the code.

## Commands

```bash
# Build everything with tests. Use your own configuration name (Lead, Release, …): the user often runs the app
# from bin/Debug, which locks the Debug output. Warnings are errors (StyleCop + .NET analyzers).
dotnet build tests/KOD.UnitTests/KOD.UnitTests.csproj -c Lead -v q -nologo 2>&1 | grep -E "error|warn" | sort -u

# Tests (xUnit v3 on Microsoft Testing Platform; ~84 tests, a few seconds)
dotnet test tests/KOD.UnitTests -c Lead --no-build

# Run (migrations and seeding run at startup). The user's own instance is usually on https://localhost:7180;
# for your own verification use the launch config "kod-web-verify" (port 7181, -c Lead) in .claude/launch.json.
dotnet run --project src/KOD.Web --launch-profile https

# Migrations (local tool). Build with a non-Debug configuration if the app is running.
dotnet tool restore
dotnet ef migrations add <Name> --project src/KOD.Infrastructure --startup-project src/KOD.Web --configuration Release --output-dir Persistence/Migrations
```

Secrets live in user-secrets of `src/KOD.Web` (`ConnectionStrings:Database`, `Seed:AdminPassword`, `Seed:DemoPassword`).
Never print or commit them. The seeded admin password is used only when the admin is created; the first sign-in
replaces it and enrolls 2FA (TOTP), so it does not keep working afterwards.

Tooling on this machine: Windows, Git Bash + PowerShell, Node.js available (use it for scripts), `python` is only the
Store stub. Bash heredocs with Ukrainian text and quotes sometimes break — write scripts to the scratchpad with the
Write tool and run them with `node`. `/tmp` in Git Bash is not visible to Windows Node: pass `$(cygpath -w /tmp/x.js)`.

## Architecture (Clean Architecture, .NET 10 / C# 14)

```
src/KOD.Domain          Entities + invariants (User, Role, StaffPosition, OrganizationUnit, UnitType, Document with
                        signers and signatures, NewsArticle with NewsComment/NewsReaction/NewsView, MediaFile, AuditEntry,
                        UserSession), permission catalog, system roles, Guard, DomainException, localization catalog
                        (Localization/Texts.cs, Languages.cs).
src/KOD.Application     Use-case services (DocumentService, UserService, OrganizationService, UnitTypeService, RoleService,
                        NewsService, NewsDiscussionService, MediaService + MediaLibrary, ProfileService, DashboardService,
                        AuditService, SigningJournal), DTOs, Result<T>/Error, paging, access rules (IPermissionResolver,
                        IAccessScope, AdministrationScope), abstractions.
src/KOD.Infrastructure  EF Core + PostgreSQL (snake_case, citext, xmin concurrency), migrations, seeding, Identity
                        (SignInManager with mandatory 2FA, claims factory, server-side session store), file storage,
                        images (libvips/NetVips → AVIF, media storage, cleanup job), HTML sanitizer, XLSX/CSV, Markdig,
                        CMS signature inspector, audit logger.
src/KOD.Web             Host: Blazor Web App pages (Components/), REST API (Api/, /api/v1), security middleware,
                        signing proxy and trust list (Signing/), localization provider (Localization/), logging (Hosting/).
  DesignSystem/         Razor components (Components/) + DesignTokens; CSS/JS/icons/fonts live in wwwroot.
tests/KOD.UnitTests     Domain rules, access, scoped administration (EF InMemory), localization coverage, trust list.
```

Dependencies point inward: Web → Infrastructure → Application → Domain. Central Package Management
(`Directory.Packages.props`); check new package versions on NuGet and prefer ones published more than two weeks ago.

### Rendering model

- Pages are **static SSR + enhanced navigation**; list pages use `[StreamRendering]` with a skeleton first.
- Complex forms are `@rendermode InteractiveServer` (document editor, user/role/unit/unit-type/news editors, import).
  Interactive circuits keep the culture of the request that started them.
- Global behaviors are plain JS with event delegation on `document` (they survive enhanced navigation):
  `wwwroot/js/kod-ds.js` (dialogs, confirm, toasts, autosubmit, theme, sidebar, button ink, password toggle, OTP cells),
  `kod-pickers.js` (custom select drop-down and date picker calendar), `app.js` (ES module: page entrance, navigation
  progress bar, lazy signing UI / manual), `kod-wizard.js`, `help.js`, `signing/*`.
- Strict CSP: no inline scripts, no `eval`. New scripts are static files referenced through `DesignTokens` / `@Assets[...]`.

### Images, news editor, discussion

- **Images** (`MediaFile`): every upload goes through `MediaLibrary` → `IImageProcessor` (`VipsImageProcessor`: loader
  allow-list, EXIF orientation, sRGB, metadata stripped, AVIF with quality/size lowered until the main rendition fits
  its budget — news 1600 px / 100 KB, avatars 320 px square) → `IMediaStorage` (`App_Data/storage/media/xx/{id}.avif` +
  `.thumb.avif`). Served by `GET /media/{id}` and `/media/{id}/thumb` to signed-in users, `immutable` cache. Use
  `MediaUrls` for URLs, `<Avatar PhotoId=…>` / `<PersonName>` to show people. `MediaLibrary.SaveChangesAsync` deletes
  files of removed images only after the database save. `MediaCleanupService` removes unattached images after a day.
- **Profile photo is mandatory**: claim `kod:photo` (claims factory) → `AccountSetupMiddleware` (temporary password
  first, then `/account/photo`; API gets 403). The photo form posts to `POST /account/photo` (plain multipart form,
  crop fractions from `js/photo-cropper.js`) and refreshes the sign-in cookie. New user DTOs carry `PhotoId`.
- **News body** is HTML from the Tiptap editor (`RichTextEditor` component + `js/rich-text.js`; the Tiptap bundle is
  `wwwroot/lib/tiptap/tiptap.js`, rebuilt from `tools/rich-text` with `npm ci && npm run build`). The server sanitizes it
  (`IRichTextSanitizer`, allow-list; images must be `/media/…`). Older articles keep `NewsBodyFormat.Markdown` until edited.
  `NewsService.SyncImagesAsync` attaches the images an article uses and removes the dropped ones. Galleries are
  `<div data-type="gallery">` of thumbnails with `data-full`; `js/lightbox.js` opens them.
- **Discussion**: `NewsDiscussionPanel` is an interactive island on the static article page; reactions (one per reader
  and target) and one-level comments via `INewsDiscussionService`; `INewsDiscussionEvents` (in-process) refreshes open
  pages — replace it with a message bus implementation when running several servers.

## Conventions

- **StyleCop + analyzers, TreatWarningsAsErrors.** XML documentation in English on every type and member, including
  private ones; Razor components have a `*.razor.cs` code-behind with docs. Member order matters (SA1201/SA1202: fields
  before properties, public before private). Avoid multi-line `a && b ? x : y` (SA1143) — use `if`.
- Modern C#: file-scoped namespaces, primary constructors, `field`, extension blocks (`extension(IServiceCollection services) { … }`),
  collection expressions, `_camelCase` private fields, no `this.`.
- JS: JSDoc on every function; IIFE scripts for global behaviors, ES modules for lazily loaded parts.
- Match the surrounding code: comment density, naming, idioms. Code and docs in English, UI texts in Ukrainian via `T()`.
- Commits: imperative subject, body with bullets, end with the Co-Authored-By line. Branch `KOD/full-refactoring`,
  main branch `develop`.

## Localization (Ukrainian source, English translation)

- **The Ukrainian text is the key.** Razor: `@T("Зберегти")`, `Title="@T("…")"` (both `_Imports.razor` have
  `@using static KOD.Domain.Localization.Texts`). C#: `Texts.T("…")`. JS: `kodDs.t("…")`.
- With values: `T("Знайдено {0} документів", count)` — never interpolate or concatenate inside `T()`; the key must be a
  plain literal so the coverage test can find it.
- Same Ukrainian word, different English: `Texts.In("context", "…")` looks up `"context|…"` first
  (e.g. `In("document-kind", "Довідка")` = Certificate, plain `T("Довідка")` = Help; `audit|…` for audit event names,
  `help|…` for manual titles that collide with UI buttons).
- Translations: `src/<Project>/Localization/en/*.json` (flat `{ "uk": "en" }`, embedded automatically by
  `Directory.Build.props`), client side `wwwroot/js/i18n/en*.js` (loaded only on English pages, see `DesignTokens.TranslationScripts`).
- Data-annotation `ErrorMessage` stays a Ukrainian constant; show it with `<ValidationError For="…"/>` (never `ValidationMessage`).
- `Guard.Text(value, max, "Назва")` translates the field name itself — add the name to a catalog.
- `PermissionDefinition.Title/Description` and `SystemRoleDefinition.Name/Description` are translated properties; the
  Ukrainian source is `Source*` (used for seeding). Seeded role and unit-type names are translated in display-only DTOs
  via `StoredText.Display` (never in edit models — a translated name must not be saved back).
- Database data (audit details, user-entered names, seed data) is not translated.
- The manual: `Components/Pages/Help/Chapters/<X>Chapter.razor` (Ukrainian) and `Chapters/English/<X>Chapter.razor`
  (English twin with the same Ids). Update both when a feature changes. Structure and titles: `HelpContents.cs`.
- Culture: `LanguageCultureProvider` (claim `kod:lang` from the profile, then cookie `__Host-kod.lang`, else uk-UA);
  `UseRequestLocalization` runs **after** authentication. Language switch: `POST /account/language` (plain, not enhanced form).
- `LocalizationTests` fails for any `T(…)`, `In(…)`, `ErrorMessage`, Guard field name or `kodDs.t(…)` literal without
  an English translation. Tests run in uk-UA (`TestCulture` module initializer).

## Security invariants (do not break)

- The private key and its password never leave the browser: they go only to the Web Worker
  (`wwwroot/js/signing/eusign.worker.js`, IIT EUSignCP-JS). The server receives the finished signature + certificate data,
  public OCSP/TSP/CMP messages through `/api/v1/eusign/proxy` (host allowlist; only hosts are logged), and for the file
  signing tool only file names and parameters (`POST /api/v1/signing/journal` → audit `signing.files-signed`).
- Before signing a document the browser checks the file's SHA-256 against the registered one.
- **Signature chain** (`SignatureChain`, `Document.AddSignature`): the first signature fixes format + packaging; in the
  cumulative formats (CAdES, PAdES, ASiC) every next signature is added to the newest signed version
  (`BaseSignatureId` must equal `LatestSignature`), so the newest signature file holds the document with all signatures
  ("Document with all signatures" on the card). XAdES has no append in the IIT library: independent signatures, base null.
  The worker appends with `CtxAppendSign` / `CtxPDFSignData` on the signed PDF / `CtxASiCAppendSign` after checking the
  previous version (signature count, same document). The server accepts only "previous version + exactly one signature"
  (`ISignatureInspector.TryCountSignatures`, PDF byte prefix) and takes the new CMS signer's certificate. Removing a signer
  from the route drops the whole cumulative chain.
- **Signing turn** (`Document.BeginSigning`, `SignatureChain.TurnDuration` = 10 min): the browser takes it when the signing
  window opens (`POST /api/v1/documents/{id}/signing-session`, 409 code `signing_turn` while someone else holds it — the
  window polls every 5 s), renews it right before signing, and gives it up on close (`DELETE`). `Document` also has an
  `xmin` concurrency token; `AddSignatureAsync` reloads on `DbUpdateConcurrencyException`, and a stale base is rejected
  ("sign the latest version again").
- 2FA is mandatory; TOTP codes are single-use; secrets are encrypted with Data Protection; sessions are server-side
  (`DatabaseTicketStore`) with idle + absolute lifetime; permissions are re-read every minute (security stamp validator,
  claims factory). Changing the sign-in cookie: `SignInManager.RefreshSignInAsync(user)` (used by password and language changes).
- Private files (documents, signatures) live outside `wwwroot` (`App_Data/storage`) and are served only through the API
  after an access check.
- Administration is scoped to the organizations of the administrator's positions (`AdministrationScope`); `roles.manage`
  and `audit.view` are cabinet-wide.

## Logging

Serilog (`Hosting/LoggingExtensions.cs`, `LoggingOptions`): console, daily rolling file `App_Data/logs/kod-YYYYMMDD.log`
(31 files), PostgreSQL table `logs` (auto-created, 90-day retention). One line per request (`UseRequestLogging`): pages and
API at Information, static files / Blazor at Debug, >3 s Warning, failures Error. Levels in `Serilog:MinimumLevel`.
Use `[LoggerMessage]` partial methods for new log statements.

## Design system notes

- Monochrome Diia-like style, Manrope, Lucide sprite (`wwwroot/icons/sprite.svg`, add symbols there; use `<Icon Name="…"/>`).
- Buttons and button-like controls have the "ink" hover: a fill of the theme's contrast colour grows from the pointer
  (`--_x/--_y` set in `kod-ds.js`; selector list `INK`). Inverse surfaces (`.ds-card--inverse`, `.ds-alert--inverse`,
  `.home-hero`) flip the fill colours. Inputs show focus with the border only.
- Components worth knowing: `DataGrid` (+ `GridColumn`, paging/sorting by query string), `GridToolbar` (GET form,
  `data-enhance`), `DatePicker` (hidden ISO input + text field), custom `select.ds-select` (no markup change needed),
  `OtpInput`, `PasswordReveal`, `ValidationError`, `GridSkeleton` / `CardSkeleton` / `FormSkeleton`, `Dialog`, `Tabs`,
  `PageHeader` (renders `<PageTitle>`).
- Scrollbars of dialogs, listboxes and `.help-toc` are styled; in Chromium do not set `scrollbar-width`
  on them (it disables the `::-webkit-scrollbar` styles).

## Gotchas learned the hard way

- **EF Core:** filter before projecting into a record (`Where` on a projected record's property cannot be translated).
  Calling .NET methods (e.g. `Texts.T`, `StoredText.Display`) is fine only in the final projection. The InMemory test
  provider does not catch untranslatable queries — check new queries against PostgreSQL in the browser.
- **Forms:** a form field named `action` (or `method`, `submit`…) shadows `form.action`; Blazor enhanced form navigation
  then posts to `/[object HTMLSelectElement]`. Use other names (`category`).
- **Streaming + `<PageTitle>`:** a `PageTitle` that first appears in a later streamed chunk does not reach `<head>`, and
  changing it during streaming can crash the renderer ("The renderer does not have a component with ID…"). Pages whose
  header depends on loaded data are not streamed; streamed pages render `PageHeader` in the first chunk.
- **`enhancedload` also fires for every streaming update** (skeleton → data), even on a full page load. One-time
  effects of a navigation (the page entrance in `app.js`) run only after `enhancednavigationstart`.
- **Interactive pages are prerendered, then rendered again by the circuit.** Data loaded in `OnInitialized/OnParametersSet`
  goes into public `[PersistentState]` properties and is loaded only when missing, or the form blinks back to the
  skeleton (and a "New …" heading) when the circuit starts. Keep this for new interactive editors.
- **Font preloads use the plain `fonts/…` paths** that `kod-ds.css` references, not `@Assets[…]` (a fingerprinted URL
  is a different resource: the font downloads twice and the first paint uses the fallback font).
- **`replaceChildren(…, null)`** prints "null"; filter optional children first.
- `<html>` attributes are re-synced by enhanced navigation: theme/sidebar/navigation state are restored on `enhancedload`.
- Help page performance: topics use `content-visibility: auto`; `help.js` renders them once to remember real heights so
  anchors land exactly. Keep that when changing the manual's layout.
- PAdES has no signing-time attribute; verification shows the signature time-stamp, else the PDF `/M` time.
- Cumulative signing could only be checked structurally here: producing real signatures needs a qualified key, so test
  append flows with the user (CAdES detached/enveloped, PAdES, ASiC-S/E, sequential and parallel routes).
  A PDF without `/ByteRange` has no signatures (the library would report "damaged data").
- The audit log category filter uses the query parameter `category`, not `action`.

## Verifying changes

1. Build + tests with your configuration.
2. For UI changes start `kod-web-verify` (7181) via the preview tools; the browser session cookie is shared with 7180.
   If the pane is signed out, ask the user to sign in in the Browser pane (2FA) — do not use their credentials.
3. Check console/server errors (`preview_logs`), use `javascript_tool` for computed state and `find`/refs for clicks.
4. Report what was and was not verified; restore emulated viewports; stop your verification server.
