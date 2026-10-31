import { Users } from "lucide-react";
import type { ChatKind, UserRef } from "@/shared/api/schema";
import { Avatar, type AvatarSize } from "@/shared/ui/Avatar";
import { Icon } from "@/shared/ui/Icon";
import styles from "./chats.module.css";

/**
 * Props of {@link ChatAvatar}.
 */
export interface ChatAvatarProps {
    /** The kind of the chat. */
    readonly kind: ChatKind;
    /** The members other than the current user. */
    readonly others: readonly UserRef[];
    /** The size. */
    readonly size?: AvatarSize;
}

/**
 * The picture of a chat: the other person of a one-to-one chat, two overlapping members of a group (or a group icon).
 * @param props The props.
 * @returns The picture.
 */
export function ChatAvatar({ kind, others, size = "md" }: ChatAvatarProps) {
    const [first, second] = others;
    if (kind === "Direct" && first) {
        return <Avatar name={first.fullName} photoId={first.photoId} size={size} decorative />;
    }

    if (first && second) {
        return (
            <span className={styles.groupAvatar} data-size={size} aria-hidden="true">
                <Avatar name={first.fullName} photoId={first.photoId} size="sm" decorative />
                <Avatar name={second.fullName} photoId={second.photoId} size="sm" decorative />
            </span>
        );
    }

    return (
        <span className={styles.groupIcon} data-size={size} aria-hidden="true">
            <Icon icon={Users} />
        </span>
    );
}
