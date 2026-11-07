import { useLingui } from "@lingui/react/macro";

/**
 * Props of {@link DocumentTitle}.
 */
export interface DocumentTitleProps {
    /** The page name; the product name is appended. */
    readonly children: string;
}

/**
 * Sets the document title (React hoists the title element into the head).
 * @param props The props.
 * @returns The title element.
 */
export function DocumentTitle({ children }: DocumentTitleProps) {
    const { t } = useLingui();
    return <title>{`${children} · ${t`КОД`}`}</title>;
}
