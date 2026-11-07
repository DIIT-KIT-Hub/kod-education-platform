import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Camera, LogOut } from "lucide-react";
import { useNavigate } from "react-router";
import { reloadAuthContext, useCurrentUser } from "@/shared/session/session";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { authApi } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { PhotoForm } from "../components/PhotoForm";
import { useReturnUrl } from "../flow";

/**
 * The profile photo: required before the cabinet can be used, and replaceable later.
 * @returns The page.
 */
export function PhotoPage() {
    const { t } = useLingui();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const returnUrl = useReturnUrl();
    const required = useCurrentUser().photoId === null;

    /**
     * Signs out (a user without a photo cannot go anywhere else).
     */
    async function signOut(): Promise<void> {
        await authApi.logout();
        await reloadAuthContext(queryClient);
        await navigate("/account/login", { replace: true });
    }

    return (
        <>
            <AuthHeader
                icon={Camera}
                title={required ? t`Додайте фото профілю` : t`Фото профілю`}
                text={
                    required
                        ? t`Колеги бачать Ваше фото в документах, новинах і структурі. Без фото кабінетом користуватися не можна.`
                        : t`Нове фото замінить поточне всюди в кабінеті.`
                }
            />

            <PhotoForm
                onSaved={async () => {
                    await navigate(required ? returnUrl : "/profile", { replace: true });
                }}
            />

            {required ? (
                <Button variant="ghost" icon={LogOut} block onClick={() => void signOut()}>
                    {t`Вийти`}
                </Button>
            ) : (
                <ButtonLink to="/profile" variant="ghost" icon={ArrowLeft}>
                    {t`Повернутися`}
                </ButtonLink>
            )}
        </>
    );
}
