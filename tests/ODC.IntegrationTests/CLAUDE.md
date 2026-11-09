# CLAUDE.md — ODC.IntegrationTests

The whole cabinet in memory (`WebApplicationFactory<Program>`: the real `Program`, migrations, seeding, middleware,
endpoints, background services) on a throwaway PostgreSQL database, driven through HTTP like the React client: cookies,
the antiforgery header, the password, the authenticator, the photo. xUnit v3 on Microsoft Testing Platform; ~22 tests,
~65 s (most of it starting the host and hashing the demo passwords once).

```bash
dotnet build tests/ODC.IntegrationTests/ODC.IntegrationTests.csproj -c Lead -v q -nologo -p:SkipClientBuild=true
dotnet test tests/ODC.IntegrationTests -c Lead --no-build
# One class (the full name):
dotnet test tests/ODC.IntegrationTests -c Lead --no-build -- --filter-class ODC.IntegrationTests.Documents.DocumentTests
```

As with the unit tests, a failed build keeps the old DLL and `--no-build` runs the old tests: check the build output.

## The host (`Hosting/OdcApplication`)

- Databases: `odc_it_<guid>` (master) and `odc_it_<guid>_org` (organizations) on the server of `ODC_TEST_DATABASE` (a connection string; CI) or the user secrets of
  ODC.Web (`ConnectionStrings:Database`); only the database name is replaced. Without either every test skips. Both
  databases are dropped at the end (`WITH (FORCE)`).
- Everything else lives in a temporary folder: its own P-256 JWT key (never `App_Data/keys`, shared with the running
  app), storage, logs, the trust list cache, an empty client (`Spa:Root`) and the e-mail pickup folder.
- Settings: environment Development (Data Protection keys may stay unencrypted off Windows), `Seed:DemoData=true` with a
  random `Seed:DemoPassword`, no breached-password check, no trust list download, no activation padding. Nothing calls
  the internet.
- One host and one database for all tests (`[Collection(IntegrationCollection.Name)]`, classes run one after another).
  Tests create what they look at and never rely on totals or on a clean database.
- `TestClock` replaces `TimeProvider`: real time plus an offset that `Advance` moves forward (token lifetimes, idle
  limits, authenticator steps). ASP.NET Core Identity's lockout counts real time, so a lockout cannot be waited out.
- `ClientAddressStartupFilter` gives every client its own remote address (header `X-Test-Client-Address`), so the
  per-address rate limits of the sign-in and activation endpoints count each client separately.

## Clients (`Clients/`)

- `app.CreateApiClient()`: a cookie-keeping client on `https://localhost`. `ApiClient` sends `X-CSRF-TOKEN` with every
  state-changing request; call `RefreshContextAsync()` after an identity change (the token is bound to the identity —
  the helpers below do it). `GetAsync<T>`/`PostAsync<T>` fail the test with the problem details on an unexpected status;
  `SendFormAsync` posts multipart forms (`UploadedFile`); `ProblemCodeAsync` reads `code`.
- `app.Accounts`: `SignInAsync(email)` (demo password) signs in through `/auth/login`, enrolls the authenticator at the
  first sign-in or enters its next code, and uploads a photo when the account has none. `AdminAsync()` activates
  `admin@odc.local` by e-mail once per run, then signs in. `ActivateAsync(email, password, withPhoto)` is the activation
  page. Codes are single-use: `NextCode` moves the clock to the next 30-second step instead of reusing one;
  `LastCode` gives a used one (replay tests).
- `Demo`: the demo accounts (`DatabaseInitializer.DemoStaff`) — first organization `ORG` (director, deputy, engineer,
  clerk, HR, heads of IT and finance, press), second organization `FUND` (head, HR, project coordinator).
  `Demo.LockedOut` is locked by the sign-in tests: no other test may use it.
- `People`: `CreateUserAsync(admin, email, unitCode, roleCode)` (the administrator's form; an invitation e-mail follows —
  take it with `Mailbox.WaitForAsync` before activating, or the activation reads the invitation instead of the code) and
  `FindAsync(client, email)` (the user list the client may see).
- `Mailbox`: the pickup folder. `WaitForAsync(address)` waits up to 15 s for the next e-mail to an address and hands
  each file out once; `WaitForCodeAsync` takes the six-digit code; `HasMailTo` checks that nothing was sent.
- Request and response types are the server's own contracts (`LoginRequest`, `DocumentDetails`, …), so a contract change
  breaks the build here.

## Layout

| Folder | What is tested |
| --- | --- |
| `Auth/` | sign-in (enrolment, codes, single use, same answer for wrong password and unknown address, lockout, the photo requirement, anonymous requests), activation (the first administrator, wrong codes, no enumeration), session (access token renewed by the refresh cookie, idle limit, sign-out, antiforgery) |
| `Admin/` | invitation → activation → work; an HR specialist sees and manages only the people of their organization |
| `Documents/` | registration with a file and a route, the signer's list, the file, hidden from another organization, rejection, a signer from another organization, refused file types |
| `Chats/` | a one-to-one chat across organizations and its privacy, idempotent resend |
| `Tickets/` | a request answered by support, seen only by the author and support |

These pin today's behaviour before the move to a database per organization (the cross-organization signer and chat
tests in particular must keep passing).

## Rules

- Same as the unit tests: `uk-UA` (`TestCulture`), `TestContext.Current.CancellationToken`, StyleCop, XML docs on
  classes and helpers.
- Dispose every `ApiClient` (`using var`).
- Prefer the API over the database. When a test must look into the database, use `app.InScopeAsync(...)`.
- A new demo account a test changes for good (locks, blocks, resets) is reserved like `Demo.LockedOut`; better, create a
  fresh account with `People.CreateUserAsync`.
