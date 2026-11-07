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

Latest versions (the user allowed the newest npm releases; the two-week rule is for NuGet): React 19 + React Compiler
(`@rolldown/plugin-babel` + `reactCompilerPreset`), Vite 8, TypeScript 6 (strict, `noUncheckedIndexedAccess`;
`exactOptionalPropertyTypes` is off — it fights CSS-module classes and React props), React Router (data mode,
`createBrowserRouter`, lazy routes), TanStack Query, openapi-fetch + openapi-typescript, react-hook-form + zod, Lingui,
lucide-react, Vitest + happy-dom + Testing Library. Pinned on purpose: `typescript` 6.0.3 (typescript-eslint does not
support 7 yet), `@rolldown/plugin-babel` 0.2.4 (0.3 breaks `@vitejs/plugin-react` 6).

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
  reports 401s; `unwrap()` turns a failed call into `ApiError(status, code, detail)` (`detail` is already translated);
  `describeError(error, fallback)` for messages. The JWTs stay in HttpOnly cookies — scripts never see them.
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
  the page; inline `Alert`s are for load errors, explanations and results that are content (an import report, a
  signature verification). Field errors stay under their fields.
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
- **Manual** (`features/help`): the table of contents (anchors, titles, icons, permissions) is `contents.ts`; the text is
  `chapters/uk/<X>Chapter.tsx` and its English twin `chapters/en/<X>Chapter.tsx` with the same anchors (only the
  reader's language is loaded). Building blocks from `components/Manual.tsx`: `<Chapter id>`, `<Topic id>`, `<Ui>` (an
  interface label written exactly as on screen), `<Steps>`, `<Ref to>` (link to a topic), `<Table>`, `<Faq>`, `<Code>`.
  The roles chapter is built from `GET /api/v1/permissions` and `/permissions/system-roles`. Update both languages when
  a feature changes; `contents.test.ts` checks that both use every anchor and that every `<Ref>` points to a topic.
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
- Steppers: `Wizard` (`shared/ui`) for forms the user moves through freely (Back / Next); `StepProgress` for flows the
  server drives one step at a time (activation). Both share the same look.

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
  file a glob should find (e.g. a manual chapter) delete that folder if a test still sees the old list.
- Manual performance: topics use `content-visibility: auto`; `HelpPage` renders them once (`data-help-measuring`) to
  remember real heights so anchors land exactly. Keep that when changing the manual's layout. Its scroll, search
  highlight and contents tracking run in animation frames, which a hidden browser tab pauses.
- Navigating the browser to the same address with another `#hash` does not reload the page (the client keeps its
  language and state); reload explicitly when a test needs a fresh start.
- Shell edits with backticks (template literals) break in Git Bash heredocs; write scripts with the Write tool.
