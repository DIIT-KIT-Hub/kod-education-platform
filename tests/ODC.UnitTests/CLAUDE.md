# CLAUDE.md — ODC.UnitTests

xUnit v3 on Microsoft Testing Platform. ~500 tests, ~75 s (about half of it is the PostgreSQL document tests):

```bash
dotnet build tests/ODC.UnitTests/ODC.UnitTests.csproj -c Lead -v q -nologo -p:SkipClientBuild=true
dotnet test tests/ODC.UnitTests -c Lead --no-build
# One class (the full name; a bare class name runs nothing):
dotnet test tests/ODC.UnitTests -c Lead --no-build -- --filter-class ODC.UnitTests.Infrastructure.EmailTests
# Coverage (Microsoft.Testing.Extensions.CodeCoverage):
dotnet test tests/ODC.UnitTests -c Lead --no-build -- --coverage --coverage-output-format cobertura
```

A build that fails keeps the old DLL, and `dotnet test --no-build` then quietly runs the old tests: check the build
output for `error` (analyzer ids such as `xUnit2017` are lower-case and easy to miss in a filtered log).

## Layout

| Folder | What is tested |
| --- | --- |
| `Domain/` | entity invariants (users, permissions, documents and the signature chain) |
| `Application/` | slices on the `TwoOrganizations` fixture (two EF Core InMemory databases as in production: `Master` with accounts, roles and unit types, `Db` with two organizations and their members; `MemberSync` copies account changes into `Db` — save `Db` before `Master` when both are new; change accounts and roles through `Master`), roles, staff and fakes for identity, spreadsheets, audit, e-mail): scoped administration, user security and export (`Admin/`), roles and audit, unit types and the unit tree (`Structure/`), profile, account settings (`Auth/`), chats, tickets, notifications, news discussion, signing journal and media. `As(user)` builds a real container (`AddApplication()` + fakes), so `SendAsync(new X.Command(…))` runs the whole pipeline: decorators, permission checks and domain events. `ChatClient` adapts the chat slices to short helper calls. Unlike production, both organizations share `Db`: the request works in organization A (`OrganizationContext`), `FakeProvisioning` adds a top-level unit for a new organization, `ResolvedPermissions` answers `IAccountPermissions` from `Db` and `SingleOrganization` runs the work across organizations once on `Db` (the registry has rows for A and B) |
| `Application/Documents/` | the document slices on **PostgreSQL** (`DocumentWorld` on top of `TwoOrganizations`, a manual clock, a scripted signature inspector): lifecycle, signing chains and turns, access |
| `Messaging/` | the pipeline itself: domain events delivered once and only after success, failures isolated, reactions raising events |
| `Architecture/` | `DataPlacementTests` (every entity placed in one database, each model holds exactly its entities, no foreign key or table shared across the databases), `SliceArchitectureTests` (allowed edges between features, no injection of another feature's internals, handlers/endpoints internal and sealed, every request handled and registered, `Domain/` free of EF Core/HTTP/pipeline) and `EndpointSecurityTests` (the real host's endpoints: anonymous only on the list, antiforgery on every state-changing method) |
| `Infrastructure/` | JWT tokens and refresh rotation, account activation (`AccountActivationTests`), `IdentityServiceTests` (a real `UserManager` configured like the cabinet: password rules, lockout, replay-protected TOTP, encrypted recovery codes, the breached password check over a fake HTTP handler, the claims factory), file and image storage on a temporary folder (`TemporaryFolder`: a folder per organization, the encrypted format, another organization's key, ranges, a cut file), the e-mail outbox and pickup sender, the audit writer, TOTP/token protection, sanitizer, images, CMS signatures; `PostgresDatabase` / `PostgresCollection` |
| `Web/` | API contracts, localization coverage (every `T`/`In`/`ErrorMessage`/Guard literal has English), trust list, `Security/` (headers and CSP, the photo requirement, problem details, permission policies, the current user), endpoint helpers (results → HTTP, antiforgery filter, form files, enum schemas), the SignalR hub and live senders (fake hub context and clients), shared rules (file allow-lists, e-mail addresses, account e-mails, language) |

## PostgreSQL tests

- `PostgresDatabase` is an xUnit collection fixture (`[Collection(PostgresCollection.Name)]`): it creates
  `odc_tests_<guid>` and `odc_tests_<guid>_org` from the two models once (`EnsureCreated`; `MasterOptions`,
  `OrganizationOptions`), each test empties the tables of both (`Reset()`: `DELETE` in
  dependants-first order, much faster than `TRUNCATE`), and it drops both databases at the end (`WITH (FORCE)`).
- The server comes from `ODC_TEST_DATABASE` (a connection string, for CI) or the user secrets of ODC.Web
  (`ConnectionStrings:Database`); only the database names are replaced, so the developer's data is never touched.
  Without either the tests are skipped. `synchronous_commit=off` keeps them fast (~2 s per test).
- Use PostgreSQL when the code needs the real provider: complex properties (`Document.File`: InMemory cannot load them),
  `xmin` concurrency, `ExecuteDelete`, SQL translation.

## Helpers

- `TwoOrganizations.As(user, configure)`: the `configure` hook replaces services, e.g. `ScriptedIdentity.Register`
  (records calls like `"ResetAuthenticator a@b"`, `Outcome` to fail them, `ValidCode` "123456") and
  `RecordingAudit.Register` (`Entries`, `Actions()`). `Written` holds what the fake spreadsheet wrote last.
- `As(user)` resolves the user's permissions with `Task.Run` around a synchronous wait: xUnit's synchronization
  context deadlocks a plain `.GetResult()` on EF Core async calls.
- Fire-and-forget senders (`LiveChatEvents`, …) complete synchronously with fake clients that return completed tasks,
  so a test can assert right after the call.

## Rules

- Tests run in `uk-UA` (`TestCulture` module initializer); assert Ukrainian messages or switch the culture explicitly.
- Pass `TestContext.Current.CancellationToken` to every method that takes a token (analyzer xUnit1051 is an error).
- StyleCop applies here too: document classes and helpers (test methods themselves need no XML docs); member order
  (SA1201/SA1202/SA1204: fields, properties, public test methods, then private static and private helpers).
- The InMemory provider ignores transactions, `ExecuteDelete` and untranslatable queries — use the PostgreSQL fixture
  or verify against PostgreSQL in the running app.
- When an entity passed in from one scope is changed in another, load it again in that scope (EF tracking conflicts).
- `LocalizationTests` and `SliceArchitectureTests` find the repository root by `OrganizationalDocumentsCabinet.slnx` and
  scan `src/ODC.Web` sources.
- A handler with a new dependency: register it (or a fake) in `TwoOrganizations.As(...)`; the container fails to resolve
  the handler otherwise.
- A new anonymous endpoint or a state-changing one without the antiforgery token must be added to the lists in
  `EndpointSecurityTests` on purpose; a new dependency between features to `SliceArchitectureTests.AllowedEdges`.
- Sign-in, 2FA, sign-out and the activation endpoints (cookies, the sign-in manager) are covered by
  `tests/ODC.IntegrationTests` (the real host over HTTP).
