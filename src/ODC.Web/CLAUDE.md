# CLAUDE.md — ODC.Web

The whole server in one project, organized as **vertical slices**: every use case is one file with its request, its
handler and its minimal API endpoint. REST API under `/api/v1`, the SignalR hub, JWT authentication, security
middleware, images, the signing proxy and trust list, and hosting of the React client (`src/ODC.Client`) at `/`.
No server-rendered pages. Domain, Application and Infrastructure used to be separate projects (Clean Architecture);
their code now lives in the folders below and their old guides are in the git history.

## Layout

| Folder | What lives there |
| --- | --- |
| `Features/<Area>/` | The slices of a feature (`Documents/CreateDocument.cs`, `Chats/PostChatMessage.cs`, …), its contracts (request/response records with the OpenAPI schema names the client uses), a feature helper ("desk": `DocumentDesk`, `TicketDesk`, `ChatRoom`, `NewsDesk`, `UserDirectory`, …), `Domain/` (the feature's own entities and domain events) and `Persistence/` (their EF Core configurations) |
| `Features/Notifications/Reactions/` | Domain event handlers that turn the events of documents, requests, chats and news into notifications |
| `Features/DependencyInjection.cs` | `AddApplication()`: `AddMessaging(assembly)` plus the desks and shared services |
| `Shared/Kernel` | `Entity` (Guid v7 `Id`, `Raise` / `TakeDomainEvents`), `AuditableEntity`, `Guard`, `DomainException`, `DomainErrors.GuardAsync`, `Result`/`Result<T>`/`Error`/`IOutcome`, `IDomainEvent`, `ItemSeen` |
| `Shared/Domain/` | Entities several features share: `Users` (`User : IdentityUser`, `Member` — the organization's copy with `StaffPosition`s), `Access` (`Role`, permission catalog `Permissions`, `SystemRoles`), `Organization` (`OrganizationUnit`, `UnitType`), `Media`, `Audit` (`AuditEntry`, `AuditActions`), `Reactions` (`ReactionKind`) |
| `Shared/…` | Cross-feature services and ports: `Access` (`IAccessScope`, `AdministrationScope`, `PermissionResolver`, `PermissionHolders`, `Catalog`, `PositionDirectory`), `Identity` (`ICurrentUser`, `IIdentityService`), `Email` (`EmailLayout`, `AccountEmails`, `IEmailOutbox`), `Files` (`FileRules`, `AttachmentStore`), `Media` (`MediaLibrary`), `Text`, `Paging`, `Models`, `Localization` (`Texts`, `Languages`, `en/*.json`) |
| `Messaging/` | Our own mediator (no MediatR): `IRequest<T>`/`ICommand<T>`/`IQuery<T>`, `IRequestHandler<,>`, the decorators, domain event delivery, `AddMessaging` |
| `Endpoints/` | `IEndpoint`, `ApiEndpoints.MapApiEndpoints` (the secure `/api/v1` group), `RequirePermission`, `RequireAntiforgeryToken`, `ResultHttpExtensions` (`ToOk`, `ToCreated`, `ToNoContent`, `ToFile`, `ToProblem`), `NonNullableEnumSchemaTransformer` |
| `Infrastructure/` | Implementations: `Persistence` (`MasterDbContext`, `OrganizationDbContext`, `DataPlacement`, `MemberSync`, shared configurations, one migration per database, seeding), `Identity` (sign-in manager, claims, JWT + refresh tokens in `Tokens/`, authentication handlers in `Authentication/`), `Email` (outbox → SMTP / pickup folder), `Files`, `Media` (libvips → AVIF, cleanup), `Signatures`, `Text`, `Auditing`; `AddInfrastructure()` |
| `Security/` | `SecurityHeadersMiddleware` (strict CSP), `AccountSetupMiddleware` (photo required), permission policies (`RequirePermissionAttribute`), `CurrentUser` |
| `Hosting/` | `WebServiceCollectionExtensions` (JSON, OpenAPI, validation, rate limits, auth, SignalR), `WebApplicationExtensions` (pipeline order and mapping), `SpaHostingExtensions`, `ApiExceptionHandler`, logging |
| `Realtime/` | `LiveHub` at `/api/v1/live` and the event senders (chats, news discussion, notifications) |
| `wwwroot/lib/eusign` | the vendored IIT signing library only (16 MB, do not edit) |

## A slice

```csharp
public static class RenameChat                                   // Features/Chats/RenameChat.cs
{
    public sealed record Command(Guid ChatId, string? Title) : ICommand<Result>;  // the request (public)

    internal sealed class Handler(ChatRoom room)                 // the use case: rules, EF Core, Result
        : IRequestHandler<Command, Result> { … }

    internal sealed class Endpoint : IEndpoint                    // found by MapApiEndpoints
    {
        public void Map(IEndpointRouteBuilder api) => ChatRoom.MapChats(api).MapPut("{id:guid}/title", HandleAsync);

        private static async Task<Results<NoContent, ProblemHttpResult>> HandleAsync(
            Guid id, ChatTitleRequest body, IRequestHandler<Command, Result> handler, CancellationToken ct)
            => (await handler.HandleAsync(new Command(id, body.Title), ct)).ToNoContent();
    }
}
```

- Endpoints inject the handler interface directly (no dispatcher, no `ISender`): the dependency is visible and checked
  at startup. A handler may call another slice's public request the same way (`GetDashboard`, `LiveHub`).
- The interface resolves to `Logging(Authorization(DomainEvents(handler)))`:
  - `LoggingDecorator` logs the request name, the time and the error code of a failed result — never the request
    itself (it may hold passwords, codes or personal data);
  - `AuthorizationDecorator` checks `[RequirePermission(…)]` on the request type (defence in depth behind the
    endpoint's `RequirePermission`; object-level checks stay in the handler);
  - `DomainEventsDecorator` delivers domain events after the handler succeeded (below).
- Contracts: records; validation attributes on the primary constructor parameters (`[Required] string Email`), never
  `[property: …]`. Built-in minimal API validation (`AddValidation`) answers 400 problem details. Optional query values
  are nullable (a non-nullable value type in `[AsParameters]` becomes required) — `PageRequest` normalizes them in
  `CurrentPage`/`CurrentPageSize`. Every public property of an `[AsParameters]`/`[FromForm]` type is bound and shows
  up in the OpenAPI document, so computed values are methods (`PhotoCropForm.ToCrop()`). Files are separate
  `IFormFile`/`IFormFileCollection?` parameters; `RequestSizeLimitAttribute` goes through `WithMetadata`.
- Results: return `Result`/`Result<T>` from handlers (never throw for expected failures) and turn them into HTTP with
  `ToOk`/`ToCreated`/`ToNoContent`/`ToProblem`. Problem details carry `code` (= error code); the client switches on it,
  keep codes stable. `ErrorType.TooManyRequests` → 429, `Unavailable` → 503. A `DomainException` becomes a validation
  error through `DomainErrors.GuardAsync`.
- Security is on by default: the `/api/v1` group requires a signed-in user and the `X-CSRF-TOKEN` header for every
  state-changing method (`RequireAntiforgeryToken`); anonymous endpoints opt out with `AllowAnonymous()` and must be on
  the list in `EndpointSecurityTests`. Permission checks: `.RequirePermission(Permissions.…)` on the endpoint (or its
  group) plus `[RequirePermission]` on the request. Data visibility goes through `IAccessScope` /
  `AdministrationScope` — administration is scoped to the organizations of the admin's positions; only
  `roles.manage` and `audit.view` are cabinet-wide.
- Desks are `internal sealed` helpers shared by the slices of **one** feature (loading with access checks, mapping,
  file handling). Another feature never takes them; use the feature's public request or a domain event instead.

## Domain and domain events

- Entities keep their invariants: private setters, state changes through methods (`Member.SetPositions`,
  `Document.AddSignature`, `Ticket.Comment`), `DomainException` with a translated message for a broken rule. EF Core
  needs a private parameterless constructor; collections are private `List<T>` fields exposed read-only. Mapping
  (tables, lengths, indexes, `xmin`) lives in `Persistence/` configurations, never as attributes on entities.
  `Domain/` folders reference neither EF Core nor HTTP nor the pipeline (checked by `SliceArchitectureTests`); ASP.NET
  Core Identity base types are the domain's own (`User : IdentityUser<Guid>`).
- A feature owns its entities (`Features/Documents/Domain`); entities used by several features live in
  `Shared/Domain`. The contexts are the units of work (no repositories): slices query `OrganizationDbContext` for the
  organization's data and `MasterDbContext` for accounts, catalogs and requests (root `CLAUDE.md`, "Databases"); a
  query never joins the two. Role names, unit types and people's positions on the organization's side come from
  `Catalog` and `PositionDirectory`.
- **Domain events** decouple features: an entity raises one (`Raise(new TicketSubmitted(…))`) or a handler raises one
  through the scoped `DomainEvents` (`domainEvents.Raise(new ItemSeen(me, id))` for "the user saw it", or a batch of
  chat messages). `DomainEventsDecorator` takes them from the tracked entities and `DomainEvents` **after the handler
  returned a successful result** (its changes are saved) and runs every `IDomainEventHandler<T>` in the same scope;
  a failed result or an exception drops them. A failing reaction is logged and never fails the request. Reactions may
  raise further events (a few rounds at most). Events are in-process and not stored: no outbox — a crash between the
  save and the reaction loses a notification, which is acceptable here.
- Current events: `DocumentSigningProgressed`, `TicketSubmitted`/`TicketCommented`/`TicketStatusChanged`/
  `TicketAssigned`, `ChatMessagesPosted`, `NewsCommentPosted`, `ItemSeen`. Reactions: `Features/Notifications/
  Reactions/*` (notifications) and `TicketMail` (e-mail to the handlers of requests). No feature references
  `Features.Notifications`.
- Allowed edges between features are listed in `SliceArchitectureTests.AllowedEdges`; a new one is a design decision.

## Persistence

- Schema: snake_case names, `citext` for case-insensitive text, PostgreSQL `xmin` as the concurrency token. Two
  databases, one migration each (`InitialCreate`, 2026-11-09). A new entity goes into `DataPlacement` and gets its own
  configuration class (a class configures entities of one database only). Add migrations per context:
  `dotnet ef migrations add <Name> --project src/ODC.Web --configuration Lead --context MasterDbContext --output-dir Infrastructure/Persistence/Migrations/Master`
  (and the same with `OrganizationDbContext` / `…/Organization`) with the environment variable `SkipClientBuild=true`
  (`-p:` is not passed through `dotnet ef`). Check `dotnet ef migrations has-pending-model-changes … --context <X>` for
  both after moving entities: the snapshot names entity types by their full CLR name.
- EF Core: filter before projecting into a record; .NET calls (`Texts.T`, `StoredText.Display`) only in the final
  projection. The InMemory test provider does not catch untranslatable queries — check them against PostgreSQL.
- Secrets at rest go through Data Protection (application `ODC`, purposes `ODC.*`); keys live in the database and are
  protected by DPAPI or a certificate. Identity changes go through `UserManager` (validators, security stamp); never
  write `PasswordHash`. Accounts are created without a password (`IIdentityService.CreateAsync`), resets remove it.
- Side effects after the save: domain events (notifications, mail), `IEmailOutbox.Enqueue` (queued, sent in the
  background; check `outbox.IsEnabled`), `IAuditLogger`. E-mails: `EmailLayout.InLanguage(recipient.Language, …)` so
  texts follow the recipient; never secrets or codes in the subject; addresses are never logged.

## Other rules

- XML documentation on every type and member (it becomes the OpenAPI document the client types are generated from).
  After changing an endpoint or a contract: build, run the app, regenerate `schema.d.ts` (`pnpm api:types`, or
  `openapi-typescript <file> --root-types --root-types-no-schema-prefix`) and make the client typecheck.
- Rate limits are in `AddRateLimiting()`: POSTs to `/api/v1/auth` and `/api/v1/profile` share a per-IP window; named
  policies (`activation`, `eusign-proxy`) are applied with `RequireRateLimiting`.
- Cookies: `__Host-` prefix, `Secure`, `HttpOnly`, `Path=/`; token cookies are written only by the JWT handler / sign-in
  manager, the activation cookie by `ActivationApi`.
- Messages: Ukrainian `Texts.T("…")` with a literal key; English in `Shared/Localization/en/*.json` (the file names
  still follow the former projects: `domain`, `application`, `infrastructure`, `web`, `common`). Guard field names
  (`Guard.Text(value, max, "Назва")`) are translated too. A new permission: `Permissions` module class, system role
  seeding, the client (`shared/session`, `moduleNames`) and the manual. A new audit event: `AuditActions` + the client
  (`auditActionNames`).
- Logging: `[LoggerMessage]` partial methods.
- `UserSecretsId` is `odc-web-3b8f9c52-…`; the user's secrets live under that id (changing it loses them).
