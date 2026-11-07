# CLAUDE.md — ODC.Infrastructure

Implementations of the application's ports: EF Core + PostgreSQL, ASP.NET Core Identity and the token services, account
activation, files and images, e-mail, spreadsheets, sanitizing, CMS signature inspection, audit. Registered by
`AddInfrastructure()` in `DependencyInjection.cs`.

## Layout

| Folder | What lives there |
| --- | --- |
| `Persistence/` | `ApplicationDbContext` (full `IdentityDbContext` with the domain types, `IApplicationDbContext`), `Configurations/` (one `IEntityTypeConfiguration` per aggregate), `Migrations/` (a single `InitialCreate`), `Seeding/` (`DatabaseInitializer`, `SeedOptions`), `AuditableEntityInterceptor`, `DocumentNumberGenerator` |
| `Identity/` | `AppSignInManager` (JWT scheme, blocked users out, 2FA always required), `AppUserClaimsPrincipalFactory`, `IdentityService`, `ProtectedUserStore` + `UserTokenProtector` (authenticator keys/recovery codes encrypted), `ReplayProtectedAuthenticatorTokenProvider`, password validators (length, Have I Been Pwned), `AppClaimTypes` |
| `Identity/Tokens/` | `JwtSigningKey` (ES256 PEM or generated into `App_Data/keys`, encrypted with Data Protection), `JwtTokenService`, `RefreshToken` + `RefreshTokenStore` + `RefreshTokenFamily` (rotation, reuse detection), `RefreshTokenCleanupService` (also deletes old activation codes) |
| `Identity/Activation/` | `AccountActivationService`, `ActivationChallenge` (table `activation_challenges`), `ActivationOptions` (`Authentication:Activation`), result records |
| `Email/` | `EmailOutbox` (bounded channel) → `SmtpEmailSender` (MailKit, batches, retries; writes `.eml` files when `Email:PickupDirectory` is set) |
| `Files/`, `Media/` | private file storage (`App_Data/storage`), libvips image processing → AVIF, media storage and cleanup |
| `Signatures/`, `Spreadsheets/`, `Text/`, `Auditing/` | CMS signature inspector, XLSX/CSV users, HTML sanitizer + Markdig, audit logger |

## Rules

- Schema: snake_case names, `citext` for case-insensitive text, PostgreSQL `xmin` as the concurrency token
  (`builder.Property<uint>("Version").IsRowVersion()`). Configure lengths and indexes in the configuration class, not
  with attributes on entities.
- **Migrations**: the history was squashed into one `InitialCreate` (2026-11-07). Add new migrations normally:
  `dotnet ef migrations add <Name> --project src/ODC.Infrastructure --startup-project src/ODC.Web --configuration Lead
  --output-dir Persistence/Migrations` with the environment variable `SkipClientBuild=true` (MSBuild reads it; `-p:` is
  not passed through `dotnet ef`). Use a non-Debug configuration — the user's app may lock `bin/Debug`.
- Secrets at rest go through Data Protection (application name `ODC`) with an `ODC.*` purpose string per use. Keys are stored in the database (`DataProtectionKeys`) and protected by DPAPI or a certificate — a
  different database means different keys.
- Identity changes go through `UserManager` (validators, security stamp). Never write `PasswordHash` directly; a new
  security stamp ends every refresh token family of the user.
- Activation: one row per code request for every valid address (enumeration-safe), code encrypted, compared in constant
  time, attempts counted with `xmin` retries; only the newest row of an address counts. See the root `CLAUDE.md`.
- E-mail never blocks a request: enqueue, the background sender delivers. Addresses are never logged.
- Use `[LoggerMessage]` partial methods for logging. Messages in `Texts.T` with English in
  `Localization/en/infrastructure.json`.
- Seeding creates the first administrator without a password (activation by e-mail at `Seed:AdminEmail`); demo staff
  (`Seed:DemoData` + `Seed:DemoPassword`) are created already activated for development.
