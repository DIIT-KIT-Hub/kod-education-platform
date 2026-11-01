import { useLingui } from "@lingui/react/macro";
import { useQueryClient } from "@tanstack/react-query";
import { Eraser, LogOut, Trash2, UserMinus, UserPlus } from "lucide-react";
import { useState } from "react";
import { useNavigate } from "react-router";
import { describeError } from "@/shared/api/problem";
import type { ChatDetails, UserLookupItem } from "@/shared/api/schema";
import { Badge } from "@/shared/ui/Badge";
import { Button } from "@/shared/ui/Button";
import { confirm, confirmWithOption } from "@/shared/ui/ConfirmHost";
import { Dialog } from "@/shared/ui/Dialog";
import { Field } from "@/shared/ui/Field";
import { PersonName } from "@/shared/ui/PersonName";
import { TextInput } from "@/shared/ui/TextInput";
import { toast } from "@/shared/ui/Toast";
import { chatApi, chatKeys } from "@/features/chats/api";
import styles from "./chats.module.css";
import { NewChatDialog } from "./NewChatDialog";
import { PeoplePicker } from "./PeoplePicker";

/**
 * Props of {@link MembersDialog}.
 */
export interface MembersDialogProps {
    /** Whether the dialog is open. */
    readonly open: boolean;
    /** Closes the dialog. */
    readonly onClose: () => void;
    /** The chat. */
    readonly chat: ChatDetails;
    /** The current user. */
    readonly me: string;
}

/**
 * The people of a chat. In a group anyone renames the chat and adds people, the owner removes people, and everyone may
 * leave; adding people to a one-to-one chat starts a new group with the other person (the one-to-one chat stays).
 * Everyone may clear the history for themselves; a one-to-one chat is deleted for one person or both, a group chat for
 * everyone by its owner.
 * @param props The props.
 * @returns The dialog.
 */
export function MembersDialog({ open, onClose, chat, me }: MembersDialogProps) {
    const { t } = useLingui();
    const queryClient = useQueryClient();
    const navigate = useNavigate();
    const [title, setTitle] = useState(chat.title ?? "");
    const [adding, setAdding] = useState<readonly UserLookupItem[]>([]);
    const [busy, setBusy] = useState(false);
    const [groupFromDirect, setGroupFromDirect] = useState(false);
    const isGroup = chat.kind === "Group";
    const owner = chat.myRole === "Owner";
    const peer = chat.members.find((m) => m.user.id !== me);

    /**
     * Runs a change and refreshes the chat (the other members get it live).
     * @param change The change.
     * @param fallback The message when it fails.
     */
    async function run(change: () => Promise<unknown>, fallback: string): Promise<void> {
        setBusy(true);
        try {
            await change();
            await queryClient.invalidateQueries({ queryKey: chatKeys.details(chat.id) });
            await queryClient.invalidateQueries({ queryKey: chatKeys.list });
        } catch (failure) {
            toast(describeError(failure, fallback), { tone: "error" });
        } finally {
            setBusy(false);
        }
    }

    /**
     * Leaves the chat after a confirmation.
     */
    async function leave(): Promise<void> {
        const confirmed = await confirm({
            title: t`Вийти з чату?`,
            text: t`Чат зникне з вашого списку. Повернутися можна, лише якщо хтось із учасників додасть вас знову.`,
            confirmText: t`Вийти`,
            danger: true,
        });
        if (!confirmed) {
            return;
        }

        try {
            await chatApi.removeMember(chat.id, me);
            queryClient.removeQueries({ queryKey: chatKeys.thread(chat.id) });
            await queryClient.invalidateQueries({ queryKey: chatKeys.list });
            onClose();
            void navigate("/chats", { replace: true });
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося вийти з чату.`), { tone: "error" });
        }
    }

    /**
     * Leaves the page of a chat that is gone for the user.
     */
    async function closeChat(): Promise<void> {
        onClose();
        await navigate("/chats", { replace: true });
        queryClient.removeQueries({ queryKey: chatKeys.thread(chat.id) });
        queryClient.removeQueries({ queryKey: chatKeys.details(chat.id) });
        await queryClient.invalidateQueries({ queryKey: chatKeys.list });
    }

    /**
     * Deletes a one-to-one chat for the user, or for both people when the check box is ticked.
     */
    async function deleteDirect(): Promise<void> {
        const forBoth = await confirmWithOption({
            title: t`Видалити чат?`,
            text: t`Історія зникне у вас, а чат повернеться до списку з новим повідомленням.`,
            confirmText: t`Видалити`,
            danger: true,
            option: {
                label: t`Також видалити для ${peer?.user.fullName ?? ""}`,
                hint: t`Чат з усіма повідомленнями й файлами буде видалено в обох.`,
            },
        });
        if (forBoth === null) {
            return;
        }

        try {
            await (forBoth ? chatApi.deleteChat(chat.id) : chatApi.clearHistory(chat.id));
            await closeChat();
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося видалити чат.`), { tone: "error" });
        }
    }

    /**
     * Clears the history of a group chat for the user after a confirmation.
     */
    async function clearHistory(): Promise<void> {
        const confirmed = await confirm({
            title: t`Очистити історію?`,
            text: t`Повідомлення зникнуть лише у вас, інші учасники чату й надалі їх бачитимуть.`,
            confirmText: t`Очистити`,
            danger: true,
        });
        if (!confirmed) {
            return;
        }

        try {
            await chatApi.clearHistory(chat.id);
            await queryClient.invalidateQueries({ queryKey: chatKeys.thread(chat.id) });
            await queryClient.invalidateQueries({ queryKey: chatKeys.list });
            onClose();
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося очистити історію.`), { tone: "error" });
        }
    }

    /**
     * Deletes a group chat for everyone after a confirmation (the owner).
     */
    async function deleteGroup(): Promise<void> {
        const confirmed = await confirm({
            title: t`Видалити чат для всіх?`,
            text: t`Чат з усіма повідомленнями й файлами буде видалено для всіх учасників. Цю дію не можна скасувати.`,
            confirmText: t`Видалити`,
            danger: true,
        });
        if (!confirmed) {
            return;
        }

        try {
            await chatApi.deleteChat(chat.id);
            await closeChat();
        } catch (failure) {
            toast(describeError(failure, t`Не вдалося видалити чат.`), { tone: "error" });
        }
    }

    if (groupFromDirect && peer) {
        return (
            <NewChatDialog
                open
                initial={[
                    { id: peer.user.id, fullName: peer.user.fullName, email: "", position: peer.position, photoId: peer.user.photoId },
                ]}
                onClose={() => {
                    setGroupFromDirect(false);
                    onClose();
                }}
            />
        );
    }

    return (
        <Dialog open={open} onClose={onClose} title={isGroup ? t`Учасники чату` : t`Про чат`}>
            <div className={styles.dialogBody}>
                {isGroup && (
                    <form
                        className={styles.renameRow}
                        onSubmit={(event) => {
                            event.preventDefault();
                            void run(() => chatApi.rename(chat.id, title), t`Не вдалося змінити назву.`);
                        }}
                    >
                        <Field label={t`Назва чату`} className={styles.grow}>
                            {(control) => (
                                <TextInput {...control} value={title} maxLength={120} onChange={(event) => setTitle(event.target.value)} />
                            )}
                        </Field>
                        <Button type="submit" variant="secondary" disabled={busy || title.trim() === (chat.title ?? "")}>
                            {t`Зберегти`}
                        </Button>
                    </form>
                )}

                <ul className={styles.memberList}>
                    {chat.members.map((member) => (
                        <li key={member.user.id} className={styles.member}>
                            <PersonName
                                name={member.user.fullName}
                                photoId={member.user.photoId}
                                meta={member.isActive ? member.position : t`Обліковий запис заблоковано`}
                            />
                            {member.role === "Owner" && isGroup && <Badge tone="outline">{t`Власник`}</Badge>}
                            {member.user.id === me && <Badge>{t`Ви`}</Badge>}
                            {isGroup && owner && member.user.id !== me && (
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    iconOnly
                                    icon={UserMinus}
                                    disabled={busy}
                                    onClick={() =>
                                        void run(() => chatApi.removeMember(chat.id, member.user.id), t`Не вдалося вилучити людину з чату.`)
                                    }
                                >
                                    {t`Вилучити ${member.user.fullName}`}
                                </Button>
                            )}
                        </li>
                    ))}
                </ul>

                {isGroup ? (
                    <section className={styles.addPeople} aria-label={t`Додати людей`}>
                        <h3 className={styles.sectionTitle}>{t`Додати людей`}</h3>
                        <PeoplePicker selected={adding} onChange={setAdding} exclude={chat.members.map((m) => m.user.id)} />
                        <div className={styles.dialogActions}>
                            <Button
                                icon={UserPlus}
                                disabled={adding.length === 0}
                                busy={busy}
                                onClick={() =>
                                    void run(
                                        async () => {
                                            await chatApi.addMembers(
                                                chat.id,
                                                adding.map((person) => person.id),
                                            );
                                            setAdding([]);
                                        },
                                        t`Не вдалося додати людей.`,
                                    )
                                }
                            >
                                {t`Додати`}
                            </Button>
                            <Button variant="danger" icon={LogOut} onClick={() => void leave()}>
                                {t`Вийти з чату`}
                            </Button>
                        </div>
                    </section>
                ) : (
                    <div className={styles.dialogActions}>
                        <Button variant="secondary" icon={UserPlus} onClick={() => setGroupFromDirect(true)}>
                            {t`Створити груповий чат`}
                        </Button>
                        <Button variant="danger" icon={Trash2} onClick={() => void deleteDirect()}>
                            {t`Видалити чат`}
                        </Button>
                    </div>
                )}

                {isGroup && (
                    <div className={styles.chatActions}>
                        <Button variant="secondary" icon={Eraser} onClick={() => void clearHistory()}>
                            {t`Очистити історію`}
                        </Button>
                        {owner && (
                            <Button variant="danger" icon={Trash2} onClick={() => void deleteGroup()}>
                                {t`Видалити чат для всіх`}
                            </Button>
                        )}
                    </div>
                )}
            </div>
        </Dialog>
    );
}
