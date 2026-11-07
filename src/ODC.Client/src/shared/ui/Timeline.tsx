import type { ReactNode } from "react";
import layout from "@/shared/styles/layout.module.css";
import styles from "./Timeline.module.css";

/**
 * The state of a timeline step.
 */
export type TimelineState = "done" | "current" | "failed" | "pending";

/**
 * Props of {@link Timeline}.
 */
export interface TimelineProps {
    /** The steps ({@link TimelineItem}). */
    readonly children: ReactNode;
}

/**
 * Steps on a vertical line (e.g. a signing route).
 * @param props The props.
 * @returns The timeline.
 */
export function Timeline({ children }: TimelineProps) {
    return <ol className={[styles.timeline, layout.stagger].join(" ")}>{children}</ol>;
}

/**
 * Props of {@link TimelineItem}.
 */
export interface TimelineItemProps {
    /** The state; done steps colour the line to the next one. */
    readonly state: TimelineState;
    /** The text in the circle (e.g. the order). */
    readonly marker: ReactNode;
    /** The content. */
    readonly children: ReactNode;
}

/**
 * A step of a {@link Timeline}.
 * @param props The props.
 * @returns The step.
 */
export function TimelineItem({ state, marker, children }: TimelineItemProps) {
    return (
        <li className={[styles.item, styles[state]].join(" ")}>
            <span className={styles.marker}>{marker}</span>
            <div className={styles.content}>{children}</div>
        </li>
    );
}
