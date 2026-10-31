import { useLingui } from "@lingui/react/macro";
import { SmilePlus } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import type { ReactionKind, ReactionSummary } from "@/shared/api/schema";
import { reactions } from "@/shared/i18n/display";
import layout from "@/shared/styles/layout.module.css";
import { Icon } from "@/shared/ui/Icon";
import styles from "./ReactionBar.module.css";

/**
 * Props of {@link ReactionBar}.
 */
export interface ReactionBarProps {
    /** The reactions so far and the user's own. */
    readonly summary: ReactionSummary;
    /** Whether the user may react (the article is published). */
    readonly canReact: boolean;
    /** Sets, changes or (the same emotion again) removes the user's reaction. */
    readonly onReact: (kind: ReactionKind) => Promise<void>;
    /** The small look of comments. */
    readonly compact?: boolean;
}

/**
 * The reactions to an article or a comment: one chip per emotion with its count, and a picker. One reaction per reader;
 * choosing the same one again removes it.
 * @param props The props.
 * @returns The bar.
 */
export function ReactionBar({ summary, canReact, onReact, compact }: ReactionBarProps) {
    const { t } = useLingui();
    const [open, setOpen] = useState(false);
    const [busy, setBusy] = useState(false);
    const picker = useRef<HTMLSpanElement>(null);

    // A click elsewhere closes the picker.
    useEffect(() => {
        if (!open) {
            return;
        }

        const close = (event: PointerEvent) => {
            if (!picker.current?.contains(event.target as Node)) {
                setOpen(false);
            }
        };
        document.addEventListener("pointerdown", close);
        return () => document.removeEventListener("pointerdown", close);
    }, [open]);

    /**
     * Sends a reaction.
     * @param kind The emotion.
     */
    async function react(kind: ReactionKind): Promise<void> {
        setOpen(false);
        setBusy(true);
        try {
            await onReact(kind);
        } finally {
            setBusy(false);
        }
    }

    return (
        <div className={[styles.reactions, compact && styles.compact].filter(Boolean).join(" ")}>
            {summary.counts.map((count) => {
                const reaction = reactions.find((item) => item.kind === count.kind);
                const name = reaction ? t(reaction.name) : count.kind;
                return (
                    <button
                        key={count.kind}
                        type="button"
                        className={styles.chip}
                        aria-pressed={summary.mine === count.kind}
                        disabled={!canReact || busy}
                        title={name}
                        onClick={() => void react(count.kind)}
                    >
                        <span className={styles.emoji} aria-hidden="true">
                            {reaction?.emoji}
                        </span>
                        <span className={layout.visuallyHidden}>{name}:</span>
                        <span>{count.count}</span>
                    </button>
                );
            })}
            {canReact && (
                <span ref={picker} className={styles.picker}>
                    <button
                        type="button"
                        className={styles.add}
                        aria-expanded={open}
                        disabled={busy}
                        title={t`Поставити реакцію`}
                        aria-label={t`Поставити реакцію`}
                        onClick={() => setOpen(!open)}
                    >
                        <Icon icon={SmilePlus} size="sm" />
                        {!compact && summary.counts.length === 0 && <span>{t`Реакція`}</span>}
                    </button>
                    {open && (
                        <span
                            className={styles.menu}
                            role="menu"
                            onKeyDown={(event) => {
                                if (event.key === "Escape") {
                                    setOpen(false);
                                }
                            }}
                        >
                            {reactions.map((reaction) => (
                                <button
                                    key={reaction.kind}
                                    type="button"
                                    role="menuitemradio"
                                    className={styles.option}
                                    aria-checked={summary.mine === reaction.kind}
                                    title={t(reaction.name)}
                                    aria-label={t(reaction.name)}
                                    onClick={() => void react(reaction.kind)}
                                >
                                    {reaction.emoji}
                                </button>
                            ))}
                        </span>
                    )}
                </span>
            )}
        </div>
    );
}
