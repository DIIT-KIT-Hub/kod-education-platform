import { queryOptions } from "@tanstack/react-query";
import { api } from "@/shared/api/client";
import { unwrap } from "@/shared/api/problem";
import type { DocumentKind, DocumentScope, DocumentStatus, SigningMode } from "@/shared/api/schema";

/**
 * The filters, sorting and paging of the document registry (from the address).
 */
export interface DocumentFilters {
    readonly scope: DocumentScope;
    readonly status?: DocumentStatus | undefined;
    readonly kind?: DocumentKind | undefined;
    readonly search?: string | undefined;
    readonly page: number;
    readonly pageSize: number;
    readonly sortBy?: string | undefined;
    readonly descending: boolean;
}

/**
 * The keys of the document queries (invalidated after changes).
 */
export const documentKeys = {
    all: ["documents"] as const,
    list: (filters: DocumentFilters) => ["documents", "list", filters] as const,
    details: (id: string) => ["documents", "details", id] as const,
    edit: (id: string) => ["documents", "edit", id] as const,
};

/**
 * A page of the registry.
 * @param filters The filters.
 * @returns The query.
 */
export function documentListQuery(filters: DocumentFilters) {
    return queryOptions({
        queryKey: documentKeys.list(filters),
        queryFn: () =>
            unwrap(
                api.GET("/api/v1/documents", {
                    params: {
                        query: {
                            Scope: filters.scope,
                            Status: filters.status,
                            Kind: filters.kind,
                            Search: filters.search,
                            Page: filters.page,
                            PageSize: filters.pageSize,
                            SortBy: filters.sortBy,
                            Descending: filters.descending,
                        },
                    },
                }),
            ),
        placeholderData: (previous) => previous,
    });
}

/**
 * A document with its route, signatures and the actions the user may take.
 * @param id The document.
 * @returns The query.
 */
export function documentQuery(id: string) {
    return queryOptions({
        queryKey: documentKeys.details(id),
        queryFn: () => unwrap(api.GET("/api/v1/documents/{id}", { params: { path: { id } } })),
    });
}

/**
 * A document as the editor shows it.
 * @param id The document.
 * @returns The query.
 */
export function documentEditQuery(id: string) {
    return queryOptions({
        queryKey: documentKeys.edit(id),
        queryFn: () => unwrap(api.GET("/api/v1/documents/{id}/edit", { params: { path: { id } } })),
        staleTime: 0,
    });
}

/**
 * The units the user may register documents in.
 */
export const authorUnitsQuery = queryOptions({
    queryKey: ["documents", "author-units"],
    queryFn: () => unwrap(api.GET("/api/v1/documents/author-units")),
    staleTime: 5 * 60_000,
});

/**
 * Finds users to add to a signing route.
 * @param term A part of the name or e-mail (at least two characters).
 * @returns The query.
 */
export function signerSearchQuery(term: string) {
    return queryOptions({
        queryKey: ["documents", "signers", term],
        queryFn: () => unwrap(api.GET("/api/v1/documents/signers", { params: { query: { term } } })),
        enabled: term.length >= 2,
        staleTime: 60_000,
    });
}

/**
 * The fields of a new or changed document.
 */
export interface DocumentForm {
    readonly title: string;
    readonly description: string;
    readonly kind: DocumentKind;
    readonly unitId: string;
    readonly mode: SigningMode;
    readonly signerIds: readonly string[];
    /** Sends the document to signing right away. */
    readonly submit: boolean;
    /** The file (required for a new document; a changed one keeps its file without it). */
    readonly file: File | null;
}

/**
 * Builds the multipart body of a document.
 * @param form The fields.
 * @returns The form data.
 */
function toFormData(form: DocumentForm): FormData {
    const data = new FormData();
    data.append("Title", form.title);
    if (form.description) {
        data.append("Description", form.description);
    }

    data.append("Kind", form.kind);
    data.append("OrganizationUnitId", form.unitId);
    data.append("SigningMode", form.mode);
    form.signerIds.forEach((id) => data.append("SignerIds", id));
    data.append("Submit", String(form.submit));
    if (form.file) {
        data.append("File", form.file);
    }

    return data;
}

/**
 * The changes of documents.
 */
export const documentsApi = {
    create: async (form: DocumentForm): Promise<string> => {
        const created = (await unwrap(
            api.POST("/api/v1/documents", { body: {} as never, bodySerializer: () => toFormData(form) }),
        )) as unknown as {
            id: string;
        };
        return created.id;
    },
    update: (id: string, form: DocumentForm) =>
        unwrap(api.PUT("/api/v1/documents/{id}", { params: { path: { id } }, body: {} as never, bodySerializer: () => toFormData(form) })),
    setStatus: (id: string, status: "InSigning" | "Draft" | "Archived") =>
        unwrap(api.PUT("/api/v1/documents/{id}/status", { params: { path: { id } }, body: { status } })),
    remove: (id: string) => unwrap(api.DELETE("/api/v1/documents/{id}", { params: { path: { id } } })),
    reject: (id: string, reason: string) =>
        unwrap(api.POST("/api/v1/documents/{id}/rejections", { params: { path: { id } }, body: { reason } })),
};
