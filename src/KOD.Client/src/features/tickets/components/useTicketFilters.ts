import { useSearchParams } from "react-router";
import type { TicketCategory, TicketStatus } from "@/shared/api/schema";
import { ticketCategoryNames, ticketStatusNames } from "@/shared/i18n/display";
import type { TicketFilters } from "@/features/tickets/api";

/**
 * The search, filters, sorting and paging of a request list, read from the address, and a setter for its filters.
 * @returns The filters and a filter setter that goes back to the first page.
 */
export function useTicketFilters() {
    const [params, setParams] = useSearchParams();
    const status = params.get("status");
    const category = params.get("category");
    const scope = params.get("scope");
    const filters: TicketFilters = {
        search: params.get("search") ?? undefined,
        page: Math.max(1, Number(params.get("page")) || 1),
        pageSize: Number(params.get("pageSize")) || 20,
        sortBy: params.get("sortBy") ?? undefined,
        descending: params.get("descending") === "true",
        status: status && status in ticketStatusNames ? (status as TicketStatus) : undefined,
        category: category && category in ticketCategoryNames ? (category as TicketCategory) : undefined,
        openOnly: scope === "open",
        assignedToMe: scope === "assigned",
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

    return { filters, scope: scope ?? "", setFilter };
}
