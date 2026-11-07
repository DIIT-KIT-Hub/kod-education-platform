import { CircleAlert, Info, type LucideIcon } from "lucide-react";
import type { ReactNode } from "react";
import styles from "./Alert.module.css";
import { Icon } from "./Icon";

/**
 * Alert looks.
 */
export type AlertTone = "neutral" | "inverse" | "outline" | "danger";

/**
 * Props of {@link Alert}.
 */
export interface AlertProps {
    /** The look; danger is announced at once (role="alert"). */
    readonly tone?: AlertTone;
    /** A bold first line. */
    readonly title?: ReactNode;
    /** An icon instead of the default one. */
    readonly icon?: LucideIcon;
    /** The message. */
    readonly children?: ReactNode;
    /** An extra class. */
    readonly className?: string;
}

/**
 * A message box.
 * @param props The props.
 * @returns The alert.
 */
export function Alert({ tone = "neutral", title, icon, children, className }: AlertProps) {
    return (
        <div
            className={[styles.alert, styles[tone], className].filter(Boolean).join(" ")}
            role={tone === "danger" ? "alert" : "status"}
            data-inverse={tone === "inverse" || undefined}
        >
            <Icon icon={icon ?? (tone === "danger" ? CircleAlert : Info)} />
            <div className={styles.body}>
                {title && <p className={styles.title}>{title}</p>}
                {children}
            </div>
        </div>
    );
}
