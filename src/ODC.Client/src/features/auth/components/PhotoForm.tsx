import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { Save } from "lucide-react";
import { useRef, useState } from "react";
import { describeError } from "@/shared/api/problem";
import { reloadAuthContext } from "@/shared/session/session";
import { Button } from "@/shared/ui/Button";
import { PhotoPicker, type PhotoSelection } from "@/shared/ui/PhotoPicker";
import { toast } from "@/shared/ui/Toast";
import { uploadPhoto } from "../api";
import styles from "../pages/auth.module.css";

/**
 * Props of {@link PhotoForm}.
 */
export interface PhotoFormProps {
    /** Called after the photo was saved and the session reloaded. */
    readonly onSaved: () => Promise<void>;
}

/**
 * Chooses, crops and uploads the profile photo of the signed-in user (the server re-issues this browser's tokens, so
 * the session is reloaded afterwards).
 * @param props The props.
 * @returns The form.
 */
export function PhotoForm({ onSaved }: PhotoFormProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const selection = useRef<PhotoSelection | null>(null);
    const [chosen, setChosen] = useState(false);
    const [busy, setBusy] = useState(false);

    /**
     * Uploads the chosen photo and its square.
     */
    async function save(): Promise<void> {
        const photo = selection.current;
        if (!photo) {
            toast(t`Оберіть фото.`, { tone: "warning" });
            return;
        }

        setBusy(true);
        try {
            await uploadPhoto(photo.file, photo.crop);
            await reloadAuthContext(queryClient);
            toast(t`Фото збережено.`);
            await onSaved();
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося зберегти фото. Спробуйте ще раз.`), { tone: "error" });
        } finally {
            setBusy(false);
        }
    }

    return (
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
    );
}
