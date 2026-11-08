# CLAUDE.md — ODC.UnitTests

xUnit v3 on Microsoft Testing Platform. ~180 tests, ~30 s:

```bash
dotnet build tests/ODC.UnitTests/ODC.UnitTests.csproj -c Lead -v q -nologo -p:SkipClientBuild=true
dotnet test tests/ODC.UnitTests -c Lead --no-build
```

## Layout

| Folder | What is tested |
| --- | --- |
| `Domain/` | entity invariants (users, permissions, documents and the signature chain) |
| `Application/` | slices on the `TwoOrganizations` fixture (EF Core InMemory with two organizations, roles, staff and fakes for identity, spreadsheets, audit, e-mail): scoped administration, chats, tickets, notifications, news discussion. `As(user)` builds a real container (`AddApplication()` + fakes), so `SendAsync(new X.Command(…))` runs the whole pipeline: decorators, permission checks and domain events. `ChatClient` adapts the chat slices to short helper calls |
| `Messaging/` | the pipeline itself: domain events delivered once and only after success, failures isolated, reactions raising events |
| `Architecture/` | `SliceArchitectureTests` (allowed edges between features, no injection of another feature's internals, handlers/endpoints internal and sealed, every request handled and registered, `Domain/` free of EF Core/HTTP/pipeline) and `EndpointSecurityTests` (the real host's endpoints: anonymous only on the list, antiforgery on every state-changing method) |
| `Infrastructure/` | JWT tokens and refresh rotation, account activation (`AccountActivationTests`: a real `UserManager` on InMemory, a manual clock, recorded e-mails), TOTP/token protection, sanitizer, images, CMS signatures |
| `Web/` | API contracts, localization coverage (every `T`/`In`/`ErrorMessage`/Guard literal has English), trust list |

## Rules

- Tests run in `uk-UA` (`TestCulture` module initializer); assert Ukrainian messages or switch the culture explicitly.
- Pass `TestContext.Current.CancellationToken` to every method that takes a token (analyzer xUnit1051 is an error).
- StyleCop applies here too: document classes and helpers (test methods themselves need no XML docs).
- The InMemory provider ignores transactions, `ExecuteDelete` and untranslatable queries — verify such code against
  PostgreSQL in the running app.
- When an entity passed in from one scope is changed in another, load it again in that scope (EF tracking conflicts).
- `LocalizationTests` and `SliceArchitectureTests` find the repository root by `OrganizationalDocumentsCabinet.slnx` and
  scan `src/ODC.Web` sources.
- A handler with a new dependency: register it (or a fake) in `TwoOrganizations.As(...)`; the container fails to resolve
  the handler otherwise.
- A new anonymous endpoint or a state-changing one without the antiforgery token must be added to the lists in
  `EndpointSecurityTests` on purpose; a new dependency between features to `SliceArchitectureTests.AllowedEdges`.
