import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Camera, LogOut, Save } from "lucide-react";
import { useRef, useState } from "react";
import { useNavigate } from "react-router";
import { describeError } from "@/shared/api/problem";
import { reloadAuthContext, useCurrentUser } from "@/shared/session/session";
import { Alert } from "@/shared/ui/Alert";
import { Button, ButtonLink } from "@/shared/ui/Button";
import { PhotoPicker, type PhotoSelection } from "@/shared/ui/PhotoPicker";
import { toast } from "@/shared/ui/Toast";
import { authApi, uploadPhoto } from "../api";
import { AuthHeader } from "../components/AuthHeader";
import { useReturnUrl } from "../flow";
import styles from "./auth.module.css";

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
    const selection = useRef<PhotoSelection | null>(null);
    const [chosen, setChosen] = useState(false);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    /**
     * Uploads the chosen photo and its square.
     */
    async function save(): Promise<void> {
        const photo = selection.current;
        if (!photo) {
            setError(t`Оберіть фото.`);
            return;
        }

        setBusy(true);
        setError(null);
        try {
            await uploadPhoto(photo.file, photo.crop);
            await reloadAuthContext(queryClient);
            toast(t`Фото збережено.`);
            await navigate(required ? returnUrl : "/profile", { replace: true });
        } catch (failure) {
            setError(describeError(failure, t`Не вдалося зберегти фото. Спробуйте ще раз.`));
        } finally {
            setBusy(false);
        }
    }

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
                        ? t`Колеги бачать ваше фото в документах, новинах і структурі. Без фото кабінетом користуватися не можна.`
                        : t`Нове фото замінить поточне всюди в кабінеті.`
                }
            />

            {error && <Alert tone="danger">{error}</Alert>}

            <form
                className={styles.form}
                noValidate
                onSubmit={(event) => {
                    event.preventDefault();
                    void save();
                }}
            >
                <PhotoPicker
                    disabled={busy}
                    onChange={(next) => {
                        selection.current = next;
                        setChosen(next !== null);
                    }}
                />
                <Button type="submit" icon={Save} block busy={busy} disabled={!chosen}>
                    {t`Зберегти фото`}
                </Button>
            </form>

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
