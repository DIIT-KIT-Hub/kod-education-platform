import { lazy, type ComponentType, type LazyExoticComponent } from "react";

/**
 * A page module of the manual: one topic in one language.
 */
interface PageModule {
    /** The content of the page. */
    readonly Content: ComponentType;
}

/**
 * The page modules (`./uk/documents-list.tsx`), each loaded only when it is opened or searched.
 */
const modules = import.meta.glob<PageModule>("./*/*.tsx");

/**
 * The pages as lazy components, made once per module (a page loads when it first renders).
 */
const lazyPages = new Map<string, LazyExoticComponent<ComponentType>>(
    Object.entries(modules).map(([path, load]) => [path, lazy(async () => ({ default: (await load()).Content }))]),
);

/**
 * The module path of a topic.
 * @param language The manual language ("uk" or "en").
 * @param id The topic.
 * @returns The path in the module map.
 */
function pathOf(language: string, id: string): string {
    return `./${language === "en" ? "en" : "uk"}/${id}.tsx`;
}

/**
 * Tells whether a topic has a page in a language.
 * @param language The manual language.
 * @param id The topic.
 * @returns Whether the page exists.
 */
export function hasPage(language: string, id: string): boolean {
    return pathOf(language, id) in modules;
}

/**
 * The lazy component of a topic page (render it inside Suspense).
 * @param language The manual language.
 * @param id The topic.
 * @returns The component, or undefined for an unknown topic.
 */
export function lazyPage(language: string, id: string): LazyExoticComponent<ComponentType> | undefined {
    return lazyPages.get(pathOf(language, id));
}

/**
 * Loads the page of a topic.
 * @param language The manual language.
 * @param id The topic.
 * @returns The content component.
 */
export async function loadPage(language: string, id: string): Promise<ComponentType> {
    const load = modules[pathOf(language, id)];
    if (!load) {
        throw new Error(`The manual has no page "${id}".`);
    }

    return (await load()).Content;
}

/**
 * Loads every page of a language (for the search).
 * @param language The manual language.
 * @param ids The topics in reading order.
 * @returns The content of each topic.
 */
export async function loadPages(language: string, ids: readonly string[]): Promise<ReadonlyMap<string, ComponentType>> {
    const pages = await Promise.all(
        ids.filter((id) => hasPage(language, id)).map(async (id) => [id, await loadPage(language, id)] as const),
    );
    return new Map(pages);
}
