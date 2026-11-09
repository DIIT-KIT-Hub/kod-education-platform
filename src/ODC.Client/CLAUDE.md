# CLAUDE.md — ODC.Client (React SPA)

The whole interface of the cabinet. `ODC.Web` serves the built `dist/` at `/` (development: `Spa:Root` =
`../ODC.Client/dist`; publish: `ClientApp`). The API, cookies and CSP are same-origin, so there is no dev server with a
proxy: build and reload.

```bash
pnpm install
pnpm dev                    # vite build --watch --mode development; reload the page after a rebuild
pnpm typecheck && pnpm lint && pnpm test && pnpm build    # build = production, fails on a missing English text
pnpm format                 # Prettier (4 spaces from .editorconfig); format only the files you changed
pnpm i18n:extract           # after changing texts; then fill every English msgstr in src/locales/en/messages.po
pnpm api:types              # regenerate src/shared/api/schema.d.ts from a running server (script points at 7180;
                            # for another port run its command with that URL)
```

A development build (`npx vite build --mode development`) does not fail on missing translations — useful while
verifying, but run `pnpm build` before finishing.

## Stack

Always the newest versions (`pnpm up --latest`; exact versions, `savePrefix: ""`): React 19 + React Compiler
(`@rolldown/plugin-babel` + `reactCompilerPreset`), Vite 8, TypeScript 7 (strict, `noUncheckedIndexedAccess`;
`exactOptionalPropertyTypes` is off — it fights CSS-module classes and React props), React Router (data mode,
`createBrowserRouter`, lazy routes), TanStack Query, openapi-fetch + openapi-typescript, react-hook-form + zod, Lingui,
lucide-react, Vitest + happy-dom + Testing Library + MSW.

TypeScript 7 (the native compiler) has no programmatic API yet, so it runs side by side with TypeScript 6 as the
TypeScript team recommends: `@typescript/native` is an alias of `typescript@7` and provides `tsc` (typecheck, build),
while `typescript` is an alias of `@typescript/typescript6` for the tools that import the compiler (typescript-eslint,
openapi-typescript). Update both aliases together; drop the TS 6 alias once typescript-eslint supports TypeScript 7.

**No UI libraries**: every component is written by hand (the design system lives in `shared/ui` and `shared/styles`).
Add libraries only for real infrastructure, never for trivial cases.

## Structure

bulletproof-react, enforced by `no-restricted-imports` in `eslint.config.js`: `src/shared` (api, i18n, lib, realtime,
session, signing, styles, ui) ← `src/features/<feature>` (api, components, pages; a feature never imports another
feature) ← `src/app` (router, guards, layout, query client). Cross-feature state (the auth context, permissions) lives
in `shared/session`.

`features/auth`: `LoginPage`, `TwoFactorPage`, `RecoveryPage`, `SetupAuthenticatorPage` (re-enrolment after an
authenticator reset), `ActivationPage` (the activation wizard), `PhotoPage`, `ChangePasswordPage` (voluntary, from
Profile → Security), `LockoutPage`; shared pieces in `components/`: `AuthenticatorEnrollment.tsx`
(`LinkAuthenticator`, `RecoveryCodes`, `AuthenticatorCodeForm`), `PhotoForm`, `AuthHeader`, `AuthLayout`.

## Account activation (`/account/activate`)

There are no temporary passwords. `ActivationPage` is one form with a read-only stepper (`shared/ui/StepProgress`):
Пошта → Код → Пароль → Автентифікатор → Фото. Calls: `activationApi` in `features/auth/api.ts`
(`/api/v1/auth/activation`, `/code`, `/password`), then the usual `authApi.enroll` / `authApi.twoFactor` and
`uploadPhoto`. The server keeps the state in an HttpOnly cookie; after a reload the page asks `GET activation`, and when
there is none, `GET login/pending` (password already set → the authenticator step). It reacts to the error codes
`activation_invalid`, `activation_expired`, `no_activation`, `too_many_codes`, `email_unavailable`. The guard
`requireActivation` sends a signed-in user to `/` or `/account/photo`. The sign-in page links here ("Вперше в КОД?").

## Rules

- **CSS Modules only** (`*.module.css` next to the component, `localsConvention: camelCaseOnly`). Tokens, themes, base
  styles and fonts are `:global` in `shared/styles/global.module.css`; reusable layout/text utilities are in
  `shared/styles/layout.module.css`. Shared keyframes are global (`ds-*`) and used **only** through the `--ds-anim-*`
  custom properties: a keyframe name written in a module's `animation` is renamed to a local one. Local keyframes
  (`@keyframes settle`) are fine. Global selectors on `<html>` state: `:global([data-sidebar="collapsed"]) .x`.
  Inverse surfaces set `data-inverse`, buttons read it for the ink colours.
- **Data**: `shared/api/client.ts` (`api`, typed by `schema.d.ts`) adds `X-CSRF-TOKEN` to state-changing requests and
  reports 401s; `unwrap()` turns a failed call into `ApiError(status, code, detail, path)` (`detail` is already
  translated, empty without a problem body); `describeError(error, fallback)` for messages. The JWTs stay in HttpOnly
  cookies — scripts never see them.
- **Failed loads are toasts, never blocks on the page.** The query cache (`createQueryCache` in
  `shared/api/loadErrors.ts`, used by the app and by `createTestQueryClient`) shows every query that still fails after
  its retries as an error toast: the server's message, else "could not reach the server" (network) or "could not load
  the data". Identical visible toasts are not stacked. A 401 that ended the sign-in is skipped (the client leads to the
  sign-in page); a 401 of a sign-in step is shown. Pages do not render `isError` alerts: they keep their frame (header,
  breadcrumbs, filters) and leave the content empty. Query `meta.loadErrors`: `"refusal-is-content"` when the page
  shows a 4xx answer as its own state (`isRefusal(error)`: document / article / unit / chat not found, a document that
  cannot be edited), `"quiet"` when the page reports it itself (the auth context → route error page).
- **Downloads the server prepares** (exports, templates) go through `downloadFile(url, fallbackName)`
  (`shared/lib/download.ts`: fetch, `Content-Disposition` name, a temporary link) so the page knows when the file is
  ready; `DownloadButton` (`features/admin/components`) toasts the start and the end and stays busy meanwhile. Tests
  answer with `file(name)` (`test/server.ts`) and record saved files with `captureDownloads()` (`test/downloads.ts`).
- **Session**: `authContextQuery` (`GET /api/v1/auth/context`: language, antiforgery token, user with permissions) is
  loaded by the root route. The antiforgery token is bound to the identity, so **after every sign-in, sign-out,
  password, photo or language change call `reloadAuthContext(queryClient)`**. Guards in `app/guards.ts`
  (`requireReadyUser`: signed in with a photo; `requireUser`, `requireAnonymous`, `requireActivation`;
  `requirePermission(a, b, …)` lets a route through with any of the permissions, `adminPermissions` for `/admin`).
  Return addresses go through `safeReturnUrl`.
- **Forms**: react-hook-form + `zodResolver`; build the zod schema inside the component with `t` so its messages are
  translated; `<Field>` gives the control its id / `aria-describedby` / `aria-invalid` through a render prop.
- **Pages** set the document title with `<DocumentTitle>` or `<PageHeader>` (React hoists `<title>`).
- Global UI services are module stores read with `useSyncExternalStore`: `toast(text, { tone })` (`success` by
  default, `error`, `warning`, `info`; a top-layer popover, so it shows above modal dialogs), `await confirm({...})`,
  `await confirmWithOption({..., option})` (a check box such as "also delete for…"; null when cancelled).
- Loading data shows skeletons shaped like the content (`shared/ui/Skeleton`: `SkeletonTable`, `SkeletonList`,
  `SkeletonCards`, `SkeletonPage`, `SkeletonMessages`, `SkeletonText`, primitives `Skeleton` in a `SkeletonGroup`), not
  a spinner; `Spinner` stays only for a running action (a message being sent).
- Every outcome of an action (saved, deleted, failed, a rejected file, a form-level check) is a toast, never an alert on
  the page; inline `Alert`s are for explanations and results that are content (an import report, a
  signature verification, a file the signing wizard refuses to sign: too large or another checksum). Import and export
  report their start and end with toasts. Field errors stay under their fields.
- List pages keep search, filters, sorting and paging in the history entry, not in the address bar: `useViewParams`
  (`shared/lib/viewParams.ts`, the `useSearchParams` API over `location.state.view`) survives reloads and back/forward;
  an address that still has a query string is moved into the state once. `Tab`, `ButtonLink`, `StatCard` and
  breadcrumbs pass `to` through `viewTarget`, so `/documents?scope=AwaitingMe` opens `/documents` with that view; a
  plain `<Link>`/`navigate` to another page's view uses `viewTarget` too. (`DataGrid` sorts by `sortBy`/`descending`,
  `Pagination` by `page`/`pageSize`; `useListParams` in `features/admin`.) `DatePicker` (`shared/ui`) takes and returns
  `yyyy-MM-dd`, accepts typed dates in the day-month-year order and opens a calendar popover.
- Admin users: accounts are created without a password (the user gets an invitation); "Скинути пароль" sends the user
  back through the activation; "Надіслати запрошення ще раз" for active accounts awaiting activation; status filter
  `AwaitingActivation`. No password is ever shown in the UI.
- **Main menu** (`app/layout/navigation.ts`): `home` + sections (`NavigationGroup`) rendered by `NavGroups` as an
  accordion — the section of the current page is open until the user opens another (the choice is kept per pathname,
  no effects); a one-item section renders as a plain link; folded sections are `inert`; on the collapsed rail every
  entry shows. The tab bar of phones takes `navigation.items` with `tabBar`.
- Unit type icons are stored as Lucide names (`UnitType.Icons` on the server); the client maps them to components in
  `shared/lib/unitTypeIcons.ts` — add a new icon in both places. Module names: `moduleNames` in `shared/i18n/display.ts`;
  audit event names: `auditActionNames` (context `audit`).
- After a language change, queries other than the auth context are invalidated: some data comes translated by the
  server (permission titles, role and unit type names).
- **Signing** (`shared/signing`): `SigningClient` (typed worker client; worker messages are Ukrainian and translated
  through a table in `client.ts`), `SigningWizard` (document or local files), `KeyPanel`, format rules in `formats.ts`.
  The worker (`eusign.worker.js`, plain JS) is bundled by Vite (`new Worker(new URL(...))`); the IIT library stays in
  `ODC.Web/wwwroot/lib/eusign` (16 MB, served compressed by `MapStaticAssets`; the trust list fallback lives there too).
  Keep the order: key read only on the last step, released after signing / going back / closing / leaving. The private
  key and its password never leave the worker.
- **Manual** (`features/help`, routes `/help` and `/help/:topicId`): a user guide, not technical documentation. It is
  a docs-style site inside the cabinet: its route sits outside `AppShell` (no cabinet sidebar) but behind
  `requireReadyUser`, so only signed-in users see it. `HelpLayout` has the chapter navigation (`HelpNav`), the search
  (`HelpSearch`, Ctrl + K) and "Повернутися до системи", which opens the last cabinet page with its view
  (`shared/lib/lastPage.ts`: `AppShell` remembers it in sessionStorage; unsafe or unknown addresses fall back to `/`).
    - `contents.ts`: chapters → topics (id, title, icon, required permissions); `topicPath`, `neighboursOf`, `findEntry`.
    - One page per topic: `content/uk/<topic-id>.tsx` and its English twin `content/en/<topic-id>.tsx`, each exporting
      `Content`. `content/pages.ts` loads them lazily (`import.meta.glob`), only in the reader's language; `HelpTopicPage`
      shows one page with breadcrumbs, the access badge, "On this page" (from its `h2`s) and previous / next.
    - Building blocks from `components/Manual.tsx`: `<Ui>` (an interface label exactly as on screen), `<Steps>`,
      `<Ref to>` (link to a topic), `<Table>`, `<Faq>`, `<Kbd>`, `<Code>`, `<Tip>` / `<Note>` / `<Warning>`,
      `<Figure src alt caption>` (screenshots from `images/<language>/<name>.webp`), `<Access permissions>`. Subheadings
      are `h2` only (the topic title is the `h1`). The roles page is built from `GET /api/v1/permissions` and
      `/permissions/system-roles` (`RoleReference`).
    - Search (`search.ts`) indexes the pages by rendering them hidden once and matches words with Ukrainian/English
      normalization; a hit opens the page at its section.
    - Screenshots: `scripts/help-screenshots.mjs` drives headless Edge/Chrome over the DevTools protocol against a running
      verification instance with demo data (both languages, 1440 × 900, light theme, WebP). Run it only against your own
      instance: `ODC_URL`, `ODC_EMAIL`, `ODC_PASSWORD`, `ODC_TOTP_KEY` (see the comment at its top).
    - Update both languages when a feature changes. `contents.test.ts` checks unique ids, chapter order, that every
      `<Ref>` points to a topic and the `h2`-only structure; `help.test.tsx` covers the layout, navigation and search.
- Texts: Lingui with Ukrainian source texts — ``t`Зберегти` `` / `<Trans>` from `@lingui/react/macro`, ``msg`…` `` for
  module-level descriptors, `msg({ message, context })` for the same word with different meanings. Dates in Kyiv time
  through `shared/i18n/format.ts`, enum names through `shared/i18n/display.ts`.
- TypeScript: JSDoc on every exported and inner function, component and props member; function components, named
  exports, `@/` imports across folders.

## Design system

- Monochrome Diia-like style, Manrope (fonts in `shared/styles/fonts`), Lucide icons (`lucide-react` through `<Icon>`).
- Buttons and button-like controls have the "ink" hover: a `::before` fill of the theme's contrast colour fades in
  (opacity, `--ds-hover-duration`/`--ds-hover-ease`; compose `ink` from `shared/styles/ink.module.css`). Hover shadows
  transition with the same tokens — add `box-shadow` to the `transition` of anything that gains a shadow. Inverse
  surfaces flip the fill colours (`data-inverse`). Inputs show focus with the border only.
- Toasts are monochrome cards on the inverse surface; only the icon takes the tone colour (`--ds-color-*-on-inverse`).
- Brand: the mark is the message `К` (English "C") and the short name `КОД` (English "ODC"), on the server too (e-mail
  header, authenticator issuer).
- Popovers (`Select`, `DatePicker`) are `popover="manual"` panels in the top layer, placed next to their control, so
  they also open above modal dialogs.
- Scrollbars of dialogs, listboxes and the manual's contents are styled; in Chromium do not set `scrollbar-width`
  on them (it disables the `::-webkit-scrollbar` styles).
- **Fluid scale** (`global.module.css`): the root font size is `clamp(100%, 75% + 0.3125vw, 125%)` — 16 px up to a
  1280 px viewport, 20 px at 2560 px. Write every size in `rem` (never `px` for layout, type, space or panels) so it
  scales; `px` stays for hairlines, shadows, blur and media-query breakpoints (media queries ignore the root size).
  `--ds-container` is 85rem, 96rem from 1600 px. Wide layouts (`@media (min-width: 1600px)`, Material 3
  "extra-large") add columns or widen side panels (news grid 16rem cards, chat list 20–26rem) instead of only growing.
  Do not use CSS `zoom`, `transform: scale` or `vw`-only font sizes; check new layouts at 1366, 1920 and 2560 px.
- Steppers: `Wizard` (`shared/ui`) for forms the user moves through freely (Back / Next); `StepProgress` for flows the
  server drives one step at a time (activation). Both share the same look.

## Tests

Vitest + happy-dom + Testing Library + MSW (`pnpm test`, `pnpm test:coverage` → `coverage/index.html`). A test sits next
to its module (`LoginPage.test.tsx`). Shared helpers live in `src/test` (excluded from coverage and from the lint
rule that keeps features apart):

- `renderPage(element, { path, url, user, routes, state })` (`@/test/render`): renders inside the app's providers —
  a fresh query client (no retries) with the auth context already in the cache (`user` defaults to a user with every
  permission, `null` = visitor), a memory router (`path` is the route pattern, `url` the opened address, `state` the
  history state the list views read), Lingui in Ukrainian, toasts and confirmation dialogs. Returns `user`
  (user-event), `queryClient` and `router`. A navigation to an unrendered route shows its address:
  `await navigatedTo()`.
- `mockApi(method, "/api/v1/documents/{id}", answer)` (`@/test/server`): mocks an endpoint (OpenAPI path) and returns
  the recorded calls (`path`, `query`, `headers`, JSON or form `body`). The answer is a JSON body, a status code or
  a function; `problem(status, code, detail)` builds the server's problem details. **Every request needs a handler**:
  an unmocked call fails the test.
- `makeUser`, `makeAuthContext`, `makeUserRef`, `makePage` (`@/test/factories`); type other fixtures with
  `Schemas["DocumentDetails"]` so a contract change breaks the test at compile time.
- `src/test/architecture.test.ts` checks the sources: every `queryFn` passes the abort signal, no hand-written
  `fetch("/api/…")`.
- Query by role and visible Ukrainian text (`getByRole("button", { name: "Зберегти" })`, `getByLabelText`), not by CSS
  classes. Cover the success path, every validation message, every error code the page handles, permission-dependent
  UI (render with `makeUser({ permissions: [...] })`), empty and loading states, and what is sent (body, query, the
  `X-CSRF-TOKEN` header on state-changing calls).
- happy-dom lacks the popover API and modal dialogs; `src/test/setup.ts` adds minimal stand-ins. A confirmation dialog
  is found by its heading (`findByRole("heading", { name })` → `closest("dialog")`); its action button can share the
  name of the close button, take the last one.
- Live hub messages: `vi.mock("@/shared/realtime/live", () => import("@/shared/realtime/fakeLive"))`, then
  `emitLive("NotificationReceived", dto, 3)` / `reconnectLive()`.
- `ButtonLink`, `StatCard` and breadcrumbs move a target's query string into the history state (`viewTarget`), so
  their `href` is the bare path.
- happy-dom quirks met so far: a `user.click` into a Tiptap editor may leave an empty paragraph before the typed one;
  pointer moves fire a stray `mouseleave` (a hover-closed picker needs `fireEvent.click`); multipart parsing drops
  empty fields and reads non-ASCII file names as Latin-1 (use ASCII names when asserting `FormData`); `user-event`
  installs its own `navigator.clipboard` on setup, so stub the clipboard after `renderPage`. Lingui components need
  `renderPage` (or an `I18nProvider`), not a bare `render`.
- MSW matches handlers newest first: register `/api/v1/x/{id}` before fixed paths such as `/api/v1/x/options`.
- Timing under the full parallel run: `findBy*`/`waitFor` wait up to 5 s (`configure` in `setup.ts`), a test up to
  20 s (`vite.config.ts`); the manual's search builds its index from every page, so its tests wait up to 30 s (45 s per test). A
  success toast leaves after 4.5 s: assert it **before** the navigation that follows it, never after.

## Gotchas

- **React Compiler runs before the Lingui macro**: a static element inside `<Trans>` (`<strong>Microsoft
Authenticator</strong>`) can be hoisted into a memoized value; the runtime message id then misses the catalog and the
  page shows an id like `OFoRj4`. Keep `<Trans>` children dynamic (`<strong>{email}</strong>`, `<Link to>`) or use `t`.
- `react-hooks/purity` flags `Date.now()` even inside async handlers of a component; read the clock through a small
  module-level helper called only from handlers (see `answeredAt` in `ActivationPage`).
- An element whose size is measured (`clientWidth` in `PhotoPicker.place`) must not be hidden with `hidden` /
  `display: none` while it is measured — it reads 0. Hide it with `visibility: hidden` (`data-ready` in `PhotoPicker`).
- **CSS module without a used class is dropped**: importing a `.module.css` only for its side effects emits no CSS —
  use at least one of its classes (`global.root` on the root element keeps the global styles).
- **CSP and Vite**: `script-src 'self'` and `font-src 'self'` forbid `data:` URLs, so `build.assetsInlineLimit` is 0
  (Vite would inline small fonts). `img-src` allows `data:` (the authenticator QR code is an `<img>` with an SVG data
  URL) and `blob:`.
- **Vitest transform cache**: `import.meta.glob` results are cached in `node_modules/.vitest-cache`; after adding a
  file a glob should find (e.g. a manual page) delete that folder if a test still sees the old list.
- Manual pages are `React.lazy` components created at module level (`content/pages.ts`): `use()` with a promise does
  not settle inside act-based tests, and creating a lazy component during render breaks the lint rule and remounts.
  `HelpTopicPage` reads the page's headings in `useEffect` (not a layout effect: a cached page's child effect runs
  before the parent's ref is attached) and passes a stable state setter down, never a new callback per render
  (that looped).
- **Lookups by a value from outside** (server codes, history state): use `Object.hasOwn(map, key)`, not `map[key]` or
  `key in map` — `constructor` or `toString` would match a member of `Object.prototype`.
- Navigating the browser to the same address with another `#hash` does not reload the page (the client keeps its
  language and state); reload explicitly when a test needs a fresh start.
- Shell edits with backticks (template literals) break in Git Bash heredocs; write scripts with the Write tool.
