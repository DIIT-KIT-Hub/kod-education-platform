import { mediaUrl } from "@/shared/lib/media";
import styles from "./Avatar.module.css";

/**
 * Avatar sizes.
 */
export type AvatarSize = "sm" | "md" | "lg";

/**
 * Props of {@link Avatar}.
 */
export interface AvatarProps {
    /** The person's full name (initials when there is no photo, the image description otherwise). */
    readonly name: string;
    /** The profile photo. */
    readonly photoId?: string | null | undefined;
    /** The size; the large one loads the full photo, the others the thumbnail. */
    readonly size?: AvatarSize;
    /** Hides the image from assistive technology when the name is written next to it. */
    readonly decorative?: boolean;
    /** An extra class. */
    readonly className?: string;
}

/**
 * The first letters of the first two words of a name.
 * @param name The name.
 * @returns The initials, or "?".
 */
export function initials(name: string): string {
    const parts = name.split(" ").filter(Boolean);
    return parts.length === 0
        ? "?"
        : parts
              .slice(0, 2)
              .map((part) => part.charAt(0))
              .join("");
}

/**
 * A person's profile photo, or their initials.
 * @param props The props.
 * @returns The avatar.
 */
export function Avatar({ name, photoId, size = "md", decorative, className }: AvatarProps) {
    const classes = [styles.avatar, styles[size], className].filter(Boolean).join(" ");
    return photoId ? (
        <img className={classes} src={mediaUrl(photoId, size !== "lg")} alt={decorative ? "" : name} loading="lazy" decoding="async" />
    ) : (
        <span className={classes} aria-hidden="true">
            {initials(name)}
        </span>
    );
}
