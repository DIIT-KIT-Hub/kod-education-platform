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

```bash
# React client (src/KOD.Client; Node 24+, pnpm). KOD.Web serves its dist/ at /app, so build it before checking it.
cd src/KOD.Client
pnpm install
pnpm build                  # tsc -b + vite build (production; fails on a missing English translation)
pnpm dev                    # vite build --watch in development mode (sourcemaps); reload the page after a rebuild
pnpm typecheck && pnpm lint && pnpm test
pnpm format                 # Prettier (indent from .editorconfig: 4 spaces)
pnpm i18n:extract           # update src/locales/{uk,en}/messages.po after changing texts
pnpm api:types              # regenerate src/shared/api/schema.d.ts from a running server's /openapi/v1.json
                            # (the script points at 7180; for your own server run the same command with 7181)
```

Publishing KOD.Web runs `pnpm install --frozen-lockfile && pnpm build` and copies `dist` (without source maps) into
`wwwroot/app`; `dotnet build ... -p:SkipClientBuild=true` skips it.

Secrets live in user-secrets of `src/KOD.Web` (`ConnectionStrings:Database`, `Seed:AdminPassword`, `Seed:DemoPassword`).
Never print or commit them. The seeded admin password is used only when the admin is created; the first sign-in
replaces it and enrolls 2FA (TOTP), so it does not keep working afterwards.

Tooling on this machine: Windows, Git Bash + PowerShell, Node.js available (use it for scripts), `python` is only the
Store stub. Bash heredocs with Ukrainian text and quotes sometimes break — write scripts to the scratchpad with the
Write tool and run them with `node`. `/tmp` in Git Bash is not visible to Windows Node: pass `$(cygpath -w /tmp/x.js)`.

## Architecture (Clean Architecture, .NET 10 / C# 14)

```
src/KOD.Domain          Entities + invariants (User, Role, StaffPosition, OrganizationUnit, UnitType, Document with
                        signers and signatures, NewsArticle with NewsComment/NewsReaction/NewsView, MediaFile, AuditEntry),
                        permission catalog, system roles, Guard, DomainException, localization catalog
                        (Localization/Texts.cs, Languages.cs).
src/KOD.Application     Use-case services (DocumentService, UserService, OrganizationService, UnitTypeService, RoleService,
                        NewsService, NewsDiscussionService, MediaService + MediaLibrary, ProfileService, DashboardService,
                        AuditService, SigningJournal), DTOs, Result<T>/Error, paging, access rules (IPermissionResolver,
                        IAccessScope, AdministrationScope), abstractions.
src/KOD.Infrastructure  EF Core + PostgreSQL (snake_case, citext, xmin concurrency), migrations, seeding, Identity
                        (full Identity schema, SignInManager with mandatory 2FA, claims factory, ES256 key + JWT service
                        in Identity/Tokens), file storage,
                        images (libvips/NetVips → AVIF, media storage, cleanup job), HTML sanitizer, XLSX/CSV, Markdig,
                        CMS signature inspector, audit logger.
src/KOD.Web             Host: Blazor Web App pages (Components/), REST API (Api/, /api/v1), JWT authentication handlers
                        (Identity/), security middleware, signing proxy and trust list (Signing/), localization provider
                        (Localization/), logging (Hosting/).
  DesignSystem/         Razor components (Components/) + DesignTokens; CSS/JS/icons/fonts live in wwwroot.
src/KOD.Client          React SPA at /app that replaces the Blazor pages module by module (see "React client").
tests/KOD.UnitTests     Domain rules, access, scoped administration (EF InMemory), JWT tokens, localization coverage,
                        trust list.
```

### Identity tables and authentication

- `ApplicationDbContext` is a full `IdentityDbContext<User, Role, Guid, UserPermissionOverride, StaffPosition,
  IdentityUserLogin<Guid>, RolePermission, IdentityUserToken<Guid>>`; tables `users`, `roles`, `user_roles`, `role_claims`,
  `user_claims`, `user_logins`, `user_tokens` (snake_case names of the AspNet* tables). Domain types are the Identity types:
  `Role : IdentityRole` (`Name` = unique display name, `NormalizedName` via `Role.NormalizeName`; `Code` stays the stable
  key), `RolePermission : IdentityRoleClaim` and `UserPermissionOverride : IdentityUserClaim` (claim types in
  `PermissionClaimTypes`: `kod:permission` granted, `kod:permission.denied`), `StaffPosition : IdentityUserRole` with its
  own `Id` key and the unit (a user may hold one role in several units). Query permissions through `ClaimType`/`ClaimValue`
  in LINQ. Positions change only through `User.SetPositions` (`UserManager.AddToRoleAsync` cannot choose a unit).
- **Stateless JWT access tokens + rotating refresh tokens (RFC 9700 §4.14.2).** `JwtTokenService` issues ES256 JWTs (key:
  `JwtSigningKey`, PEM from `Authentication:Jwt:SigningKeyPath` or generated into `App_Data/keys` encrypted with Data
  Protection): access `at+jwt` (5 min; `sub`, `sid`, `auth_time`, `amr`, all user claims incl. permissions) and `2fa+jwt`
  (between password and code). Refresh tokens are opaque (`RefreshToken`: family id ‖ generation ‖ HMAC with the family
  key) and live in `refresh_token_families` (`RefreshTokenStore`, the only server-side auth state): every use rotates to
  the next generation, any older authentic generation revokes the family (except the previous one within
  `RefreshTokenReuseInterval` = 30 s, for parallel requests: it gets the current token, no rotation). Families end on
  sign-out, idle 1 h / absolute 12 h (NIST SP 800-63B-4 AAL2), or a security stamp mismatch.
  `JwtAuthenticationHandler` (scheme `Jwt`, `AppSignInManager.AuthenticationScheme`) reads `__Host-kod.at`, falls back to
  `__Host-kod.rt` (rotates, re-reads the user: active + stamp, rebuilds claims) only when the response can still set
  cookies, and challenges with a redirect (pages) or 401 (`/api`). `TwoFactorJwtHandler` is registered under
  `IdentityConstants.TwoFactorUserIdScheme`. Claim types are JWT names (`AppClaimTypes`); the security stamp never goes
  into a token.
- Revocation: sign-out deletes the family; a new security stamp (`IIdentityService.InvalidateSessionsAsync`: password
  change/reset, block, 2FA reset, "end all sessions", profile "sign out on other devices") stops every family bound to the
  old stamp, while `RefreshSignInAsync` rebinds the current browser's family. Issued access tokens live until expiry
  (≤ 5 min); circuits re-check the family every minute (`IdentityRevalidatingAuthenticationStateProvider`).

Dependencies point inward: Web → Infrastructure → Application → Domain. Central Package Management
(`Directory.Packages.props`); check new package versions on NuGet and prefer ones published more than two weeks ago.

### Rendering model

- Pages are **static SSR + enhanced navigation**; list pages use `[StreamRendering]` with a skeleton first.
- Complex forms are `@rendermode InteractiveServer` (document editor, user/role/unit/unit-type/news editors, import).
  Interactive circuits keep the culture of the request that started them.
- Global behaviors are plain JS with event delegation on `document` (they survive enhanced navigation):
  `wwwroot/js/kod-ds.js` (dialogs, confirm, toasts, autosubmit, theme, sidebar, button ink, password toggle, OTP cells),
  `kod-pickers.js` (custom select drop-down and date picker calendar), `app.js` (ES module: page entrance, navigation
  progress bar, lazy manual), `help.js`.
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
  crop fractions from `js/photo-cropper.js`) and re-issues the JWT cookies (`RefreshSignInAsync`). New user DTOs carry `PhotoId`.
- **News body** is HTML from the Tiptap editor (`features/news/components/RichTextEditor.tsx`, `@tiptap/react`; images
  are uploaded at once through `POST /api/v1/media`). The server sanitizes it (`IRichTextSanitizer`, allow-list; images
  must be `/media/…`) and the client inserts only that HTML (`ArticleBody`). Older articles keep
  `NewsBodyFormat.Markdown` until edited. `NewsService.SyncImagesAsync` attaches the images an article uses and removes
  the dropped ones. Galleries are `<div data-type="gallery">` of thumbnails with `data-full`; `ArticleBody` opens them
  in a full-screen viewer.
- **Discussion** (Reddit-like): a comment answers the article or any comment; `NewsComment` keeps `ParentId`, `RootId`
  (the top-level comment of its thread) and `Depth` (max 50), so `GET /api/v1/news/{id}/discussion?page&pageSize&sort`
  loads one page of threads with their whole branches in two queries and builds the tree in memory (`ReplyTo` names the
  author answered, `ReplyCount` counts the branch). A deleted comment stays a placeholder while replies remain and goes
  away with the last one; nobody can answer it. The client draws branches with fold lines, shows 5 replies then "more",
  opens branches deeper than 6 on their own, pages threads (`?comments=`) and sorts them (Best / Newest / Oldest).
- **Real time: SignalR** (`Realtime/LiveHub` at `/api/v1/live`, `[Authorize]`, own-origin connections only). One
  connection per client (`shared/realtime/live.ts`, `useLiveGroup`); a page joins groups (`news:{id}` now, chats later)
  and gets signals only — data still comes from the REST API, and every change goes through REST (antiforgery, audit).
  `INewsDiscussionEvents.Publish` → `LiveNewsDiscussionEvents` → the article group. With several servers add a SignalR
  backplane (Redis). Groups are joined again after a reconnect; sign-out and 401 stop the connection.

## React client (`src/KOD.Client`, migration from Blazor)

The frontend moves from Blazor to a React SPA; at the end the MVC/Blazor part disappears and KOD.Web is a Web API plus
the static SPA. Phases: 1 sign-in + shell + home (done), 2 documents and signing (done), 3 news (done),
4 structure and profile (done), 5 administration, 6 manual, 7 remove Blazor and move the SPA from `/app` to `/`. Until a
module is moved, the menu links to its Blazor page with a plain `href` (full page load); moved pages use `to`. A moved
section loses its Blazor pages and its old addresses redirect to `/app/...` (`MovedSections` in `SpaHostingExtensions`),
so links in the remaining Blazor pages and the manual keep working.

- **Stack** (latest versions; the user allowed the newest npm releases, the two-week rule is for NuGet): React 19 +
  React Compiler (`@rolldown/plugin-babel` + `reactCompilerPreset`), Vite 8, TypeScript 6 (strict,
  `noUncheckedIndexedAccess`; `exactOptionalPropertyTypes` is off — it fights CSS-module classes and React props),
  React Router (data mode, `createBrowserRouter`, lazy routes, basename `/app`), TanStack Query, openapi-fetch +
  openapi-typescript, react-hook-form + zod, Lingui, lucide-react, Vitest + happy-dom + Testing Library.
  Pinned on purpose: `typescript` 6.0.3 (typescript-eslint does not support 7 yet), `@rolldown/plugin-babel` 0.2.4
  (0.3 breaks `@vitejs/plugin-react` 6).
- **No UI libraries**: every component is written by hand, ported from the Blazor design system (`kod-ds.css`,
  `app.css`, `kod-ds.js`). Add libraries only for real infrastructure, never for trivial cases.
- **Structure** (bulletproof-react, enforced by `no-restricted-imports` in `eslint.config.js`): `src/shared` (api, i18n,
  lib, session, styles, ui) ← `src/features/<feature>` (api, components, pages; a feature never imports another
  feature) ← `src/app` (router, guards, layout, query client). Cross-feature state (the auth context, permissions)
  lives in `shared/session`.
- **CSS Modules only** (`*.module.css` next to the component, `localsConvention: camelCaseOnly`). Tokens, themes, base
  styles and fonts are `:global` in `shared/styles/global.module.css`; reusable layout/text utilities are in
  `shared/styles/layout.module.css`. Shared keyframes are global (`ds-*`) and used **only** through the `--ds-anim-*`
  custom properties: a keyframe name written in a module's `animation` is renamed to a local one. Local keyframes
  (`@keyframes settle`) are fine. Global selectors on `<html>` state: `:global([data-sidebar="collapsed"]) .x`.
  Inverse surfaces set `data-inverse`, buttons read it for the ink colours.
- **Data**: `shared/api/client.ts` (`api`, typed by `schema.d.ts`) adds `X-CSRF-TOKEN` to state-changing requests and
  reports 401s; `unwrap()` turns a failed call into `ApiError(status, code, detail)` (`detail` is already translated);
  `describeError(error, fallback)` for messages. The JWTs stay in HttpOnly cookies — scripts never see them.
- **Session**: `authContextQuery` (`GET /api/v1/auth/context`: language, antiforgery token, user with permissions) is
  loaded by the root route. The antiforgery token is bound to the identity, so **after every sign-in, sign-out,
  password, photo or language change call `reloadAuthContext(queryClient)`**. Guards in `app/guards.ts`
  (`requireReadyUser`: signed in, own password, photo; `requireUser`, `requirePasswordSet`, `requireAnonymous`).
  Return addresses go through `safeReturnUrl`.
- **Forms**: react-hook-form + `zodResolver`; build the zod schema inside the component with `t` so its messages are
  translated; `<Field>` gives the control its id / `aria-describedby` / `aria-invalid` through a render prop.
- **Pages** set the document title with `<DocumentTitle>` or `<PageHeader>` (React hoists `<title>`).
- Global UI services are module stores read with `useSyncExternalStore`: `toast(text)`, `await confirm({...})`.
- Unit type icons are stored as Lucide names (`UnitType.Icons` on the server); the client maps them to components in
  `shared/lib/unitTypeIcons.ts` — add a new icon in both places. Module names: `moduleNames` in `shared/i18n/display.ts`.
- After a language change, queries other than the auth context are invalidated: some data comes translated by the
  server (permission titles, role and unit type names).
- **Signing** (`shared/signing`): `SigningClient` (typed worker client; worker messages are Ukrainian and translated
  through a table in `client.ts`), `SigningWizard` (document or local files), `KeyPanel`, format rules in `formats.ts`.
  The worker (`eusign.worker.js`, plain JS) is bundled by Vite (`new Worker(new URL(...))`); the IIT library stays in
  `KOD.Web/wwwroot/lib/eusign` (16 MB, served compressed by `MapStaticAssets`; the trust list fallback lives there too).
  Keep the order: key read only on the last step, released after signing / going back / closing / leaving.

## Conventions

- **StyleCop + analyzers, TreatWarningsAsErrors.** XML documentation in English on every type and member, including
  private ones; Razor components have a `*.razor.cs` code-behind with docs. Member order matters (SA1201/SA1202: fields
  before properties, public before private). Avoid multi-line `a && b ? x : y` (SA1143) — use `if`.
- Modern C#: file-scoped namespaces, primary constructors, `field`, extension blocks (`extension(IServiceCollection services) { … }`),
  collection expressions, `_camelCase` private fields, no `this.`.
- JS: JSDoc on every function; IIFE scripts for global behaviors, ES modules for lazily loaded parts.
- TypeScript/React: JSDoc on every exported and inner function, component and props member; function components,
  named exports, `@/` imports across folders.
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
- **React client**: Lingui with Ukrainian source texts — ``t`Зберегти` `` / `<Trans>` from `@lingui/react/macro`,
  ``msg`…` `` for module-level descriptors (shown with `t(descriptor)`), `msg({ message, context })` for the same word
  with different meanings (the context matches the server's `In("context", …)`). Catalogs:
  `src/KOD.Client/src/locales/{uk,en}/messages.po`; after changing texts run `pnpm i18n:extract` and fill every English
  `msgstr` (reuse the server JSON translations). The production build fails on a missing translation. Dates in Kyiv
  time through `shared/i18n/format.ts`, enum names through `shared/i18n/display.ts`.
- `LocalizationTests` fails for any `T(…)`, `In(…)`, `ErrorMessage`, Guard field name or `kodDs.t(…)` literal without
  an English translation. Tests run in uk-UA (`TestCulture` module initializer).

## Security invariants (do not break)

- The private key and its password never leave the browser: they go only to the Web Worker
  (`src/KOD.Client/src/shared/signing/eusign.worker.js`, IIT EUSignCP-JS). The server receives the finished signature + certificate data,
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
- 2FA is mandatory; TOTP codes are single-use; secrets are encrypted with Data Protection. Authentication is JWT access
  tokens + rotating refresh tokens (see "Identity tables and authentication"): only ES256 with server-fixed
  `typ`/`iss`/`aud`/`kid`/lifetime is accepted, tokens live in `__Host-` `HttpOnly` `Secure` `SameSite` cookies (never
  readable by scripts), refresh token reuse revokes the family, the security stamp stays on the server. Do not weaken
  rotation (RFC 9700 §2.2.2 MUST for browser-held refresh tokens). Re-issuing the tokens of the current browser:
  `SignInManager.RefreshSignInAsync(user)` (password, language, photo, sign-out elsewhere) — it keeps `auth_time`, so it
  never extends the absolute lifetime.
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
- **CSS module without a used class is dropped**: importing a `.module.css` only for its side effects emits no CSS —
  use at least one of its classes (`global.root` on the root element keeps the global styles).
- **CSP and Vite**: `script-src 'self'` and `font-src 'self'` forbid `data:` URLs, so `build.assetsInlineLimit` is 0
  (Vite would inline small fonts). `img-src` allows `data:` (the authenticator QR code is an `<img>` with an SVG data URL).
- **Record contracts**: validation attributes go on the primary constructor parameters (`[Required] string Body`), never
  `[property: Required]` — MVC throws (500) for the latter. `ApiContractTests` checks every contract.
- **API JSON**: enums are serialized as names and numbers strictly as numbers, for controllers and the OpenAPI document
  alike (`ConfigureJson` in `WebServiceCollectionExtensions`); otherwise the generated types say `number | string`.
  A computed form property needs `[BindNever]` or it shows up in the OpenAPI request body.
- **Antiforgery tokens are bound to the `sub` claim and its `Issuer`.** Every identity of a signed-in user (validated
  JWT, the one issued during a refresh, a circuit's refreshed principal) must carry the token issuer (`urn:kod`), or a
  form rendered in one request fails with 400 in the next. Testing token refresh: run the app with
  `--Authentication:Jwt:AccessTokenLifetime=00:00:10 --Authentication:Jwt:ClockSkew=00:00:01` after `--`.

## Verifying changes

1. Build + tests with your configuration; for the client `pnpm typecheck && pnpm lint && pnpm test && pnpm build`.
2. For UI changes start `kod-web-verify` (7181) via the preview tools; the JWT cookies are shared with 7180 (same host, same signing key in `App_Data/keys`).
   If the pane is signed out, ask the user to sign in in the Browser pane (2FA) — do not use their credentials.
3. Check console/server errors (`preview_logs`), use `javascript_tool` for computed state and `find`/refs for clicks.
4. Report what was and was not verified; restore emulated viewports; stop your verification server.
