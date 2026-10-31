import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { LogOut } from "lucide-react";
import { useNavigate } from "react-router";
import { authApi } from "@/features/auth/api";
import { describeError } from "@/shared/api/problem";
import { disconnectLive } from "@/shared/realtime/live";
import { reloadAuthContext, useCurrentUser } from "@/shared/session/session";
import layout from "@/shared/styles/layout.module.css";
import { Avatar } from "@/shared/ui/Avatar";
import { Button } from "@/shared/ui/Button";
import { confirm } from "@/shared/ui/ConfirmHost";
import { ThemeToggle } from "@/shared/ui/ThemeToggle";
import { toast } from "@/shared/ui/Toast";
import styles from "./AppShell.module.css";

/**
 * The signed-in user's card: profile link, theme and sign-out.
 * @returns The card.
 */
export function UserMenu() {
    const { t } = useLingui();
    const user = useCurrentUser();
    const navigate = useNavigate();
    const queryClient = useQueryClient();

    /**
     * Signs out after a confirmation.
     */
    async function signOut(): Promise<void> {
        if (!(await confirm({ title: t`Вийти з кабінету?`, confirmText: t`Вийти` }))) {
            return;
        }

        try {
            await authApi.logout();
            await disconnectLive();
            await reloadAuthContext(queryClient);
            queryClient.removeQueries({ predicate: (query) => query.queryKey[0] !== "auth" });
            await navigate("/account/login", { replace: true });
        } catch (error) {
            toast(describeError(error, t`Не вдалося вийти. Спробуйте ще раз.`), { danger: true });
        }
    }

    return (
        <div className={styles.user}>
            <a className={styles.userCard} href="/profile">
                <Avatar name={user.fullName} photoId={user.photoId} decorative />
                <span className={styles.userText}>
                    <span className={[layout.strong, layout.truncate].join(" ")}>{user.shortName}</span>
                    <span className={[layout.textXs, layout.muted].join(" ")}>{t`Мій профіль`}</span>
                </span>
            </a>
            <div className={[layout.row, styles.userActions].join(" ")}>
                <ThemeToggle />
                <Button variant="ghost" size="sm" iconOnly icon={LogOut} onClick={() => void signOut()}>
                    {t`Вийти`}
                </Button>
            </div>
        </div>
    );
}
