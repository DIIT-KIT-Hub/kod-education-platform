# CLAUDE.md — ODC / КОД (Organizational Documents Cabinet, Кабінет організаційних документів)

Electronic document management for any organization: documents with signing routes and a **qualified electronic
signature (QES) made in the browser**, several organizations with arbitrary unit trees, roles and permissions per
position, news, profile, admin panel, user manual. The UI is Ukrainian with a full English translation.
`README.md` (Ukrainian) is the user/operator documentation; this file is the working guide for changing the code.
Every project has its own `CLAUDE.md` with its layout, rules and pitfalls — read the one of the project you touch:
`src/ODC.Web` (the whole server: vertical slices, mediator, domain events), `src/ODC.Client`, `tests/ODC.UnitTests`,
`tests/ODC.IntegrationTests`.

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

# Tests (xUnit v3 on Microsoft Testing Platform; ~500 tests, ~75 seconds; the document tests need PostgreSQL, see tests/ODC.UnitTests/CLAUDE.md)
dotnet test tests/ODC.UnitTests -c Lead --no-build

# Integration tests: the whole app in memory on a throwaway PostgreSQL database, through HTTP (~22 tests, ~65 seconds)
dotnet build tests/ODC.IntegrationTests/ODC.IntegrationTests.csproj -c Lead -v q -nologo -p:SkipClientBuild=true
dotnet test tests/ODC.IntegrationTests -c Lead --no-build

# Run (migrations and seeding run at startup). The user's own instance is usually on https://localhost:7180;
# for your own verification use the launch config "odc-web-verify" (port 7181, -c Lead) in .claude/launch.json.
dotnet run --project src/ODC.Web --launch-profile https

# Migrations (local tool), one set per database. Build with a non-Debug configuration if the app is running.
dotnet tool restore
# (environment variable SkipClientBuild=true: -p: is not passed through dotnet ef)
dotnet ef migrations add <Name> --project src/ODC.Web --configuration Lead --context MasterDbContext --output-dir Infrastructure/Persistence/Migrations/Master
dotnet ef migrations add <Name> --project src/ODC.Web --configuration Lead --context OrganizationDbContext --output-dir Infrastructure/Persistence/Migrations/Organization
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

A master database (`ConnectionStrings:Database`, e.g. `odc`) and one database per organization on the same server
(`<master>_org_<code>`, e.g. `odc_org_org`, `odc_org_fund`; see "Databases" below). Each context has one migration
(`InitialCreate`, 2026-11-11); the start migrates the master and every organization's database. A database created
before 2026-11-11 cannot be migrated: drop the master and every `<master>_org*` database and the roles
`<master>_org_*_owner` / `_app`, and let the start create them again (ask the user first — it is their data). Never run
`dotnet ef database update`: the design-time connection is the master's.

Operator commands (the app with its usual settings does the work and stops; README "Бази організацій: обслуговування"):
`dotnet run --project src/ODC.Web -c Lead -- migrate` | `backup CODE|--all` | `restore CODE FOLDER` |
`rotate-passwords CODE|--all` (`Hosting/OperatorCommands`).

Tooling on this machine: Windows, Git Bash + PowerShell, Node.js available (use it for scripts), `python` is only the
Store stub. Bash heredocs with Ukrainian text and quotes sometimes break — write scripts to the scratchpad with the
Write tool and run them with `node`. `/tmp` in Git Bash is not visible to Windows Node: pass `$(cygpath -w /tmp/x.js)`.

## Architecture (vertical slices, .NET 10 / C# 14 + React)

The server is **one project organized by feature** (vertical slice architecture): a use case is one file with its
request, handler and minimal API endpoint, and a feature folder holds its slices, contracts, entities and EF Core
configurations. The client is sliced by feature the same way. Details and rules: `src/ODC.Web/CLAUDE.md`.

```
src/ODC.Web             The server.
  Features/<Area>/      Slices (Documents, Chats, Tickets, News (+ Discussion), Notifications, Admin (Users, Roles,
                        Audit, Organizations), Structure (Units, UnitTypes), Auth (+ Activation), Profile, Media, Signing, Home);
                        Domain/ = the feature's entities and domain events, Persistence/ = their configurations.
  Shared/               Kernel (Entity, Result/Error, Guard, IDomainEvent), Domain/ (entities several features use:
                        User, Role and the permission catalog, OrganizationUnit, UnitType, MediaFile, AuditEntry),
                        access rules (IAccessScope, AdministrationScope), e-mail layout, files, media, paging, texts.
  Messaging/            Our own mediator: IRequestHandler<,> injected directly, decorators
                        Logging(Authorization(DomainEvents(handler))), domain event delivery. No MediatR.
  Endpoints/            IEndpoint, the secure /api/v1 group (signed-in + antiforgery by default), Result → HTTP.
  Infrastructure/       EF Core + PostgreSQL (MasterDbContext, OrganizationDbContext, DataPlacement, MemberSync, the
                        migrations, seeding), Identity + JWT, e-mail outbox, file/media storage (libvips → AVIF), CMS
                        signatures, sanitizer, audit.
  Security/, Hosting/,  Middleware and permission policies; DI, pipeline, SPA hosting, logging; SignalR hub.
  Realtime/
src/ODC.Client          React SPA at / (see "React client").
tests/ODC.UnitTests     Domain rules, slices through the real pipeline (EF InMemory), domain events, architecture
                        rules (slice boundaries, endpoint security), JWT, activation, localization, API contracts.
tests/ODC.IntegrationTests  The real host over HTTP (WebApplicationFactory) on a throwaway PostgreSQL database: sign-in
                        with the authenticator, activation by e-mail, sessions, administration, documents, chats,
                        requests, and what one organization must not see of another.
```

- Features meet only along the edges listed in `tests/ODC.UnitTests/Architecture/SliceArchitectureTests.cs`; side
  effects in other features go through **domain events** (e.g. notifications react to `DocumentSigningProgressed`,
  `TicketSubmitted`, `ChatMessagesPosted`, `NewsCommentPosted`, `ItemSeen`), delivered after the request succeeded.
- Pragmatic DDD: aggregates with invariants and domain events; no repositories (slices use `MasterDbContext` and
  `OrganizationDbContext`), no separate bounded-context projects, no outbox.

### Databases (master + organizations)

The plan (`docs`: the published "Бази організацій ODC" page) moves every organization into a database of its own.
Phases 2–6 are done: two contexts, the registry and routing (every organization has its own database), the client
(addresses `/o/:code/…`, the switcher, lists gathered from all organizations), the work across organizations, and the
maintenance (roles and secrets per database, encrypted files per organization, suspension and deletion, the migrator,
backups). Phases 7 (the isolation matrix) and 8 (the user's guide) follow.

- **Registry** (master): `organizations` (`Organization`: code, name, `DatabaseName`, `State` (Creating / Active /
  Suspended / Deleting), `SchemaVersion`, the encrypted passwords of its roles and key of its files) and `memberships`
  (`Membership`: who works in which organization — kept by `Memberships.SyncAsync` whenever positions change: a person
  with a position there is a member). `OrganizationRegistry` (singleton) keeps the registry in memory for a minute
  (`Invalidate` after a change). The root unit of an organization's structure has the organization's identifier.
- **Routing** (`Infrastructure/Tenancy`): `/o/{code}/…` addresses are served as the plain ones
  (`OrganizationPathMiddleware` moves the code into `PathBase`); `OrganizationMiddleware` (after authentication)
  chooses the organization — the one of the address, else the person's earliest membership, else (a platform
  administrator) the first one — and enters it into the scoped `OrganizationContext`. An unknown or suspended code
  answers 404 `organization_not_found`. `OrganizationDbContext` opens the database of `OrganizationContext.Require()`;
  without an organization it throws `NoOrganizationException` (409 `no_organization`): fail closed.
- **Permissions per organization**: the access token carries the union over the person's organizations
  (`IAccountPermissions`); `OrganizationMiddleware` replaces the permission claims of the request with those of the
  chosen organization (`OrganizationPermissions.Effective`, cached for a minute in `AccessCache`, `Changed()` drops
  it): a member gets their positions' permissions there; a platform administrator (`administration.all`) gets every
  permission everywhere and the platform-wide ones (`roles.manage`, `audit.view`, `tickets.manage`) follow the person;
  anyone else is a **guest** with at most `documents.view`, `documents.sign`, `chats.use` (data rules decide what they
  see: the documents they sign, the chats they are in).
- **People of other organizations** (`ExternalParticipants`): `AdmitAsync` lets a signer or chat member from another
  organization who holds the permission in their own take part: a `Participation` (master, written first) and a copy in
  this organization's `members` (no membership); they work there as guests through `/o/{code}/…`. `EnsurePresentAsync`
  does the same for a platform administrator at work there (middleware) and a guest who comments on a news of the
  whole cabinet. `SearchAsync` / `DescribeGuestsAsync` give the pickers (signers, chat partners) people of other
  organizations with their organization in place of the position. `MemberCopies` writes profile changes to every
  organization of memberships and participations.
- **Work across organizations** (`IAcrossOrganizations`, `Workplaces`): a person's organizations are their memberships
  (the default one first), participations and, for the switcher of a platform administrator, every other one
  (`OrganizationAccess.Member` / `Guest` / `Platform`; the auth context returns `organization` and `organizations`).
  `GatherAsync` / `GatherInAsync` / `EachAsync` run a read or change in each of a person's organizations (not every
  organization of a platform administrator): notifications (list, unread count, mark all read, delete read, items seen),
  the home page (documents waiting for the signature, counts) and the chat list carry `OrganizationId`/`OrganizationCode`
  of each item. `EveryAsync` reads every active organization (role and unit-type usage, the audit log of `audit.view`,
  which names the trail of each entry); `HoldersAsync` finds holders of a platform-wide permission everywhere (request
  handlers: lists, assignment, mail); `INotifier.NotifyWhereTheyWorkAsync` saves a notification in each recipient's
  home organization (requests to the support). Unit tests replace it with `SingleOrganization`.
- **News of the whole cabinet** stay in the organization they were written in; `GeneralArticle` (master, written after
  the save by `NewsDesk.IndexAsync`, rebuilt at every start) lets every other organization list them (`ListNews` merges
  them, `OrganizationCode` names where they open). A guest sees only the news of the whole cabinet of an organization.
- **Double audit**: what a platform administrator does in an organization they do not work in
  (`OrganizationAccess.Platform`; where they work they are a member) is audited there and, after its save, in the
  platform's trail with `[CODE]` in front of the details (`PendingAuditInterceptor`).
- No outbox: the master's pointers (participations, the news index) are written so that a lost write only hides
  something until the start repairs it, never shows what should not be seen.
- **Creating an organization** = creating a top-level unit (`CreateUnit` → `IOrganizationProvisioning`,
  `OrganizationProvisioning`): the registry row (`Creating`, new secrets), the roles, the database, the migrations as
  the owner, the root unit, then `Active`; a failure removes it all. Renaming the root unit renames the organization; a
  unit never moves between organizations. The platform administrator suspends, resumes, deletes (a suspended one, the
  code typed again) and rotates the passwords on `/admin/organizations` (`Features/Admin/Organizations`, audited in the
  platform's trail). A start finishes an interrupted creation or deletion.
- **PostgreSQL roles** (`OrganizationDatabaseAdmin`, with the master's account: `CREATEDB` + `CREATEROLE`):
  `<database>_owner` owns the database and runs the migrations and backups, `<database>_app` serves the requests (data
  only through default privileges, no DDL); `CONNECT` is revoked from `PUBLIC`. Passwords (256 bits) live in the
  registry encrypted with Data Protection, a purpose per organization (`OrganizationSecrets`);
  `OrganizationDatabases.ConnectionStringOf` builds the app role's connection, `OwnerConnectionStringOf` the owner's.
  `Organizations:SeparateRoles=false` uses the master's credentials for every database. A suspension locks the app role
  (`NOLOGIN`).
- **Files** (`LocalFileStorage`, scoped): `{PrivateRoot}/orgs/{organization}/…` with the organization's key and
  `{PrivateRoot}/platform/…` (`tickets`, images) with the platform's key (`PlatformSecret`), every file in the
  `EncryptedFile` format (AES-256-GCM in 64 KiB chunks, a key per file by HKDF, seekable for ranges). The organization
  is the unit of work's, so a storage key of another organization opens nothing. The SHA-256 of a document is of the
  content (the browser checks it before signing).
- **Migrator**: every start (`DatabaseInitializer.PrepareOrganizationsAsync`, `Organizations:MigrationParallelism` at
  once) repairs the roles and privileges, migrates as the owner and records `SchemaVersion`; one failure is logged and
  that organization answers 503 `organization_unavailable` (`OrganizationSchema`) while the others work.
- **Backups** (`OrganizationBackups`): `pg_dump` as the owner encrypted with the organization's key, its files (already
  encrypted) and a manifest; a restore replaces one organization only (and reloads the Npgsql types: the extension
  types get new identifiers — another running process must restart).
- **The server's own work** in an organization: `OrganizationScopes.RunAsync(organization, work)` (a scope with the
  organization entered); seeding, `MemberCopies` and `AccountPermissions` use it. SignalR runs hub calls in scopes of
  their own: `OrganizationHubFilter` enters the organization of the connection's request, so hubs resolve handlers
  inside their methods (the hub is created before the filters run).

- `DataPlacement` lists where every entity lives; each context applies only the configurations of its entities, and
  `DataPlacementTests` fail for an unplaced entity, a model that holds a foreign entity or a shared table name.
  - **Master** (`MasterDbContext : IdentityUserContext`): accounts (`users`, `user_logins`, `user_tokens`, an unused
    `user_claims`), refresh token families, activation challenges, Data Protection keys, the registry of organizations
    with memberships and participations, the index of the news of the whole cabinet, the catalogs every organization shares (roles with their permissions, unit types), images
    (`media_files`), requests to the platform support, the platform's audit trail.
  - **Organization** (`OrganizationDbContext`): the structure, **members** with their positions and individual
    permissions, documents, news with discussions, chats, notifications, the organization's audit trail, the document
    number sequence.
- `Member` (table `members`) is the organization's copy of an account's profile (name, e-mail, phone, photo, active,
  awaiting activation, last sign-in). Authors, signers, chat members and notification actors point at members, so the
  organization's queries never join the master database. `MemberSync` (an interceptor of the master context) copies
  every added or changed `User` into the databases of the account's organizations (`MemberCopies`) after the save — after the commit inside a transaction, nothing on a rollback — and the
  seeding repairs missing copies at every start. Never edit a member's profile on the organization's side.
- Positions (`StaffPosition`, table `staff_positions`) and individual permissions (`UserPermissionOverride`, table
  `permission_overrides`) belong to the member: `Member.SetPositions` / `SetPermissionOverrides`. They refer to roles
  and unit types by identifier only.
- The organization's side reads the catalogs through `Catalog` (scoped, loaded once per request: `RolesAsync`,
  `UnitTypesAsync`, `OrganizationTypeIdsAsync`, `RoleIdsGrantingAsync`) and people's positions through
  `PositionDirectory` (`PrimaryAsync`, `AllAsync`, `LookupAsync` for people pickers). Who holds a permission:
  `db.MembersWithAsync(catalog, permission)`.
- A slice that changes both databases saves them one after another (no transaction spans both): the master first when
  an account is created (the member must exist), the organization first otherwise. `IAuditLogger.Add` keeps the entry
  until the next save of either database (`PendingAudit`), so an organization's action is audited in its database and
  an account change in the master one; the audit log merges both trails.
- No foreign key crosses the databases (e.g. `media_files.news_article_id`): the code keeps those references.

### Hosting

- `SpaHostingExtensions`: static files of `Spa:Root` (`../ODC.Client/dist` in development, `ClientApp` when published;
  `/assets/*` cached for a year, the rest revalidated) and `index.html` as the fallback of every route without a file
  extension. Unknown `/api/*` addresses answer 404 problem details; the former `/app/*` addresses redirect to the root.
- Errors answer with problem details (`ApiExceptionHandler`, RFC 9457). Strict CSP (`SecurityHeadersMiddleware`): no
  inline scripts, no `eval`, `connect-src` allows the own origin and its WebSocket for SignalR.
- `AccountSetupMiddleware`: until a profile photo is uploaded, the API answers 403 (except `/api/v1/auth` and
  `/api/v1/profile/photo`); the client's guards lead the user to the same step.

### Identity tables and authentication

- `MasterDbContext` is an `IdentityUserContext<User, Guid, …>` (users, logins, tokens; no Identity role store:
  `ProtectedUserStore : UserOnlyStore`). `Role : IdentityRole` (`Name` = unique display name, `NormalizedName` via
  `Role.NormalizeName`; `Code` stays the stable key) and `RolePermission : IdentityRoleClaim` are mapped by
  `RoleConfiguration`, never through a `RoleManager`. Permission claim types are in `PermissionClaimTypes`
  (`odc:permission` granted, `odc:permission.denied`); query them through `ClaimType`/`ClaimValue` in LINQ. Positions
  and individual permissions live in the organization's database (see "Databases"); `PermissionResolver` combines them
  with the role catalog.
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

Central Package Management (`Directory.Packages.props`). Keep every dependency on its newest version — NuGet
packages, the dotnet tools (`dotnet-tools.json`), pnpm (`packageManager`) and the client packages — including releases
published only days ago. Prereleases only where no stable release exists (StyleCop.Analyzers is beta-only); a major
version that needs a newer SDK than the installed one (a .NET release candidate) waits for that SDK.

### Account activation (no temporary passwords)

Nobody hands out passwords. Accounts are created (UI, import, seeding of the first admin) **without a password**:
`User.AwaitsActivation` ⇔ `PasswordHash == null`. An admin "password reset" (`IIdentityService.RemovePasswordAsync`)
removes the password and changes the security stamp; the authenticator stays and is asked for after the reset.
Invitations / reset notices: `AccountEmails` (`Shared/Email`), sent by the user administration slices (`UserDirectory`) when
e-mail is configured.

- Flow (`AccountActivationService` in `Features/Auth/Activation`, slices `StartActivation`, `VerifyActivationCode`,
  `SetActivationPassword` at `/api/v1/auth/activation`, client `features/auth/pages/ActivationPage.tsx` at `/account/activate`):
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
  `NewsBodyFormat.Markdown` until edited. `NewsDesk.SyncImagesAsync` attaches the images an article uses and removes
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
- **Chats** (`Features/Chats` slices + `ChatRoom`, client `features/chats`): one-to-one (`Chat.DirectKey`, unique per pair) and group chats (owner
  removes people, anyone renames/adds/leaves; the last member leaving deletes the chat with its files). Every message
  takes the next `Chat.LastSequence` (xmin concurrency, retried), so numbers have no gaps: the client detects a gap in a
  live message and fetches `?after=`; members keep `LastReadSequence` (unread = newer text messages of others).
  Deleting works like Telegram: an own message for everyone (`DeleteChatMessage`, placeholder) or any message for oneself
  (`HideChatMessage` → `HiddenChatMessage`; the reader gets an empty `IsHidden` placeholder, so numbering keeps no gaps,
  and the client keeps it hidden in `mergeMessages`); `ClearChatHistory` sets `ChatMember.ClearedSequence` (older
  messages are not returned, a cleared one-to-one chat leaves the list until a newer message); `DeleteChat`
  removes a chat for everyone (either person of a one-to-one chat, the owner of a group). `ForwardChatMessages` copies messages
  into another chat as the user's new messages with `ForwardedFromId` (the first author) and copies their files (a
  forwarded message cannot be edited); the client picks the chat or a person in `ForwardDialog`.
  `ClientId` (unique per chat and author) makes a resend idempotent; the client shows the message at once as pending
  and offers "send again" with the same id. Bodies are sanitized by `IRichTextSanitizer.SanitizeMessage` (text
  formatting and links only) and `ToText` gives previews. Attachments (`FileRules.ChatFileTypes` allow-list, 10 × 25 MB,
  50 MB per message) go to `IFileStorage` (`chats/…`); photos become AVIF (`MediaPurpose.Chat`) with a thumbnail; all
  are served by `GET /api/v1/chats/{id}/attachments/{attachmentId}` to members only, as downloads. The client keeps
  each open conversation in the query cache (`threadQuery`, merged by `mergeMessages`), and `ChatSync` (mounted in the
  shell for users with `chats.use`) applies live events everywhere, including the unread badge of the menu.
- **Requests** (`Ticket`, `Features/Tickets` slices + `TicketDesk`, client `features/tickets`, routes `/requests`, `/requests/new`, `/requests/:id`,
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
  requests: `TicketMail` (a reaction to the domain event `TicketSubmitted`) writes to every `tickets.manage` holder
  except the author.
- **Notifications** (`Notification`, `Features/Notifications`, client `features/notifications`): other features never call
  them. They raise domain events and `Features/Notifications/Reactions` turns them into notifications through the
  internal `INotifier` (`NotifyAsync(draft, recipients)`: saves separately, skips the actor and inactive accounts, logs
  failures instead of throwing) after the request succeeded; `ItemSeen` (document/ticket opened, chat read to the end
  or gone) marks the user's notifications about it read. `Collect` folds chat
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
- Server translations: `src/ODC.Web/Shared/Localization/en/*.json` (flat `{ "uk": "en" }`, embedded automatically by
  `Directory.Build.props`; the file names `domain`, `application`, `infrastructure`, `web`, `common` follow the former
  projects — any of them works).
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
  `xmin` concurrency token; the `AddSignature` slice reloads on `DbUpdateConcurrencyException`, and a stale base is rejected
  ("sign the latest version again").
- 2FA is mandatory; TOTP codes are single-use; secrets are encrypted with Data Protection. Authentication is JWT access
  tokens + rotating refresh tokens (see "Identity tables and authentication"): only ES256 with server-fixed
  `typ`/`iss`/`aud`/`kid`/lifetime is accepted, tokens live in `__Host-` `HttpOnly` `Secure` `SameSite` cookies (never
  readable by scripts), refresh token reuse revokes the family, the security stamp stays on the server. Do not weaken
  rotation (RFC 9700 §2.2.2 MUST for browser-held refresh tokens). Re-issuing the tokens of the current browser:
  `SignInManager.RefreshSignInAsync(user)` (password, language, photo, sign-out elsewhere) — it keeps `auth_time`, so it
  never extends the absolute lifetime.
- State-changing API requests need the `X-CSRF-TOKEN` header (`AntiforgeryEndpointFilter` via `RequireAntiforgeryToken` on the `/api/v1` group; the token comes with the auth
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
  `[property: Required]`. `ApiContractTests` checks every contract.
- **API JSON**: enums are serialized as names and numbers strictly as numbers, for endpoints, SignalR and
  the OpenAPI document alike (`ConfigureJson` in `WebServiceCollectionExtensions`); otherwise the generated types say
  `number | string`. Minimal APIs bind (and document) every public property of `[AsParameters]`/`[FromForm]` types: a
  computed value is a method, an optional filter is nullable (a non-nullable value type would be required), and enum
  schemas stay free of `null` (`NonNullableEnumSchemaTransformer`).
- **React Compiler runs before the Lingui macro** (`vite.config.ts`): a static element inside `<Trans>` (e.g.
  `<strong>Microsoft Authenticator</strong>`) can be hoisted into a memoized value, so the runtime message id no longer
  matches the catalog and the page shows an id like `OFoRj4`. Keep `<Trans>` children dynamic or use a plain `t`.
- **Bash heredocs** with Ukrainian text and backticks break in this shell (a TypeScript template literal is executed as
  a command substitution): write edit scripts with the Write tool and run them with `node`.
- **Antiforgery tokens are bound to the `sub` claim and its `Issuer`.** Every identity of a signed-in user (validated
  JWT, the one issued during a refresh) must carry the token issuer (`urn:odc`), or a token issued in one request fails
  with 400 in the next. Testing token refresh: run the app with
  `--Authentication:Jwt:AccessTokenLifetime=00:00:10 --Authentication:Jwt:ClockSkew=00:00:01` after `--`.
- **One clock for tokens**: `JwtTokenService` issues with the injected `TimeProvider` and checks lifetimes with the same
  one (its own `LifetimeValidator`; the IdentityModel handler would read the system clock). Identity's lockout still
  counts real time.
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
