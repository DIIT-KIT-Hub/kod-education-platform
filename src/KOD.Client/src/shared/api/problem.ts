/**
 * An RFC 9457 problem returned by the API. `detail` is already in the user's language; `code` identifies the case
 * (e.g. `validation`, `locked_out`).
 */
export class ApiError extends Error {
    /**
     * Creates the error.
     * @param status The HTTP status.
     * @param code The application error code.
     * @param detail The message for the user.
     */
    constructor(
        readonly status: number,
        readonly code: string,
        readonly detail: string,
    ) {
        super(detail);
        this.name = "ApiError";
    }
}

/**
 * The shape of a problem response.
 */
interface ProblemBody {
    readonly status?: number;
    readonly title?: string;
    readonly detail?: string;
    readonly code?: string;
}

/**
 * Narrows an unknown value to a problem body.
 * @param value The value.
 * @returns True when it looks like a problem.
 */
function isProblem(value: unknown): value is ProblemBody {
    return typeof value === "object" && value !== null && ("detail" in value || "title" in value);
}

/**
 * The result of an openapi-fetch call.
 */
interface FetchResult<T> {
    readonly data?: T;
    readonly error?: unknown;
    readonly response: Response;
}

/**
 * Returns the data of a successful call or throws an {@link ApiError}, so TanStack Query and forms see one error type.
 * @param result The openapi-fetch result.
 * @returns The data.
 */
export async function unwrap<T>(result: Promise<FetchResult<T>>): Promise<T> {
    const { data, error, response } = await result;
    if (response.ok) {
        return data as T;
    }

    const problem = isProblem(error) ? error : undefined;
    throw new ApiError(
        response.status,
        problem?.code ?? problem?.title ?? String(response.status),
        problem?.detail ?? problem?.title ?? response.statusText,
    );
}

/**
 * The message to show for a failed call: the server's own message when it sent one, else the given text.
 * @param error The caught error.
 * @param fallback The message for network failures and unexpected answers (already translated).
 * @returns The message.
 */
export function describeError(error: unknown, fallback: string): string {
    return error instanceof ApiError && error.detail ? error.detail : fallback;
}
