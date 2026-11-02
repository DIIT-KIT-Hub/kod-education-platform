# CLAUDE.md — KOD (Кабінет організаційних документів)

Electronic document management for any organization: documents with signing routes and a **qualified electronic
signature (QES) made in the browser**, several organizations with arbitrary unit trees, roles and permissions per
position, news, profile, admin panel, user manual. The UI is Ukrainian with a full English translation.
`README.md` (Ukrainian) is the user/operator documentation; this file is the working guide for changing the code.

KOD.Web is an ASP.NET Core Web API (REST under `/api/v1`, a SignalR hub, image and signing endpoints) that also serves
the React single-page application (`src/KOD.Client`) at the root of the site. There are no server-rendered pages.

## Commands

```bash
# Build everything with tests. Use your own configuration name (Lead, Release, …): the user often runs the app
# from bin/Debug, which locks the Debug output. Warnings are errors (StyleCop + .NET analyzers).
dotnet build tests/KOD.UnitTests/KOD.UnitTests.csproj -c Lead -v q -nologo -p:SkipClientBuild=true 2>&1 | grep -E "error|warn" | sort -u

# Tests (xUnit v3 on Microsoft Testing Platform; ~140 tests, a few seconds)
dotnet test tests/KOD.UnitTests -c Lead --no-build

# Run (migrations and seeding run at startup). The user's own instance is usually on https://localhost:7180;
# for your own verification use the launch config "kod-web-verify" (port 7181, -c Lead) in .claude/launch.json.
dotnet run --project src/KOD.Web --launch-profile https

# Migrations (local tool). Build with a non-Debug configuration if the app is running.
dotnet tool restore
dotnet ef migrations add <Name> --project src/KOD.Infrastructure --startup-project src/KOD.Web --configuration Release --output-dir Persistence/Migrations
```

```bash
# React client (src/KOD.Client; Node 24+, pnpm). In development KOD.Web serves its dist/ (Spa:Root). Building KOD.Web
# (F5 in Visual Studio, dotnet run/build) runs pnpm install/build itself when the lockfile or client sources are newer
# than the last result (incremental targets in KOD.Web.csproj); -p:SkipClientBuild=true skips that.
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
`ClientApp` next to the app (`Spa:Root`); `-p:SkipClientBuild=true` skips the client build.

Secrets live in user-secrets of `src/KOD.Web` (`ConnectionStrings:Database`, `Seed:AdminPassword`, `Seed:DemoPassword`).
Never print or commit them. The seeded admin password is used only when the admin is created; the first sign-in
replaces it and enrolls 2FA (TOTP), so it does not keep working afterwards.

Tooling on this machine: Windows, Git Bash + PowerShell, Node.js available (use it for scripts), `python` is only the
Store stub. Bash heredocs with Ukrainian text and quotes sometimes break — write scripts to the scratchpad with the
Write tool and run them with `node`. `/tmp` in Git Bash is not visible to Windows Node: pass `$(cygpath -w /tmp/x.js)`.

## Architecture (Clean Architecture, .NET 10 / C# 14 + React)

```
src/KOD.Domain          Entities + invariants (User, Role, StaffPosition, OrganizationUnit, UnitType, Document with
                        signers and signatures, NewsArticle with NewsComment/NewsReaction/NewsView, Chat with ChatMember/
                        ChatMessage/ChatAttachment/ChatReaction, Ticket with TicketMessage/TicketAttachment,
                        Notification, MediaFile, AuditEntry),
                        permission catalog, system roles, Guard, DomainException, localization catalog
                        (Localization/Texts.cs, Languages.cs).
src/KOD.Application     Use-case services (DocumentService, UserService, OrganizationService, UnitTypeService, RoleService,
                        NewsService, NewsDiscussionService, ChatService, TicketService, NotificationService + Notifier,
                        AttachmentStore, MediaService + MediaLibrary, ProfileService, DashboardService,
                        AuditService, SigningJournal), DTOs, Result<T>/Error, paging, access rules (IPermissionResolver,
                        IAccessScope, AdministrationScope), abstractions.
src/KOD.Infrastructure  EF Core + PostgreSQL (snake_case, citext, xmin concurrency), migrations, seeding, Identity
                        (full Identity schema, SignInManager with mandatory 2FA, claims factory, ES256 key + JWT service
                        in Identity/Tokens), file storage,
                        images (libvips/NetVips → AVIF, media storage, cleanup job), HTML sanitizer, XLSX/CSV, Markdig,
                        CMS signature inspector, audit logger.
src/KOD.Web             Host: REST API (Api/, /api/v1), SignalR hub (Realtime/), JWT authentication handlers (Identity/),
                        security middleware (Security/), images (Media/), signing proxy and trust list (Signing/),
                        localization provider (Localization/), logging and SPA hosting (Hosting/). wwwroot holds only
                        the IIT signing library (lib/eusign).
src/KOD.Client          React SPA at / (see "React client").
tests/KOD.UnitTests     Domain rules, access, scoped administration (EF InMemory), JWT tokens, localization coverage,
                        API contracts, trust list.
```

### Hosting

- `SpaHostingExtensions`: static files of `Spa:Root` (`../KOD.Client/dist` in development, `ClientApp` when published;
  `/assets/*` cached for a year, the rest revalidated) and `index.html` as the fallback of every route without a file
  extension. Unknown `/api/*` addresses answer 404 problem details; the former `/app/*` addresses redirect to the root.
- Errors answer with problem details (`ApiExceptionHandler`, RFC 9457). Strict CSP (`SecurityHeadersMiddleware`): no
  inline scripts, no `eval`, `connect-src` allows the own origin and its WebSocket for SignalR.
- `AccountSetupMiddleware`: until the temporary password is replaced and a profile photo uploaded, the API answers 403
  (except `/api/v1/auth` and `/api/v1/profile/photo`); the client's guards lead the user through the same steps.

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
  cookies, and answers 401 / 403 (never redirects: the client handles sign-in). `TwoFactorJwtHandler` is registered
  under `IdentityConstants.TwoFactorUserIdScheme`. Claim types are JWT names (`AppClaimTypes`); the security stamp never
  goes into a token.
- Revocation: sign-out deletes the family; a new security stamp (`IIdentityService.InvalidateSessionsAsync`: password
  change/reset, block, 2FA reset, "end all sessions", profile "sign out on other devices") stops every family bound to the
  old stamp, while `RefreshSignInAsync` rebinds the current browser's family. Issued access tokens live until expiry
  (≤ 5 min).

Dependencies point inward: Web → Infrastructure → Application → Domain. Central Package Management
(`Directory.Packages.props`); check new package versions on NuGet and prefer ones published more than two weeks ago.

### Images, news editor, discussion

- **Images** (`MediaFile`): every upload goes through `MediaLibrary` → `IImageProcessor` (`VipsImageProcessor`: loader
  allow-list, EXIF orientation, sRGB, metadata stripped, AVIF with quality/size lowered until the main rendition fits
  its budget — news 1600 px / 100 KB, avatars 320 px square) → `IMediaStorage` (`App_Data/storage/media/xx/{id}.avif` +
  `.thumb.avif`). Served by `GET /media/{id}` and `/media/{id}/thumb` to signed-in users, `immutable` cache.
  `MediaLibrary.SaveChangesAsync` deletes files of removed images only after the database save. `MediaCleanupService`
  removes unattached images after a day.
- **Profile photo is mandatory**: claim `kod:photo` (claims factory) → `AccountSetupMiddleware` (API 403) and the client
  guard `requireReadyUser` (→ `/account/photo`). The photo goes to `PUT /api/v1/profile/photo` (multipart, crop fractions
  from `PhotoPicker`), which re-issues the JWT cookies (`RefreshSignInAsync`). User DTOs carry `PhotoId`.
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
- **Real time: SignalR** (`Realtime/LiveHub` at `/api/v1/live`, `[Authorize]`, own-origin connections only,
  `CloseOnAuthenticationExpiration`: a connection closes when its access token expires and the client reconnects with a
  fresh one, so a revoked sign-in stops receiving within minutes). One connection per client (`shared/realtime/live.ts`:
  `useLiveEvent` for user-addressed messages, `useLiveGroup` for groups; it reconnects forever and backs off when
  connections drop right after connecting). Every change goes through REST (antiforgery, validation); the only client →
  hub call is `Typing`. News: `INewsDiscussionEvents` → the article group gets a signal, the page reloads through REST.
  Chats: `IChatEvents` → `LiveChatEvents` → `Clients.Users(members)` (`SubjectUserIdProvider` maps connections to the
  token subject) with the full `ChatMessageDto`; the members are read from the database for every change, never from
  SignalR groups. Hub JSON uses the API conventions (enums as names). With several servers add a SignalR backplane
  (Redis).
- **Chats** (`ChatService`, `features/chats`): one-to-one (`Chat.DirectKey`, unique per pair) and group chats (owner
  removes people, anyone renames/adds/leaves; the last member leaving deletes the chat with its files). Every message
  takes the next `Chat.LastSequence` (xmin concurrency, retried), so numbers have no gaps: the client detects a gap in a
  live message and fetches `?after=`; members keep `LastReadSequence` (unread = newer text messages of others).
  Deleting works like Telegram: an own message for everyone (`DeleteAsync`, placeholder) or any message for oneself
  (`HideAsync` → `HiddenChatMessage`; the reader gets an empty `IsHidden` placeholder, so numbering keeps no gaps,
  and the client keeps it hidden in `mergeMessages`); `ClearHistoryAsync` sets `ChatMember.ClearedSequence` (older
  messages are not returned, a cleared one-to-one chat leaves the list until a newer message); `DeleteChatAsync`
  removes a chat for everyone (either person of a one-to-one chat, the owner of a group). `ForwardAsync` copies messages
  into another chat as the user's new messages with `ForwardedFromId` (the first author) and copies their files (a
  forwarded message cannot be edited); the client picks the chat or a person in `ForwardDialog`.
  `ClientId` (unique per chat and author) makes a resend idempotent; the client shows the message at once as pending
  and offers "send again" with the same id. Bodies are sanitized by `IRichTextSanitizer.SanitizeMessage` (text
  formatting and links only) and `ToText` gives previews. Attachments (`FileRules.ChatFileTypes` allow-list, 10 × 25 MB,
  50 MB per message) go to `IFileStorage` (`chats/…`); photos become AVIF (`MediaPurpose.Chat`) with a thumbnail; all
  are served by `GET /api/v1/chats/{id}/attachments/{attachmentId}` to members only, as downloads. The client keeps
  each open conversation in the query cache (`threadQuery`, merged by `mergeMessages`), and `ChatSync` (mounted in the
  shell for users with `chats.use`) applies live events everywhere, including the unread badge of the menu.
- **Requests** (`Ticket`, `TicketService`, `features/tickets`, routes `/requests`, `/requests/new`, `/requests/:id`,
  `/admin/requests`; API `/api/v1/tickets`): every user submits requests and writes in their own; `tickets.manage`
  (cabinet-wide, module `Tickets`) sees all, answers, assigns (`PermissionHolders.UsersWith` lists the handlers; a
  system administrator always holds every permission), changes state/category/priority. `Ticket.Comment` makes the
  automatic changes (first handler answer → In progress + assignee; author answer on Waiting/Resolved → In progress) and
  returns the history entries to add. `Number` is an identity column. Plain-text bodies; files go through
  `AttachmentStore` (shared with chats: `FileRules.ChatFileTypes`, photos → AVIF + thumbnail) into `IFileStorage`
  (`tickets/…`), served only to the author and handlers. Client allow-list: `shared/lib/attachments.ts`.
- **E-mail** (`IEmailOutbox` → `EmailOutbox` channel → `SmtpEmailSender` background service, MailKit; settings
  `Email:*` in `EmailOptions`, password only in user secrets): requests never wait for SMTP; nothing is sent without
  `Email:Host` and `Email:FromAddress`. Bodies are built by `EmailLayout` (inline-styled tables in the cabinet's
  style, plain text twin) inside `EmailLayout.InLanguage(user.Language, …)` so `Texts.T` follows the recipient. New
  requests: `TicketMail.NewTicketAsync` writes to every `tickets.manage` holder except the author.
- **Notifications** (`Notification`, `INotifier` for other services, `INotificationService` for the user,
  `features/notifications`): services call `notifier.NotifyAsync(draft, recipients)` **after** their own save (it saves
  separately, skips the actor and inactive accounts, logs failures instead of throwing) and `MarkReadAsync(user,
  entityId)` when the user has seen the thing (document/ticket opened, chat read to the end). `Collect` folds chat
  messages into one unread notification per chat (`Count`); `OnceSince` tells a signer once per submission. Titles are
  built on the client from `Kind` (`features/notifications/display.ts`); `Code` carries a machine value (the new ticket
  state). Live: `LiveNotificationEvents` → `NotificationReceived(dto, unreadCount)` / `NotificationsChanged(unreadCount)`
  to the user; `NotificationSync` (shell) updates the badge, invalidates `["tickets"]`/`["documents"]` and toasts (not
  for chats). The bell (`NotificationTrigger`) opens `NotificationPanel` (a `popover="auto"` inside the shell, so it
  reads `--_sidebar-current`). News reply links end with `#comment-{id}` (`Discussion` scrolls there, `:target` marks it).

## React client (`src/KOD.Client`)

- **Stack** (latest versions; the user allowed the newest npm releases, the two-week rule is for NuGet): React 19 +
  React Compiler (`@rolldown/plugin-babel` + `reactCompilerPreset`), Vite 8, TypeScript 6 (strict,
  `noUncheckedIndexedAccess`; `exactOptionalPropertyTypes` is off — it fights CSS-module classes and React props),
  React Router (data mode, `createBrowserRouter`, lazy routes), TanStack Query, openapi-fetch + openapi-typescript,
  react-hook-form + zod, Lingui, lucide-react, Vitest + happy-dom + Testing Library.
  Pinned on purpose: `typescript` 6.0.3 (typescript-eslint does not support 7 yet), `@rolldown/plugin-babel` 0.2.4
  (0.3 breaks `@vitejs/plugin-react` 6).
- **No UI libraries**: every component is written by hand (the design system lives in `shared/ui` and
  `shared/styles`). Add libraries only for real infrastructure, never for trivial cases.
- **Structure** (bulletproof-react, enforced by `no-restricted-imports` in `eslint.config.js`): `src/shared` (api, i18n,
  lib, realtime, session, signing, styles, ui) ← `src/features/<feature>` (api, components, pages; a feature never
  imports another feature) ← `src/app` (router, guards, layout, query client). Cross-feature state (the auth context,
  permissions) lives in `shared/session`.
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
  (`requireReadyUser`: signed in, own password, photo; `requireUser`, `requirePasswordSet`, `requireAnonymous`;
  `requirePermission(a, b, …)` lets a route through with any of the permissions, `adminPermissions` for `/admin`).
  Return addresses go through `safeReturnUrl`.
- **Forms**: react-hook-form + `zodResolver`; build the zod schema inside the component with `t` so its messages are
  translated; `<Field>` gives the control its id / `aria-describedby` / `aria-invalid` through a render prop.
- **Pages** set the document title with `<DocumentTitle>` or `<PageHeader>` (React hoists `<title>`).
- Global UI services are module stores read with `useSyncExternalStore`: `toast(text, { tone })` (`success` by
  default, `error`, `warning`, `info`; a top-layer popover, so it shows above modal dialogs), `await confirm({...})`,
  `await confirmWithOption({..., option})` (a check box such as "also delete for…"; null when cancelled).
- Loading data shows skeletons shaped like the content (`shared/ui/Skeleton`: `SkeletonTable`, `SkeletonList`,
  `SkeletonCards`, `SkeletonPage`, `SkeletonMessages`, `SkeletonText`, primitives `Skeleton` in a `SkeletonGroup`), not
  a spinner; `Spinner` stays only for a running action (a message being sent).
- Every outcome of an action (saved, deleted, failed, a rejected file, a form-level check) is a toast, never an alert on
  the page; inline `Alert`s are for load errors, explanations and results that are content (an import report, a
  signature verification). Field errors stay under their fields.
- List pages keep search, filters, sorting and paging in the history entry, not in the address bar:
  `useViewParams` (`shared/lib/viewParams.ts`, the `useSearchParams` API over `location.state.view`) survives reloads
  and back/forward; an address that still has a query string is moved into the state once. `Tab`, `ButtonLink`,
  `StatCard` and breadcrumbs pass `to` through `viewTarget`, so `/documents?scope=AwaitingMe` opens `/documents` with
  that view; a plain `<Link>`/`navigate` to another page's view uses `viewTarget` too. (`DataGrid` sorts by
  `sortBy`/`descending`, `Pagination` by `page`/`pageSize`; `useListParams` in `features/admin`.) `DatePicker` (`shared/ui`) takes and
  returns `yyyy-MM-dd`, accepts typed dates in the day-month-year order and opens a calendar popover.
- Temporary passwords (new account, reset, import) are shown once and never put into the history state; the file of
  an import's passwords is built by `POST /api/v1/user-imports/credentials` from the report the browser holds.
- **Main menu** (`app/layout/navigation.ts`): `home` + sections (`NavigationGroup`) rendered by `NavGroups` as an
  accordion — the section of the current page is open until the user opens another (the choice is kept per pathname,
  no effects); a one-item section renders as a plain link; folded sections are `inert`; on the collapsed rail every
  entry shows. The tab bar of phones takes `navigation.items` with `tabBar`.
- Unit type icons are stored as Lucide names (`UnitType.Icons` on the server); the client maps them to components in
  `shared/lib/unitTypeIcons.ts` — add a new icon in both places. Module names: `moduleNames` in `shared/i18n/display.ts`.
- After a language change, queries other than the auth context are invalidated: some data comes translated by the
  server (permission titles, role and unit type names).
- **Signing** (`shared/signing`): `SigningClient` (typed worker client; worker messages are Ukrainian and translated
  through a table in `client.ts`), `SigningWizard` (document or local files), `KeyPanel`, format rules in `formats.ts`.
  The worker (`eusign.worker.js`, plain JS) is bundled by Vite (`new Worker(new URL(...))`); the IIT library stays in
  `KOD.Web/wwwroot/lib/eusign` (16 MB, served compressed by `MapStaticAssets`; the trust list fallback lives there too).
  Keep the order: key read only on the last step, released after signing / going back / closing / leaving.
- **Manual** (`features/help`): the table of contents (anchors, titles, icons, permissions) is `contents.ts`; the text is
  `chapters/uk/<X>Chapter.tsx` and its English twin `chapters/en/<X>Chapter.tsx` with the same anchors (only the
  reader's language is loaded). Building blocks from `components/Manual.tsx`: `<Chapter id>`, `<Topic id>`, `<Ui>` (an
  interface label written exactly as on screen), `<Steps>`, `<Ref to>` (link to a topic), `<Table>`, `<Faq>`, `<Code>`.
  The roles chapter is built from `GET /api/v1/permissions` and `/permissions/system-roles`. Update both languages when
  a feature changes; `contents.test.ts` checks that both use every anchor and that every `<Ref>` points to a topic.

## Conventions

- **StyleCop + analyzers, TreatWarningsAsErrors.** XML documentation in English on every type and member, including
  private ones. Member order matters (SA1201/SA1202: fields before properties, public before private). Avoid
  multi-line `a && b ? x : y` (SA1143) — use `if`.
- Modern C#: file-scoped namespaces, primary constructors, `field`, extension blocks (`extension(IServiceCollection services) { … }`),
  collection expressions, `_camelCase` private fields, no `this.`.
- TypeScript/React: JSDoc on every exported and inner function, component and props member; function components,
  named exports, `@/` imports across folders. Plain JS (the signing worker): JSDoc on every function.
- Match the surrounding code: comment density, naming, idioms. Code and docs in English, UI texts in Ukrainian.
- Commits: imperative subject, body with bullets, end with the Co-Authored-By line. Branch `KOD/full-refactoring`,
  main branch `develop`.

## Localization (Ukrainian source, English translation)

- **The Ukrainian text is the key.** Server: `Texts.T("…")`. Client: Lingui (below).
- With values: `T("Знайдено {0} документів", count)` — never interpolate or concatenate inside `T()`; the key must be a
  plain literal so the coverage test can find it.
- Same Ukrainian word, different English: `Texts.In("context", "…")` looks up `"context|…"` first
  (e.g. `In("document-kind", "Довідка")` = Certificate, plain `T("Довідка")` = Help; `audit|…` for audit event names;
  the manual titles use the `help` context in the client).
- Server translations: `src/<Project>/Localization/en/*.json` (flat `{ "uk": "en" }`, embedded automatically by
  `Directory.Build.props`; KOD.Web keeps its few texts in `web.json`).
- Data-annotation `ErrorMessage` stays a Ukrainian constant (translated when shown).
- `Guard.Text(value, max, "Назва")` translates the field name itself — add the name to a catalog.
- `PermissionDefinition.Title/Description` and `SystemRoleDefinition.Name/Description` are translated properties; the
  Ukrainian source is `Source*` (used for seeding). Seeded role and unit-type names are translated in display-only DTOs
  via `StoredText.Display` (never in edit models — a translated name must not be saved back).
- Database data (audit details, user-entered names, seed data) is not translated.
- Culture: `LanguageCultureProvider` (claim `kod:lang` from the profile, then cookie `__Host-kod.lang`, else uk-UA);
  `UseRequestLocalization` runs **after** authentication. Language switch: `PUT /api/v1/auth/language` (saves the
  profile, sets the cookie, re-issues the tokens).
- **React client**: Lingui with Ukrainian source texts — ``t`Зберегти` `` / `<Trans>` from `@lingui/react/macro`,
  ``msg`…` `` for module-level descriptors (shown with `t(descriptor)`), `msg({ message, context })` for the same word
  with different meanings (the context matches the server's `In("context", …)`). Catalogs:
  `src/KOD.Client/src/locales/{uk,en}/messages.po`; after changing texts run `pnpm i18n:extract` and fill every English
  `msgstr`. The production build fails on a missing translation. Dates in Kyiv time through `shared/i18n/format.ts`,
  enum names through `shared/i18n/display.ts`.
- `LocalizationTests` fails for any `T(…)`, `In(…)`, `ErrorMessage` or Guard field name literal in the C# sources without
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
- State-changing API requests need the `X-CSRF-TOKEN` header (`ApiAntiforgeryFilter`; the token comes with the auth
  context and is bound to the identity).
- Private files (documents, signatures) live outside `wwwroot` (`App_Data/storage`) and are served only through the API
  after an access check.
- Administration is scoped to the organizations of the administrator's positions (`AdministrationScope`); `roles.manage`
  and `audit.view` are cabinet-wide.

## Logging

Serilog (`Hosting/LoggingExtensions.cs`, `LoggingOptions`): console, daily rolling file `App_Data/logs/kod-YYYYMMDD.log`
(31 files), PostgreSQL table `logs` (auto-created, 90-day retention). One line per request (`UseRequestLogging`): pages and
API at Information, static files, images and the SignalR connection at Debug, >3 s Warning, failures Error. Levels in
`Serilog:MinimumLevel`. Use `[LoggerMessage]` partial methods for new log statements.

## Design system notes

- Monochrome Diia-like style, Manrope (fonts in `shared/styles/fonts`), Lucide icons (`lucide-react` through `<Icon>`).
- Buttons and button-like controls have the "ink" hover: a `::before` fill of the theme's contrast colour fades in
  (opacity, `--ds-hover-duration`/`--ds-hover-ease`; compose `ink` from `shared/styles/ink.module.css`). Hover shadows
  transition with the same tokens — add `box-shadow` to the `transition` of anything that gains a shadow. Inverse
  surfaces flip the fill colours (`data-inverse`). Inputs show focus with the border only.
- Toasts are monochrome cards on the inverse surface; only the icon takes the tone colour (`--ds-color-*-on-inverse`).
- Brand: the mark is the message `К` (English "C") and the short name `КОД` (English "ODC"), on the server too (e-mail header,
  authenticator issuer).
- Popovers (`Select`, `DatePicker`) are `popover="manual"` panels in the top layer, placed next to their control, so
  they also open above modal dialogs.
- Scrollbars of dialogs, listboxes and the manual's contents are styled; in Chromium do not set `scrollbar-width`
  on them (it disables the `::-webkit-scrollbar` styles).

## Gotchas learned the hard way

- **EF Core:** filter before projecting into a record (`Where` on a projected record's property cannot be translated).
  Calling .NET methods (e.g. `Texts.T`, `StoredText.Display`) is fine only in the final projection. The InMemory test
  provider does not catch untranslatable queries — check new queries against PostgreSQL in the browser.
- Manual performance: topics use `content-visibility: auto`; `HelpPage` renders them once (`data-help-measuring`) to
  remember real heights so anchors land exactly. Keep that when changing the manual's layout. Its scroll, search
  highlight and contents tracking run in animation frames, which a hidden browser tab pauses.
- PAdES has no signing-time attribute; verification shows the signature time-stamp, else the PDF `/M` time.
- Cumulative signing could only be checked structurally here: producing real signatures needs a qualified key, so test
  append flows with the user (CAdES detached/enveloped, PAdES, ASiC-S/E, sequential and parallel routes).
  A PDF without `/ByteRange` has no signatures (the library would report "damaged data").
- The audit log category filter uses the address parameter `category` (the API's `Action` prefix), not `action`.
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
  JWT, the one issued during a refresh) must carry the token issuer (`urn:kod`), or a token issued in one request fails
  with 400 in the next. Testing token refresh: run the app with
  `--Authentication:Jwt:AccessTokenLifetime=00:00:10 --Authentication:Jwt:ClockSkew=00:00:01` after `--`.
- **Vitest transform cache**: `import.meta.glob` results are cached in `node_modules/.vitest-cache`; after adding a
  file a glob should find (e.g. a manual chapter) delete that folder if a test still sees the old list.
- **JWT clock skew and SignalR**: a token past its expiry but within the skew is renewed on requests that can set
  cookies (`JwtAuthenticationHandler`); otherwise a SignalR connection opened with it closes at once and reconnects in
  a loop until the skew runs out.
- Navigating the browser to the same address with another `#hash` does not reload the page (the client keeps its
  language and state); reload explicitly when a test needs a fresh start.

## Verifying changes

1. Build + tests with your configuration; for the client `pnpm typecheck && pnpm lint && pnpm test && pnpm build`.
2. For UI changes start `kod-web-verify` (7181) via the preview tools; the JWT cookies are shared with 7180 (same host, same signing key in `App_Data/keys`).
   If the pane is signed out, ask the user to sign in in the Browser pane (2FA) — do not use their credentials.
3. Check console/server errors (`preview_logs`), use `javascript_tool` for computed state and `find`/refs for clicks.
4. Report what was and was not verified; restore emulated viewports; stop your verification server.
