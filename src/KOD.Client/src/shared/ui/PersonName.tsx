import type { ReactNode } from "react";
import { Avatar, type AvatarSize } from "./Avatar";
import styles from "./PersonName.module.css";

/**
 * Props of {@link PersonName}.
 */
export interface PersonNameProps {
    /** The full name. */
    readonly name: string;
    /** The profile photo. */
    readonly photoId?: string | null | undefined;
    /** A muted line under the name (position, e-mail). */
    readonly meta?: ReactNode;
    /** The avatar size. */
    readonly size?: AvatarSize;
}

/**
 * A person in a list cell or a picker: photo and name.
 * @param props The props.
 * @returns The person.
 */
export function PersonName({ name, photoId, meta, size = "sm" }: PersonNameProps) {
    return (
        <span className={styles.person}>
            <Avatar name={name} photoId={photoId} size={size} decorative />
            <span className={styles.text}>
                <span className={styles.name}>{name}</span>
                {meta && <span className={styles.meta}>{meta}</span>}
            </span>
        </span>
    );
}
