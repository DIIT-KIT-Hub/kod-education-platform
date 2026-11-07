# CLAUDE.md — ODC.Application

Use cases: one service per feature, DTOs, access rules and the abstractions the infrastructure implements. Depends only
on `ODC.Domain` (plus EF Core abstractions for `IApplicationDbContext` queries).

## Layout

| Folder | What lives there |
| --- | --- |
| `Abstractions/` | Ports: `IApplicationDbContext`, `ICurrentUser`, `IIdentityService`, `IAuditLogger`, `IEmailOutbox`, `IFileStorage`, `IMediaStorage`, `IImageProcessor`, `IRichTextSanitizer`, `ISignatureInspector`, … |
| `Common/Results` | `Result`, `Result<T>`, `Error` (`Validation`, `NotFound`, `Forbidden`, `Conflict`, `Unauthorized`; `ErrorType.TooManyRequests` → 429, `Unavailable` → 503) |
| `Common/Access` | `IPermissionResolver`, `IAccessScope` / `AccessScope`, `AdministrationScope`, `UnitHierarchy`, `OrganizationTree`, `PermissionHolders` |
| `Common/Email` | `EmailLayout` (inline-styled HTML + text twin, `InLanguage` for the recipient's language), `EmailContent` (optional big `Code`), `EmailOptions` (incl. `PickupDirectory`), `AccountEmails` (invitation, password reset, activation code) |
| `Common/…` | `Errors/DomainErrors`, `Files` (`FileRules`, uploads, downloads), `Paging`, `Models` (shared refs), `Text` (`EmailAddress`, `KyivTime`, `Slug`, `StoredText`) |
| `Features/<Feature>` | `I<Feature>Service` + implementation (internal), commands/queries/DTO records, feature mail (`TicketMail`) |
| `DependencyInjection.cs` | `AddApplication()`: every service is registered here explicitly |

## Rules

- Services are `internal sealed` with primary constructors, return `Result`/`Result<T>` (never throw for expected
  failures) and check permissions first (`currentUser.Has(Permissions.…)` → `Error.Forbidden()`). Data visibility goes
  through `IAccessScope` / `AdministrationScope` — administration is scoped to the organizations of the admin's
  positions; only `roles.manage` and `audit.view` are cabinet-wide.
- Wrap domain calls in `DomainErrors.GuardAsync` so a `DomainException` becomes a validation error.
- EF Core: filter before projecting into a record; .NET calls (`Texts.T`, `StoredText.Display`) only in the final
  projection. The InMemory test provider does not catch untranslatable queries — check new queries against PostgreSQL.
- Translate display-only names of seeded roles/unit types with `StoredText.Display`, never in edit models.
- Side effects after the save: notifications (`INotifier.NotifyAsync` after `SaveChangesAsync`, it never throws), e-mail
  (`IEmailOutbox.Enqueue` — queued, sent in the background; check `outbox.IsEnabled`), audit (`IAuditLogger`).
- E-mails: build them with `EmailLayout.InLanguage(recipient.Language, () => EmailLayout.Render(…))` so texts follow the
  recipient; links use `EmailOptions.SiteUrl`. Never put secrets or one-time codes in the subject.
- Accounts never get a password here: `IIdentityService.CreateAsync(user)` creates it without one and
  `RemovePasswordAsync` resets it; the owner sets the password through the activation (root `CLAUDE.md`).
- Messages: Ukrainian `Texts.T("…")` with English in `Localization/en/application.json`.
- A new service: interface + implementation in its feature folder, register it in `DependencyInjection.cs`, expose it
  through a controller in `ODC.Web`, add tests (usually on the `TwoOrganizations` fixture).
