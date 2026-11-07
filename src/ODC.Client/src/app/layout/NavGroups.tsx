import { useLingui } from "@lingui/react/macro";
import { ChevronDown } from "lucide-react";
import { useId, useState, type FocusEvent, type MouseEvent } from "react";
import { useLocation } from "react-router";
import { Icon } from "@/shared/ui/Icon";
import styles from "./AppShell.module.css";
import { isCurrent, type NavigationGroup } from "./navigation";
import { NavLinks } from "./NavLinks";

/**
 * Props of {@link NavGroups}.
 */
export interface NavGroupsProps {
    /** The sections. */
    readonly groups: readonly NavigationGroup[];
    /** Numbers shown on entries by their route (unread chat messages). */
    readonly counts?: Readonly<Record<string, number>>;
    /** Called when a link is chosen (closes the drawer). */
    readonly onNavigate?: () => void;
    /** Whether the sections show as the icon rail of the collapsed sidebar (every entry reachable). */
    readonly rail?: boolean;
    /** Called when the pointer or the focus reaches a link (rail tooltips). */
    readonly onHint?: (event: MouseEvent<HTMLElement> | FocusEvent<HTMLElement>, text: string) => void;
}

/**
 * The sections of the main menu as an accordion: one section is open at a time — the one of the current page, until the
 * user opens another (or closes it). A section with a single entry is that entry itself. On the icon rail of the
 * collapsed sidebar every entry shows, the sections divided by a line.
 * @param props The props.
 * @returns The sections.
 */
export function NavGroups({ groups, counts, rail, onNavigate, onHint }: NavGroupsProps) {
    const { t } = useLingui();
    const { pathname } = useLocation();
    const baseId = useId();
    const current = groups.find((group) => group.items.some((item) => isCurrent(item.to, pathname)))?.id ?? null;

    // The user's choice holds on the page where it was made; another page opens its own section again.
    const [choice, setChoice] = useState<{ readonly pathname: string; readonly id: string | null } | null>(null);
    const open = choice?.pathname === pathname ? choice.id : current;

    /**
     * Opens a section (closing the open one) or closes it.
     * @param id The section.
     */
    function toggle(id: string): void {
        setChoice({ pathname, id: open === id ? null : id });
    }

    return groups.map((group) => {
        if (group.items.length === 1) {
            return (
                <div key={group.id} className={styles.navGroup}>
                    <NavLinks
                        items={group.items}
                        linkClassName={styles.navLink}
                        activeClassName={styles.active}
                        counts={counts}
                        onNavigate={onNavigate}
                        onHint={onHint}
                    />
                </div>
            );
        }

        const expanded = open === group.id;
        const text = t(group.text);
        const bodyId = `${baseId}-${group.id}`;
        const hidden = group.items.reduce((sum, item) => sum + (counts?.[item.to] ?? 0), 0);
        return (
            <div key={group.id} className={styles.navGroup} data-open={expanded || undefined}>
                <button
                    type="button"
                    className={styles.groupHeader}
                    aria-expanded={expanded}
                    aria-controls={bodyId}
                    data-current={group.id === current || undefined}
                    onClick={() => toggle(group.id)}
                >
                    <Icon icon={group.icon} />
                    <span>{text}</span>
                    {!expanded && hidden > 0 && (
                        <span className={styles.count} aria-label={t`Непрочитаних: ${hidden}`}>
                            {hidden > 99 ? "99+" : hidden}
                        </span>
                    )}
                    <ChevronDown className={styles.chevron} aria-hidden="true" />
                </button>
                {/* A folded section keeps its links out of the tab order (on the rail every entry stays reachable). */}
                <div id={bodyId} className={styles.groupBody} role="group" aria-label={text} inert={!expanded && !rail}>
                    {/* The outer box folds to nothing; the spacing lives inside, so it slides with the entries. */}
                    <div className={styles.groupItems}>
                        <div className={styles.groupList}>
                            <NavLinks
                                items={group.items}
                                linkClassName={styles.navLink}
                                activeClassName={styles.active}
                                counts={counts}
                                onNavigate={onNavigate}
                                onHint={onHint}
                            />
                        </div>
                    </div>
                </div>
            </div>
        );
    });
}
