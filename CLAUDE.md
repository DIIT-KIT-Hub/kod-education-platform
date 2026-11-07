# CLAUDE.md — ODC / КОД (Organizational Documents Cabinet, Кабінет організаційних документів)

Electronic document management for any organization: documents with signing routes and a **qualified electronic
signature (QES) made in the browser**, several organizations with arbitrary unit trees, roles and permissions per
position, news, profile, admin panel, user manual. The UI is Ukrainian with a full English translation.
`README.md` (Ukrainian) is the user/operator documentation; this file is the working guide for changing the code.
Every project has its own `CLAUDE.md` with its layout, rules and pitfalls — read the one of the project you touch:
`src/ODC.Domain`, `src/ODC.Application`, `src/ODC.Infrastructure`, `src/ODC.Web`, `src/ODC.Client`,
`tests/ODC.UnitTests`.

Naming: the solution is `OrganizationalDocumentsCabinet.slnx` (the English expansion of the logo), projects are `ODC.*`
(namespaces too), and every runtime identifier uses the same prefix: cookies `__Host-odc.*`, claim types `odc:*`, JWT
issuer `urn:odc`, Data Protection application `ODC` and purposes `ODC.*`, the embedded translation prefix `odc-i18n:`,
the user-secrets id `odc-web-…`, log files `odc-*.log`, the database `odc`, demo accounts `@odc.local`. The old `kod`
names are gone (only the git branch `KOD/full-refactoring` and the repository folder keep them). Changing any of these
identifiers signs everyone out or makes stored keys unreadable — acceptable during development, not after a release.

ODC.Web is an ASP.NET Core Web API (REST under `/api/v1`, a SignalR hub, image and signing endpoints) that also serves
the React single-page application (`src/ODC.Client`) at the root of the site. There are no server-rendered pages.

## Commands

```bash
# Build everything with tests. Use your own configuration name (Lead, Release, …): the user often runs the app
# from bin/Debug, which locks the Debug output. Warnings are errors (StyleCop + .NET analyzers).
dotnet build tests/ODC.UnitTests/ODC.UnitTests.csproj -c Lead -v q -nologo -p:SkipClientBuild=true 2>&1 | grep -E "error|warn" | sort -u

# Tests (xUnit v3 on Microsoft Testing Platform; ~160 tests, ~30 seconds)
dotnet test tests/ODC.UnitTests -c Lead --no-build

# Run (migrations and seeding run at startup). The user's own instance is usually on https://localhost:7180;
# for your own verification use the launch config "odc-web-verify" (port 7181, -c Lead) in .claude/launch.json.
dotnet run --project src/ODC.Web --launch-profile https

# Migrations (local tool). Build with a non-Debug configuration if the app is running.
dotnet tool restore
dotnet ef migrations add <Name> --project src/ODC.Infrastructure --startup-project src/ODC.Web --configuration Release --output-dir Persistence/Migrations
```

```bash
# React client (src/ODC.Client; Node 24+, pnpm). In development ODC.Web serves its dist/ (Spa:Root). Building ODC.Web
# (F5 in Visual Studio, dotnet run/build) runs pnpm install/build itself when the lockfile or client sources are newer
# than the last result (incremental targets in ODC.Web.csproj); -p:SkipClientBuild=true skips that.
cd src/ODC.Client
pnpm install
pnpm build                  # tsc -b + vite build (production; fails on a missing English translation)
pnpm dev                    # vite build --watch in development mode (sourcemaps); reload the page after a rebuild
pnpm typecheck && pnpm lint && pnpm test
pnpm format                 # Prettier (indent from .editorconfig: 4 spaces)
pnpm i18n:extract           # update src/locales/{uk,en}/messages.po after changing texts
pnpm api:types              # regenerate src/shared/api/schema.d.ts from a running server's /openapi/v1.json
                            # (the script points at 7180; for your own server run the same command with 7181)
```

Publishing ODC.Web runs `pnpm install --frozen-lockfile && pnpm build` and copies `dist` (without source maps) into
`ClientApp` next to the app (`Spa:Root`); `-p:SkipClientBuild=true` skips the client build.

Secrets live in user-secrets of `src/ODC.Web` (`ConnectionStrings:Database`, `Seed:DemoPassword`, `Email:*`). Never print
or commit them. There is no admin password anywhere: the first administrator (`Seed:AdminEmail`) is created without a
password and activates the account by e-mail like everyone else. For your own runs set `Email:PickupDirectory` so
codes land in `.eml` files instead of real mailboxes (the user's secrets contain a real SMTP server).

There is one migration (`InitialCreate`, history squashed on 2026-11-07). An existing database created from the old
history cannot be migrated: it has to be dropped and recreated (ask the user first — it is their data).

Tooling on this machine: Windows, Git Bash + PowerShell, Node.js available (use it for scripts), `python` is only the
Store stub. Bash heredocs with Ukrainian text and quotes sometimes break — write scripts to the scratchpad with the
Write tool and run them with `node`. `/tmp` in Git Bash is not visible to Windows Node: pass `$(cygpath -w /tmp/x.js)`.

## Architecture (Clean Architecture, .NET 10 / C# 14 + React)

```
src/ODC.Domain          Entities + invariants (User, Role, StaffPosition, OrganizationUnit, UnitType, Document with
                        signers and signatures, NewsArticle with NewsComment/NewsReaction/NewsView, Chat with ChatMember/
                        ChatMessage/ChatAttachment/ChatReaction, Ticket with TicketMessage/TicketAttachment,
                        Notification, MediaFile, AuditEntry),
                        permission catalog, system roles, Guard, DomainException, localization catalog
                        (Localization/Texts.cs, Languages.cs).
src/ODC.Application     Use-case services (DocumentService, UserService, OrganizationService, UnitTypeService, RoleService,
                        NewsService, NewsDiscussionService, ChatService, TicketService, NotificationService + Notifier,
                        AttachmentStore, MediaService + MediaLibrary, ProfileService, DashboardService,
                        AuditService, SigningJournal), DTOs, Result<T>/Error, paging, access rules (IPermissionResolver,
                        IAccessScope, AdministrationScope), abstractions.
src/ODC.Infrastructure  EF Core + PostgreSQL (snake_case, citext, xmin concurrency), migrations, seeding, Identity
                        (full Identity schema, SignInManager with mandatory 2FA, claims factory, ES256 key + JWT service
                        in Identity/Tokens, account activation codes in
                        Identity/Activation), file storage, e-mail outbox (SMTP or pickup folder),
                        images (libvips/NetVips → AVIF, media storage, cleanup job), HTML sanitizer, XLSX/CSV, Markdig,
                        CMS signature inspector, audit logger.
src/ODC.Web             Host: REST API (Api/, /api/v1), SignalR hub (Realtime/), JWT authentication handlers (Identity/),
                        security middleware (Security/), images (Media/), signing proxy and trust list (Signing/),
                        localization provider (Localization/), logging and SPA hosting (Hosting/). wwwroot holds only
                        the IIT signing library (lib/eusign).
src/ODC.Client          React SPA at / (see "React client").
tests/ODC.UnitTests     Domain rules, access, scoped administration (EF InMemory), JWT tokens, localization coverage,
                        API contracts, trust list.
```

### Hosting

- `SpaHostingExtensions`: static files of `Spa:Root` (`../ODC.Client/dist` in development, `ClientApp` when published;
  `/assets/*` cached for a year, the rest revalidated) and `index.html` as the fallback of every route without a file
  extension. Unknown `/api/*` addresses answer 404 problem details; the former `/app/*` addresses redirect to the root.
- Errors answer with problem details (`ApiExceptionHandler`, RFC 9457). Strict CSP (`SecurityHeadersMiddleware`): no
  inline scripts, no `eval`, `connect-src` allows the own origin and its WebSocket for SignalR.
- `AccountSetupMiddleware`: until a profile photo is uploaded, the API answers 403 (except `/api/v1/auth` and
  `/api/v1/profile/photo`); the client's guards lead the user to the same step.

### Identity tables and authentication

- `ApplicationDbContext` is a full `IdentityDbContext<User, Role, Guid, UserPermissionOverride, StaffPosition,
  IdentityUserLogin<Guid>, RolePermission, IdentityUserToken<Guid>>`; tables `users`, `roles`, `user_roles`, `role_claims`,
  `user_claims`, `user_logins`, `user_tokens` (snake_case names of the AspNet* tables). Domain types are the Identity types:
  `Role : IdentityRole` (`Name` = unique display name, `NormalizedName` via `Role.NormalizeName`; `Code` stays the stable
  key), `RolePermission : IdentityRoleClaim` and `UserPermissionOverride : IdentityUserClaim` (claim types in
  `PermissionClaimTypes`: `odc:permission` granted, `odc:permission.denied`), `StaffPosition : IdentityUserRole` with its
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
  `JwtAuthenticationHandler` (scheme `Jwt`, `AppSignInManager.AuthenticationScheme`) reads `__Host-odc.at`, falls back to
  `__Host-odc.rt` (rotates, re-reads the user: active + stamp, rebuilds claims) only when the response can still set
  cookies, and answers 401 / 403 (never redirects: the client handles sign-in). `TwoFactorJwtHandler` is registered
  under `IdentityConstants.TwoFactorUserIdScheme`. Claim types are JWT names (`AppClaimTypes`); the security stamp never
  goes into a token.
- Revocation: sign-out deletes the family; a new security stamp (`IIdentityService.InvalidateSessionsAsync`: password
  change/reset, block, 2FA reset, "end all sessions", profile "sign out on other devices") stops every family bound to the
  old stamp, while `RefreshSignInAsync` rebinds the current browser's family. Issued access tokens live until expiry
  (≤ 5 min).

Dependencies point inward: Web → Infrastructure → Application → Domain. Central Package Management
(`Directory.Packages.props`); check new package versions on NuGet and prefer ones published more than two weeks ago.

### Account activation (no temporary passwords)

Nobody hands out passwords. Accounts are created (UI, import, seeding of the first admin) **without a password**:
`User.AwaitsActivation` ⇔ `PasswordHash == null`. An admin "password reset" (`IIdentityService.RemovePasswordAsync`)
removes the password and changes the security stamp; the authenticator stays and is asked for after the reset.
Invitations / reset notices: `AccountEmails` (Application/Common/Email), sent by `UserService` when e-mail is configured.

- Flow (`AccountActivationService` in Infrastructure/Identity/Activation, `AccountActivationController` at
  `/api/v1/auth/activation`, client `features/auth/pages/ActivationPage.tsx` at `/account/activate`):
  `POST` e-mail → a 6-digit code by e-mail → `POST /code` → `POST /password` (Identity validators, `EmailConfirmed`)
  → `PasswordSignInAsync` opens the usual 2FA step (`login/authenticator` enrolment or `login/two-factor`) → photo.
- State: `ActivationChallenge` rows (`activation_challenges`: SHA-256 of the address, nullable user, code encrypted with
  Data Protection, expiry, failed attempts, `VerifiedAt`, `xmin`), bound to the browser by the encrypted cookie
  `__Host-odc.activation` (time-limited protector, 30 min, `SameSite=Strict`). Only the newest row of an address is valid;
  rows go away after the password is set and, at the latest, a day later (`RefreshTokenCleanupService`).
- No enumeration: a row is written for **every** valid address (no e-mail and no matching code when there is no active
  account awaiting activation), so answers, limits and timing match (`MinimumRequestDuration` pads the request).
- Limits in `ActivationOptions` (`Authentication:Activation`): code 10 min, password step 15 min, 5 wrong codes, 60 s
  between codes, 5 codes per address per hour; plus the `activation` rate limit policy (30 requests / 10 min per IP).
- Error codes the client relies on: `activation_invalid`, `activation_expired`, `no_activation`, `too_many_codes` (429),
  `email_unavailable` (503). Audit: `auth.activation-code-sent`, `auth.activation-failed`, `auth.account-activated`.
- There is no self-service "forgot password": the admin resets it and the user goes through the activation again.

### Images, news editor, discussion

- **Images** (`MediaFile`): every upload goes through `MediaLibrary` → `IImageProcessor` (`VipsImageProcessor`: loader
  allow-list, EXIF orientation, sRGB, metadata stripped, AVIF with quality/size lowered until the main rendition fits
  its budget — news 1600 px / 100 KB, avatars 320 px square) → `IMediaStorage` (`App_Data/storage/media/xx/{id}.avif` +
  `.thumb.avif`). Served by `GET /media/{id}` and `/media/{id}/thumb` to signed-in users, `immutable` cache.
  `MediaLibrary.SaveChangesAsync` deletes files of removed images only after the database save. `MediaCleanupService`
  removes unattached images after a day.
- **Profile photo is mandatory**: claim `odc:photo` (claims factory) → `AccountSetupMiddleware` (API 403) and the client
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

## React client (`src/ODC.Client`)

React 19 + React Compiler, Vite, TypeScript, CSS Modules only, no UI libraries, Lingui, TanStack Query, openapi-fetch.
**Everything about the client — stack, structure, data, session and guards, forms, toasts, list views, the manual, the
design system and client gotchas — is in `src/ODC.Client/CLAUDE.md`.** Read it before changing the client.

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
  `Directory.Build.props`; ODC.Web keeps its few texts in `web.json`).
- Data-annotation `ErrorMessage` stays a Ukrainian constant (translated when shown).
- `Guard.Text(value, max, "Назва")` translates the field name itself — add the name to a catalog.
- `PermissionDefinition.Title/Description` and `SystemRoleDefinition.Name/Description` are translated properties; the
  Ukrainian source is `Source*` (used for seeding). Seeded role and unit-type names are translated in display-only DTOs
  via `StoredText.Display` (never in edit models — a translated name must not be saved back).
- Database data (audit details, user-entered names, seed data) is not translated.
- Culture: `LanguageCultureProvider` (claim `odc:lang` from the profile, then cookie `__Host-odc.lang`, else uk-UA);
  `UseRequestLocalization` runs **after** authentication. Language switch: `PUT /api/v1/auth/language` (saves the
  profile, sets the cookie, re-issues the tokens).
- **React client**: Lingui with Ukrainian source texts — ``t`Зберегти` `` / `<Trans>` from `@lingui/react/macro`,
  ``msg`…` `` for module-level descriptors (shown with `t(descriptor)`), `msg({ message, context })` for the same word
  with different meanings (the context matches the server's `In("context", …)`). Catalogs:
  `src/ODC.Client/src/locales/{uk,en}/messages.po`; after changing texts run `pnpm i18n:extract` and fill every English
  `msgstr`. The production build fails on a missing translation. Dates in Kyiv time through `shared/i18n/format.ts`,
  enum names through `shared/i18n/display.ts`.
- `LocalizationTests` fails for any `T(…)`, `In(…)`, `ErrorMessage` or Guard field name literal in the C# sources without
  an English translation. Tests run in uk-UA (`TestCulture` module initializer).

## Security invariants (do not break)

- The private key and its password never leave the browser: they go only to the Web Worker
  (`src/ODC.Client/src/shared/signing/eusign.worker.js`, IIT EUSignCP-JS). The server receives the finished signature + certificate data,
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

Serilog (`Hosting/LoggingExtensions.cs`, `LoggingOptions`): console, daily rolling file `App_Data/logs/odc-YYYYMMDD.log`
(31 files), PostgreSQL table `logs` (auto-created, 90-day retention). One line per request (`UseRequestLogging`): pages and
API at Information, static files, images and the SignalR connection at Debug, >3 s Warning, failures Error. Levels in
`Serilog:MinimumLevel`. Use `[LoggerMessage]` partial methods for new log statements.

## Gotchas learned the hard way

Client-only gotchas (CSS modules, CSP and Vite, Vitest cache, the manual) are in `src/ODC.Client/CLAUDE.md`.


- **EF Core:** filter before projecting into a record (`Where` on a projected record's property cannot be translated).
  Calling .NET methods (e.g. `Texts.T`, `StoredText.Display`) is fine only in the final projection. The InMemory test
  provider does not catch untranslatable queries — check new queries against PostgreSQL in the browser.
- PAdES has no signing-time attribute; verification shows the signature time-stamp, else the PDF `/M` time.
- Cumulative signing could only be checked structurally here: producing real signatures needs a qualified key, so test
  append flows with the user (CAdES detached/enveloped, PAdES, ASiC-S/E, sequential and parallel routes).
  A PDF without `/ByteRange` has no signatures (the library would report "damaged data").
- The audit log category filter uses the address parameter `category` (the API's `Action` prefix), not `action`.
- **Record contracts**: validation attributes go on the primary constructor parameters (`[Required] string Body`), never
  `[property: Required]` — MVC throws (500) for the latter. `ApiContractTests` checks every contract.
- **API JSON**: enums are serialized as names and numbers strictly as numbers, for controllers and the OpenAPI document
  alike (`ConfigureJson` in `WebServiceCollectionExtensions`); otherwise the generated types say `number | string`.
  A computed form property needs `[BindNever]` or it shows up in the OpenAPI request body.
- **React Compiler runs before the Lingui macro** (`vite.config.ts`): a static element inside `<Trans>` (e.g.
  `<strong>Microsoft Authenticator</strong>`) can be hoisted into a memoized value, so the runtime message id no longer
  matches the catalog and the page shows an id like `OFoRj4`. Keep `<Trans>` children dynamic or use a plain `t`.
- **Bash heredocs** with Ukrainian text and backticks break in this shell (a TypeScript template literal is executed as
  a command substitution): write edit scripts with the Write tool and run them with `node`.
- **Antiforgery tokens are bound to the `sub` claim and its `Issuer`.** Every identity of a signed-in user (validated
  JWT, the one issued during a refresh) must carry the token issuer (`urn:odc`), or a token issued in one request fails
  with 400 in the next. Testing token refresh: run the app with
  `--Authentication:Jwt:AccessTokenLifetime=00:00:10 --Authentication:Jwt:ClockSkew=00:00:01` after `--`.
- **JWT clock skew and SignalR**: a token past its expiry but within the skew is renewed on requests that can set
  cookies (`JwtAuthenticationHandler`); otherwise a SignalR connection opened with it closes at once and reconnects in
  a loop until the skew runs out.

## Verifying changes

1. Build + tests with your configuration; for the client `pnpm typecheck && pnpm lint && pnpm test && pnpm build`.
2. For UI changes start `odc-web-verify` (7181) via the preview tools; the JWT cookies are shared with 7180 (same host, same signing key in `App_Data/keys`).
   If the pane is signed out, ask the user to sign in in the Browser pane (2FA) — do not use their credentials.
   To test sign-in or activation end to end without touching the user's data, run against a separate database with
   its own key: `ConnectionStrings__Database` (same server, another database name), `Authentication__Jwt__SigningKeyPath`
   (a throwaway P-256 PEM — a different database means different Data Protection keys, and without a PEM the app would
   regenerate and overwrite `App_Data/keys/jwt-signing-key.bin` shared with 7180), `Email__PickupDirectory`,
   `Seed__DemoData=false`. Activate the seeded admin with the code from the `.eml` file and compute TOTP codes from the key
   shown on the setup page. Never print the connection string.
3. Check console/server errors (`preview_logs`), use `javascript_tool` for computed state and `find`/refs for clicks.
4. Report what was and was not verified; restore emulated viewports; stop your verification server.
