import { useLingui } from "@lingui/react/macro";
import type { FocusEvent, MouseEvent } from "react";
import { NavLink } from "react-router";
import { Icon } from "@/shared/ui/Icon";
import type { NavigationItem } from "./navigation";

/**
 * Props of {@link NavLinks}.
 */
export interface NavLinksProps {
    /** The entries. */
    readonly items: readonly NavigationItem[];
    /** The class of a link. */
    readonly linkClassName: string | undefined;
    /** The class of the active link. */
    readonly activeClassName: string | undefined;
    /** How many entries to show. */
    readonly limit?: number;
    /** Called when a link is chosen (closes the drawer). */
    readonly onNavigate?: () => void;
    /** Called when the pointer or the focus reaches a link (rail tooltips). */
    readonly onHint?: (event: MouseEvent<HTMLElement> | FocusEvent<HTMLElement>, text: string) => void;
}

/**
 * The links of the main menu: application routes as client links (marked when active), pages not moved yet as plain
 * links.
 * @param props The props.
 * @returns The links.
 */
export function NavLinks({ items, linkClassName, activeClassName, limit, onNavigate, onHint }: NavLinksProps) {
    const { t } = useLingui();
    return items.slice(0, limit).map((item) => {
        const text = t(item.text);
        const content = (
            <>
                <Icon icon={item.icon} />
                <span>{text}</span>
            </>
        );
        const hint = onHint ? (event: MouseEvent<HTMLElement> | FocusEvent<HTMLElement>) => onHint(event, text) : undefined;
        return item.to ? (
            <NavLink
                key={text}
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) => [linkClassName, isActive && activeClassName].filter(Boolean).join(" ")}
                onClick={onNavigate}
                onMouseEnter={hint}
                onFocus={hint}
            >
                {content}
            </NavLink>
        ) : (
            <a key={text} href={item.href} className={linkClassName} onClick={onNavigate} onMouseEnter={hint} onFocus={hint}>
                {content}
            </a>
        );
    });
}
