import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { useNavigate } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { UserLookupItem } from "@/shared/api/schema";
import { Button } from "@/shared/ui/Button";
import { Dialog } from "@/shared/ui/Dialog";
import { Field } from "@/shared/ui/Field";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { chatApi, chatKeys } from "@/features/chats/api";
import styles from "./chats.module.css";
import { PeoplePicker } from "./PeoplePicker";

/**
 * Props of {@link NewChatDialog}.
 */
export interface NewChatDialogProps {
    /** Whether the dialog is open. */
    readonly open: boolean;
    /** Closes the dialog. */
    readonly onClose: () => void;
    /** People chosen in advance (adding people to a one-to-one chat starts a group with the other person). */
    readonly initial?: readonly UserLookupItem[];
}

/**
 * Starts a chat: one person opens the one-to-one chat with them (an existing one, if any), several people make a group
 * chat with an optional title.
 * @param props The props.
 * @returns The dialog.
 */
export function NewChatDialog({ open, onClose, initial = [] }: NewChatDialogProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const navigate = useNavigate();
    const [people, setPeople] = useState<readonly UserLookupItem[]>(initial);
    const [title, setTitle] = useState("");
    const [busy, setBusy] = useState(false);

    /**
     * Closes the dialog and forgets the choice.
     */
    function close(): void {
        setPeople(initial);
        setTitle("");
        onClose();
    }

    /**
     * Opens or creates the chat and goes to it.
     */
    async function start(): Promise<void> {
        setBusy(true);
        try {
            const ids = people.map((person) => person.id);
            const { id } = ids.length === 1 && ids[0] ? await chatApi.openDirect(ids[0]) : await chatApi.createGroup(title, ids);
            await queryClient.invalidateQueries({ queryKey: chatKeys.list });
            close();
            void navigate(`/chats/${id}`);
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося почати чат.`), { tone: "error" });
        } finally {
            setBusy(false);
        }
    }

    return (
        <Dialog
            open={open}
            onClose={close}
            title={t`Новий чат`}
            description={t`Оберіть одну людину для особистого чату або кількох — для групового.`}
            actions={
                <>
                    <Button variant="ghost" onClick={close}>
                        {t`Скасувати`}
                    </Button>
                    <Button busy={busy} disabled={people.length === 0} onClick={() => void start()}>
                        {people.length > 1 ? t`Створити груповий чат` : t`Почати чат`}
                    </Button>
                </>
            }
        >
            <div className={styles.dialogBody}>
                <PeoplePicker selected={people} onChange={setPeople} />
                {people.length > 1 && (
                    <Field label={t`Назва чату`} hint={t`Необов'язково: без назви чат називається іменами учасників.`}>
                        {(control) => (
                            <TextInput {...control} value={title} maxLength={120} onChange={(event) => setTitle(event.target.value)} />
                        )}
                    </Field>
                )}
            </div>
        </Dialog>
    );
}
