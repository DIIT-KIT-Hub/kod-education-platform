import { QueryClient } from "@tanstack/react-query";
import { createQueryCache } from "@/shared/api/loadErrors";
import { ApiError } from "@/shared/api/problem";

/**
 * The cache of server data. Answers the server gave on purpose (4xx) are not retried; a load that still fails is
 * reported as a toast (`createQueryCache`).
 */
export const queryClient = new QueryClient({
    queryCache: createQueryCache(),
    defaultOptions: {
        queries: {
            staleTime: 30_000,
            retry: (failures, error) => !(error instanceof ApiError && error.status < 500) && failures < 2,
        },
    },
});
