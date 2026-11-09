import { queryOptions } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import type { TicketCategory, TicketPriority, TicketStatus } from "@/shared/api/schema";

/**
 * The search, filters, sorting and paging of a request list (from the address).
 */
export interface TicketFilters {
    readonly search?: string | undefined;
    readonly page: number;
    readonly pageSize: number;
    readonly sortBy?: string | undefined;
    readonly descending: boolean;
    readonly status?: TicketStatus | undefined;
    readonly category?: TicketCategory | undefined;
    readonly openOnly: boolean;
    readonly assignedToMe: boolean;
}

/**
 * A new request.
 */
export interface NewTicket {
    readonly category: TicketCategory;
    readonly priority: TicketPriority;
    readonly subject: string;
    readonly description: string;
    readonly files: readonly File[];
}

/**
 * The keys of the request queries (a notification about a request refreshes all of them).
 */
export const ticketKeys = {
    all: ["tickets"] as const,
    mine: (filters: TicketFilters) => ["tickets", "mine", filters] as const,
    list: (filters: TicketFilters) => ["tickets", "all", filters] as const,
    details: (id: string) => ["tickets", "details", id] as const,
    handlers: ["tickets", "handlers"] as const,
};

/**
 * The query parameters of a request list in the API's spelling.
 * @param filters The filters.
 * @returns The parameters.
 */
function listQuery(filters: TicketFilters) {
    return {
        Search: filters.search,
        Page: filters.page,
        PageSize: filters.pageSize,
        SortBy: filters.sortBy,
        Descending: filters.descending,
        Status: filters.status,
        Category: filters.category,
        OpenOnly: filters.openOnly,
        AssignedToMe: filters.assignedToMe,
    };
}

/**
 * A page of the current user's requests.
 * @param filters The filters.
 * @returns The query.
 */
export function myTicketsQuery(filters: TicketFilters) {
    return queryOptions({
        queryKey: ticketKeys.mine(filters),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/tickets/mine", { signal, params: { query: listQuery(filters) } })),
        placeholderData: (previous) => previous,
    });
}

/**
 * A page of the requests of all users (handlers).
 * @param filters The filters.
 * @returns The query.
 */
export function allTicketsQuery(filters: TicketFilters) {
    return queryOptions({
        queryKey: ticketKeys.list(filters),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/tickets", { signal, params: { query: listQuery(filters) } })),
        placeholderData: (previous) => previous,
    });
}

/**
 * A request with its history.
 * @param id The request.
 * @returns The query.
 */
export function ticketQuery(id: string) {
    return queryOptions({
        queryKey: ticketKeys.details(id),
        queryFn: ({ signal }) => unwrap(api.GET("/api/v1/tickets/{id}", { signal, params: { path: { id } } })),
    });
}

/**
 * The people who handle requests.
 */
export const handlersQuery = queryOptions({
    queryKey: ticketKeys.handlers,
    queryFn: ({ signal }) => unwrap(api.GET("/api/v1/tickets/handlers", { signal })),
    staleTime: 5 * 60_000,
});

/**
 * The changes of requests.
 */
export const ticketApi = {
    /**
     * Submits a request with its files.
     * @param ticket The request.
     * @returns The new request.
     */
    submit: (ticket: NewTicket) => {
        const form = new FormData();
        form.append("Category", ticket.category);
        form.append("Priority", ticket.priority);
        form.append("Subject", ticket.subject);
        form.append("Description", ticket.description);
        ticket.files.forEach((file) => form.append("Files", file, file.name));
        return unwrap(api.POST("/api/v1/tickets", { body: {} as never, bodySerializer: () => form }));
    },

    /**
     * Writes a message in a request.
     * @param id The request.
     * @param body The text.
     * @param files The attached files.
     * @returns Resolves when saved.
     */
    comment: (id: string, body: string, files: readonly File[]) => {
        const form = new FormData();
        form.append("Body", body);
        files.forEach((file) => form.append("Files", file, file.name));
        return unwrap(
            api.POST("/api/v1/tickets/{id}/messages", { params: { path: { id } }, body: {} as never, bodySerializer: () => form }),
        );
    },

    /**
     * Changes the state of a request.
     * @param id The request.
     * @param status The new state.
     * @returns Resolves when saved.
     */
    changeStatus: (id: string, status: TicketStatus) =>
        unwrap(api.PUT("/api/v1/tickets/{id}/status", { params: { path: { id } }, body: { status } })),

    /**
     * Assigns a request to a handler or releases it.
     * @param id The request.
     * @param assigneeId The handler, or `null`.
     * @returns Resolves when saved.
     */
    assign: (id: string, assigneeId: string | null) =>
        unwrap(api.PUT("/api/v1/tickets/{id}/assignee", { params: { path: { id } }, body: { assigneeId } })),

    /**
     * Changes the category and the priority of a request.
     * @param id The request.
     * @param category What it is about.
     * @param priority How urgent it is.
     * @returns Resolves when saved.
     */
    classify: (id: string, category: TicketCategory, priority: TicketPriority) =>
        unwrap(api.PUT("/api/v1/tickets/{id}/classification", { params: { path: { id } }, body: { category, priority } })),
};
