import type { LucideIcon } from "lucide-react";
import styles from "./Icon.module.css";

/**
 * Icon sizes: inherit the text size (md), or fixed.
 */
export type IconSize = "sm" | "md" | "lg" | "xl";

/**
 * Props of {@link Icon}.
 */
export interface IconProps {
    /** The Lucide icon component. */
    readonly icon: LucideIcon;
    /** The size. */
    readonly size?: IconSize;
    /** Spins the icon (progress). */
    readonly spin?: boolean;
    /** An extra class. */
    readonly className?: string | undefined;
    /** A label for an icon that means something on its own; otherwise the icon is hidden from assistive technology. */
    readonly label?: string;
}

/**
 * A Lucide icon drawn with the design system's stroke and size.
 * @param props The props.
 * @returns The icon.
 */
export function Icon({ icon: Glyph, size = "md", spin = false, className, label }: IconProps) {
    const classes = [styles.icon, styles[size], spin ? styles.spin : undefined, className].filter(Boolean).join(" ");
    return <Glyph className={classes} aria-hidden={label ? undefined : true} aria-label={label} role={label ? "img" : undefined} />;
}
