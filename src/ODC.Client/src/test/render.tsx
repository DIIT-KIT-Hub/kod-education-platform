import { I18nProvider } from "@lingui/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, type RenderResult } from "@testing-library/react";
import userEvent, { type UserEvent } from "@testing-library/user-event";
import { Suspense, type ReactNode } from "react";
import { createMemoryRouter, RouterProvider, useLocation, type RouteObject } from "react-router";
import { setAntiforgeryToken } from "@/shared/api/client";
import { ApiError } from "@/shared/api/problem";
import { i18n } from "@/shared/i18n/i18n";
import { authContextQuery } from "@/shared/session/session";
import { ConfirmHost } from "@/shared/ui/ConfirmHost";
import { ToastRegion } from "@/shared/ui/Toast";
import { makeAuthContext, makeUser, type Schemas } from "./factories";

/**
 * How a page is rendered in a test.
 */
export interface RenderOptions {
    /** The route pattern of the page (`/documents/:id`). */
    readonly path?: string;
    /** The address opened first (`/documents/42`). */
    readonly url?: string;
    /** The signed-in user; null for a visitor. Defaults to a user with every permission. */
    readonly user?: Schemas["CurrentUserResponse"] | null;
    /** Extra routes (e.g. the page a form leads to). */
    readonly routes?: readonly RouteObject[];
    /** The history state of the first entry (the list views keep their filters there). */
    readonly state?: unknown;
}

/**
 * The rendered page with what the test needs.
 */
export interface RenderedPage extends RenderResult {
    /** Simulates the user (clicks, typing). */
    readonly user: UserEvent;
    /** The query cache of the page. */
    readonly queryClient: QueryClient;
    /** The router (its current location tells where the page navigated). */
    readonly router: ReturnType<typeof createMemoryRouter>;
}

/**
 * A query client as the app configures it, without retries and caching between tests.
 * @returns The client.
 */
export function createTestQueryClient(): QueryClient {
    return new QueryClient({
        defaultOptions: {
            queries: { retry: false, staleTime: Infinity, gcTime: Infinity },
            mutations: { retry: false },
        },
    });
}

/**
 * Shows the address the page navigated to (routes a test does not render).
 * @returns The address.
 */
function Elsewhere() {
    const location = useLocation();
    return <p data-testid="location">{location.pathname + location.search + location.hash}</p>;
}

/**
 * Splits an address into a history entry.
 * @param url The address (path, query and hash).
 * @param state The history state.
 * @returns The entry.
 */
function entry(url: string, state: unknown) {
    const { pathname, search, hash } = new URL(url, "https://localhost");
    return { pathname, search, hash, state };
}

/**
 * Renders an element inside the providers of the app (query cache with the auth context, router, translations,
 * toasts and confirmation dialogs).
 * @param element The page or component.
 * @param options The route, the address and the user.
 * @returns The rendered page.
 */
export function renderPage(element: ReactNode, options: RenderOptions = {}): RenderedPage {
    const { path = "/", url, user = makeUser(), routes = [], state } = options;
    const queryClient = createTestQueryClient();
    const context = makeAuthContext(user);
    queryClient.setQueryData(authContextQuery.queryKey, context);
    setAntiforgeryToken(context.antiforgeryToken);

    const router = createMemoryRouter(
        [{ path, element: <Suspense fallback={<p>loading</p>}>{element}</Suspense> }, ...routes, { path: "*", element: <Elsewhere /> }],
        { initialEntries: [entry(url ?? path, state)] },
    );

    const result = render(
        <I18nProvider i18n={i18n}>
            <QueryClientProvider client={queryClient}>
                <RouterProvider router={router} />
                <ToastRegion />
                <ConfirmHost />
            </QueryClientProvider>
        </I18nProvider>,
    );

    return { ...result, user: userEvent.setup(), queryClient, router };
}

/**
 * The address the page navigated to (rendered by the catch-all route).
 * @returns The address.
 */
export async function navigatedTo(): Promise<string> {
    return (await screen.findByTestId("location")).textContent ?? "";
}

/**
 * Builds the error a failed call throws (`unwrap` turns problem details into it).
 * @param status The status code.
 * @param code The error code.
 * @param detail The message.
 * @returns The error.
 */
export function apiError(status: number, code: string, detail = "Помилка сервера."): ApiError {
    return new ApiError(status, code, detail);
}
