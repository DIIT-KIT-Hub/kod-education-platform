import type { ListParams } from "../api";
import { useViewParams } from "@/shared/lib/viewParams";

/**
 * The search, sorting and paging of a list page, read from its view state, and a setter for its filters.
 * @param defaultPageSize The page size when the view has none.
 * @returns The list parameters, a reader of other parameters and a filter setter that goes back to the first page.
 */
export function useListParams(defaultPageSize = 20) {
    const [params, setParams] = useViewParams();
    const list: ListParams = {
        search: params.get("search") ?? undefined,
        page: Math.max(1, Number(params.get("page")) || 1),
        pageSize: Number(params.get("pageSize")) || defaultPageSize,
        sortBy: params.get("sortBy") ?? undefined,
        descending: params.get("descending") === "true",
    };

    /**
     * Sets or clears a filter and returns to the first page.
     * @param name The parameter name.
     * @param value The value; empty clears it.
     */
    function setFilter(name: string, value: string | undefined): void {
        const next = new URLSearchParams(params);
        if (value) {
            next.set(name, value);
        } else {
            next.delete(name);
        }

        next.delete("page");
        setParams(next, { replace: true });
    }

    return { list, get: (name: string) => params.get(name) ?? undefined, setFilter };
}
