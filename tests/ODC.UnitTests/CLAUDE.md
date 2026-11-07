# CLAUDE.md — ODC.UnitTests

xUnit v3 on Microsoft Testing Platform. ~160 tests, ~30 s:

```bash
dotnet build tests/ODC.UnitTests/ODC.UnitTests.csproj -c Lead -v q -nologo -p:SkipClientBuild=true
dotnet test tests/ODC.UnitTests -c Lead --no-build
```

## Layout

| Folder | What is tested |
| --- | --- |
| `Domain/` | entity invariants (users, permissions, documents and the signature chain) |
| `Application/` | services on the `TwoOrganizations` fixture (EF Core InMemory with two organizations, roles, staff and fakes for identity, spreadsheets, audit, e-mail): scoped administration, chats, tickets, notifications, news discussion |
| `Infrastructure/` | JWT tokens and refresh rotation, account activation (`AccountActivationTests`: a real `UserManager` on InMemory, a manual clock, recorded e-mails), TOTP/token protection, sanitizer, images, CMS signatures |
| `Web/` | API contracts, localization coverage (every `T`/`In`/`ErrorMessage`/Guard literal has English), trust list |

## Rules

- Tests run in `uk-UA` (`TestCulture` module initializer); assert Ukrainian messages or switch the culture explicitly.
- Pass `TestContext.Current.CancellationToken` to every method that takes a token (analyzer xUnit1051 is an error).
- StyleCop applies here too: document classes and helpers (test methods themselves need no XML docs).
- The InMemory provider ignores transactions, `ExecuteDelete` and untranslatable queries — verify such code against
  PostgreSQL in the running app.
- When an entity passed in from one scope is changed in another, load it again in that scope (EF tracking conflicts).
- `LocalizationTests` finds the repository root by `OrganizationalDocumentsCabinet.slnx` and scans `src/ODC.*` sources.
- New service constructor parameters: update `TwoOrganizations.As(...)` (e.g. `Emails` and `EmailOptions` for
  `UserService`).
