import { QueryClient } from "@tanstack/react-query";
import { ApiError } from "@/shared/api/problem";

/**
 * The cache of server data. Answers the server gave on purpose (4xx) are not retried.
 */
export const queryClient = new QueryClient({
    defaultOptions: {
        queries: {
            staleTime: 30_000,
            retry: (failures, error) => !(error instanceof ApiError && error.status < 500) && failures < 2,
        },
    },
});
