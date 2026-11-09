import { http, HttpResponse, type DefaultBodyType, type JsonBodyType, type StrictRequest } from "msw";
import { setupServer } from "msw/node";
import type { paths } from "@/shared/api/schema";

/**
 * The mocked API of every test: requests without a handler fail the test (`onUnhandledRequest: "error"` in the setup),
 * so a page never talks to anything the test did not describe.
 */
export const server = setupServer();

/**
 * An API path of the OpenAPI document (`/api/v1/documents/{id}`).
 */
export type ApiPath = keyof paths & string;

/**
 * The HTTP methods the cabinet uses.
 */
export type ApiMethod = "get" | "post" | "put" | "patch" | "delete";

/**
 * A recorded request: what the page sent.
 */
export interface RecordedRequest {
    /** The method. */
    readonly method: string;
    /** The path with its values (`/api/v1/documents/42`). */
    readonly path: string;
    /** The query string values. */
    readonly query: URLSearchParams;
    /** The headers. */
    readonly headers: Headers;
    /** The JSON body, the form data or null. */
    readonly body: unknown;
}

/**
 * Answers one request.
 */
export type Responder = (request: RecordedRequest, params: Record<string, string>) => Response | Promise<Response>;

/**
 * Turns an OpenAPI path into an MSW pattern (`{id}` → `:id`).
 * @param path The OpenAPI path.
 * @returns The pattern.
 */
function pattern(path: ApiPath): string {
    return path.replace(/\{(\w+)\}/g, ":$1");
}

/**
 * Reads what a request carried.
 * @param request The request.
 * @returns The recorded request.
 */
async function record(request: StrictRequest<DefaultBodyType>): Promise<RecordedRequest> {
    const url = new URL(request.url);
    const type = request.headers.get("content-type") ?? "";
    let body: unknown = null;
    if (type.includes("application/json")) {
        body = await request.clone().json();
    } else if (type.includes("multipart/form-data")) {
        body = await request.clone().formData();
    }

    return { method: request.method, path: url.pathname, query: url.searchParams, headers: request.headers, body };
}

/**
 * Mocks an endpoint and records the calls.
 * @param method The HTTP method.
 * @param path The OpenAPI path.
 * @param respond The answer: a JSON body (200), a status code without a body, or a function.
 * @returns The requests the page sent, in order.
 */
export function mockApi(method: ApiMethod, path: ApiPath, respond: JsonBodyType | number | Responder = 204): RecordedRequest[] {
    const calls: RecordedRequest[] = [];
    server.use(
        http[method](pattern(path), async ({ request, params }) => {
            const recorded = await record(request);
            calls.push(recorded);
            if (typeof respond === "function") {
                return respond(recorded, params as Record<string, string>);
            }

            if (typeof respond === "number") {
                return new HttpResponse(null, { status: respond });
            }

            return HttpResponse.json(respond);
        }),
    );
    return calls;
}

/**
 * Builds a problem details answer as the server sends it (RFC 9457 with the error code).
 * @param status The status code.
 * @param code The error code the client switches on.
 * @param detail The (translated) message.
 * @returns The response.
 */
export function problem(status: number, code: string, detail = "Помилка сервера."): Response {
    return HttpResponse.json({ title: code, code, detail, status }, { status, headers: { "Content-Type": "application/problem+json" } });
}

/**
 * Answers with JSON and a status.
 * @param body The body.
 * @param status The status code.
 * @returns The response.
 */
export function json(body: JsonBodyType, status = 200): Response {
    return HttpResponse.json(body, { status });
}

/**
 * Answers with a file as the server sends a download (`Content-Disposition` with the plain and the UTF-8 name).
 * @param name The file name.
 * @param body The content.
 * @returns The response.
 */
export function file(name: string, body = "file"): Response {
    return new HttpResponse(body, {
        headers: {
            "Content-Type": "application/octet-stream",
            "Content-Disposition": `attachment; filename=${name}; filename*=UTF-8''${encodeURIComponent(name)}`,
        },
    });
}
