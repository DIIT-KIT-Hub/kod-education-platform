# CLAUDE.md — ODC.Web

The host: REST API under `/api/v1`, the SignalR hub, JWT authentication handlers, security middleware, images, the
signing proxy and trust list, and hosting of the React client (`src/ODC.Client`) at `/`. No server-rendered pages.

## Layout

| Folder | What lives there |
| --- | --- |
| `Api/` | Controllers (`ApiControllerBase`: `FromResult`, `FromFile`, `ProblemFor`), `ApiAntiforgeryFilter`; `AuthController` (sign-in, 2FA, enrolment, password, language), `AccountActivationController` (`/api/v1/auth/activation`), one controller per resource |
| `Api/Contracts/` | Request/response records of the controllers that are not application DTOs |
| `Identity/` | `JwtAuthenticationHandler` (access cookie, refresh fallback), `TwoFactorJwtHandler`, `AuthenticatorQrCode` |
| `Security/` | `SecurityHeadersMiddleware` (strict CSP), `AccountSetupMiddleware` (photo required), permission policies (`RequirePermission`), `CurrentUser`, `ClaimsPrincipalExtensions` |
| `Hosting/` | `WebServiceCollectionExtensions` (JSON conventions, rate limits, auth, SignalR), `WebApplicationExtensions` (pipeline order), `SpaHostingExtensions`, `ApiExceptionHandler`, logging |
| `Realtime/` | `LiveHub` at `/api/v1/live` and the event senders (chats, news discussion, notifications) |
| `Signing/`, `Media/`, `Localization/` | EUSign proxy + trust list, `/media/{id}` images, culture provider and `en/web.json` |
| `wwwroot/lib/eusign` | the vendored IIT signing library only (16 MB, do not edit) |

## Rules

- A controller derives from `ApiControllerBase`, takes services by primary constructor, returns `ActionResult<T>` from
  a `Result` (`FromResult`) and documents every action with XML comments — they become the OpenAPI document the client
  types are generated from (`pnpm api:types`).
- Contracts are records; validation attributes go on the primary constructor parameters (`[Required] string Email`),
  never `[property: …]` (MVC throws 500). `ApiContractTests` checks every contract. Enums are serialized as names.
- Every state-changing request needs `X-CSRF-TOKEN` (the antiforgery filter); anonymous endpoints are explicit
  `[AllowAnonymous]`. Permission checks: `[RequirePermission(Permissions.…)]` on top of the service's own checks.
- Errors are problem details with `code` (`ProblemFor(error)`); the client switches on these codes, so keep them stable.
  `ErrorType.TooManyRequests` → 429, `Unavailable` → 503.
- Rate limits are in `AddRateLimiting()`: POSTs to `/api/v1/auth` and `/api/v1/profile` share a per-IP window; named
  policies (`activation`, `eusign-proxy`) are applied with `[EnableRateLimiting]` / `RequireRateLimiting`.
- Cookies: `__Host-` prefix, `Secure`, `HttpOnly`, `Path=/`; the token cookies are written only by the JWT handler /
  sign-in manager, the activation cookie by `AccountActivationController`. All cookies are `__Host-odc.*`.
- After changing an endpoint or a contract: build, run the app, regenerate `schema.d.ts` from
  `https://localhost:<port>/openapi/v1.json`, and update the client.
- Translations of this project's texts: `Localization/en/web.json`.
- `UserSecretsId` is `odc-web-3b8f9c52-…`; the user's secrets live under that id (changing it loses them).
