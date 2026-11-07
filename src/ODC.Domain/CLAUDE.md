# CLAUDE.md — ODC.Domain

The core of the cabinet: entities with their invariants, the permission catalog and the localization catalog. It
references nothing but ASP.NET Core Identity's base types (`IdentityUser`, `IdentityRole`, … are the domain's own
types, see the root `CLAUDE.md`, "Identity tables"). No EF Core, no web, no I/O.

## Layout

| Folder | What lives there |
| --- | --- |
| `Access/` | `Role : IdentityRole`, `RolePermission`, `UserPermissionOverride`, `Permissions` (the catalog: one nested class per module, `All`, `Codes`), `PermissionDefinition`, `SystemRoles`/`SystemRoleDefinition`, `PermissionClaimTypes` |
| `Users/` | `User : IdentityUser<Guid>` (names, `IsActive`, `AwaitsActivation` = no password yet, language, photo, positions, overrides), `StaffPosition : IdentityUserRole` (user + role + unit) |
| `Organization/` | `OrganizationUnit` (tree, code, type), `UnitType` (catalog with Lucide icon names in `UnitType.Icons`) |
| `Documents/` | `Document` with signers, signatures and the signing turn, `SignatureChain` (cumulative signing rules) |
| `News/`, `Chats/`, `Tickets/`, `Notifications/`, `Media/`, `Audit/` | the aggregates of those features; `AuditActions` holds every audit event name |
| `Common/` | `Entity` (Guid v7 `Id`), `AuditableEntity`/`IAuditable` (`CreatedAt`/`UpdatedAt` set by an interceptor), `Guard`, `DomainException` |
| `Localization/` | `Texts` (`T`, `In`, catalogs embedded from every `ODC.*` assembly), `Languages`, `en/*.json` |

## Rules

- Invariants live in the entity: private setters, state changes through methods (`User.SetPositions`,
  `Document.AddSignature`, `Chat.Append…`). Throw `DomainException` with a translated message for a broken rule; the
  application layer turns it into a validation error (`DomainErrors.GuardAsync`).
- Validate text input with `Guard.Text(value, max, "Назва")` / `Guard.OptionalText` — the field name is translated, so
  add it to `Localization/en/*.json`.
- Every message is Ukrainian through `Texts.T("…")` with a literal key (no interpolation inside `T`); add the English
  text to `Localization/en/domain.json` (or `common.json` for shared words). `LocalizationTests` fails otherwise.
- EF Core needs a private parameterless constructor; collections are private `List<T>` fields exposed as
  `IReadOnlyCollection<T>`. Mapping (tables, lengths, indexes, concurrency) is in `ODC.Infrastructure`, not here.
- A new permission: add it to the module class in `Permissions` (code `module.action`, Ukrainian title/description as
  `Source*`, translated properties), then update seeding of system roles if a role should get it, the client
  (`shared/session` permission names, `moduleNames`) and both manual chapters.
- A new audit event: a constant in `AuditActions` and its name in the client (`auditActionNames` in
  `shared/i18n/display.ts`, context `audit`).
- Computed properties without a setter (`FullName`, `AwaitsActivation`) are not mapped; filter on the stored columns
  in queries (`u.PasswordHash == null`), not on them.
