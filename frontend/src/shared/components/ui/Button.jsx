import { cn } from "@/shared/utils/cn";
import styles from "./Button.module.css";

export default function Button({
  as: Tag = "button",
  variant = "primary",
  size = "md",
  loading = false,
  disabled = false,
  fullWidth = false,
  iconLeading = null,
  iconTrailing = null,
  className,
  children,
  ...rest
}) {
  const isIconOnly = !children && Boolean(iconLeading || iconTrailing);

  return (
    <Tag
      className={cn(
        styles.button,
        styles[variant],
        size !== "md" && styles[size],
        fullWidth && styles.fullWidth,
        isIconOnly && styles.iconOnly,
        className,
      )}
      data-loading={loading || undefined}
      disabled={Tag === "button" ? disabled || loading : undefined}
      aria-busy={loading || undefined}
      aria-disabled={disabled || loading || undefined}
      {...rest}
    >
      {iconLeading ? (
        <span
          className={cn(styles.icon, styles.iconLeading)}
          aria-hidden="true"
        >
          {iconLeading}
        </span>
      ) : null}

      {children ? <span className={styles.label}>{children}</span> : null}

      {iconTrailing ? (
        <span
          className={cn(styles.icon, styles.iconTrailing)}
          aria-hidden="true"
        >
          {iconTrailing}
        </span>
      ) : null}

      {loading ? (
        <span className={styles.spinner}>
          <span className={styles.spinnerRing} />
        </span>
      ) : null}
    </Tag>
  );
}
