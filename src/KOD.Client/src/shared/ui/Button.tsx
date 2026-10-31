import type { LucideIcon } from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";
import { Link } from "react-router";
import { inkHandlers } from "@/shared/lib/ink";
import styles from "./Button.module.css";
import { Icon } from "./Icon";

/**
 * Button looks.
 */
export type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

/**
 * Button sizes.
 */
export type ButtonSize = "sm" | "md" | "lg";

/**
 * Props shared by buttons and button links.
 */
interface CommonProps {
    /** The look. */
    readonly variant?: ButtonVariant | undefined;
    /** The size. */
    readonly size?: ButtonSize | undefined;
    /** Stretches the button to the full width. */
    readonly block?: boolean | undefined;
    /** An icon before the label. */
    readonly icon?: LucideIcon | undefined;
    /** An icon after the label. */
    readonly iconEnd?: LucideIcon | undefined;
    /** Shows only the icon; the label is read by assistive technology. */
    readonly iconOnly?: boolean | undefined;
    /** Shows a spinner and blocks the button while an action runs. */
    readonly busy?: boolean | undefined;
    /** The label. */
    readonly children: ReactNode;
    /** An extra class. */
    readonly className?: string | undefined;
}

/**
 * Props of {@link Button}.
 */
export type ButtonProps = CommonProps & Omit<ButtonHTMLAttributes<HTMLButtonElement>, "children" | "className">;

/**
 * Props of {@link ButtonLink}.
 */
export interface ButtonLinkProps extends CommonProps {
    /** An application route (client-side navigation). */
    readonly to?: string;
    /** A URL outside the application (full page load), e.g. a page not yet moved to the application. */
    readonly href?: string;
}

/**
 * Builds the class list of a button.
 * @param props The props.
 * @returns The class names.
 */
function classes({ variant = "primary", size = "md", block, iconOnly, className }: CommonProps): string {
    return [styles.button, styles[variant], styles[size], block && styles.block, iconOnly && styles.iconOnly, className]
        .filter(Boolean)
        .join(" ");
}

/**
 * Renders the content of a button: icons and label.
 * @param props The props.
 * @returns The content.
 */
function Content({ icon, iconEnd, iconOnly, children }: CommonProps) {
    return (
        <>
            {icon && <Icon icon={icon} />}
            {iconOnly ? <span className={styles.label}>{children}</span> : children}
            {iconEnd && <Icon icon={iconEnd} />}
        </>
    );
}

/**
 * A pill-shaped button. Hover floods it with the theme's contrast colour from the point where the pointer entered.
 * @param props The props.
 * @returns The button.
 */
export function Button({
    variant,
    size,
    block,
    icon,
    iconEnd,
    iconOnly,
    busy,
    children,
    className,
    type = "button",
    disabled,
    ...rest
}: ButtonProps) {
    return (
        <button
            {...rest}
            {...inkHandlers}
            type={type}
            disabled={disabled === true || busy === true}
            aria-busy={busy || undefined}
            data-busy={busy || undefined}
            title={iconOnly && typeof children === "string" ? children : rest.title}
            className={classes({ variant, size, block, iconOnly, className, children })}
        >
            <Content icon={icon} iconEnd={iconEnd} iconOnly={iconOnly}>
                {children}
            </Content>
        </button>
    );
}

/**
 * A link that looks like a button.
 * @param props The props.
 * @returns The link.
 */
export function ButtonLink({ to, href, variant, size, block, icon, iconEnd, iconOnly, children, className }: ButtonLinkProps) {
    const content = (
        <Content icon={icon} iconEnd={iconEnd} iconOnly={iconOnly}>
            {children}
        </Content>
    );
    const props = { ...inkHandlers, className: classes({ variant, size, block, iconOnly, className, children }) };
    return to ? (
        <Link to={to} {...props}>
            {content}
        </Link>
    ) : (
        <a href={href} {...props}>
            {content}
        </a>
    );
}
