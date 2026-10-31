import { useLingui } from "@lingui/react/macro";
import { MessagesSquare, SquarePen } from "lucide-react";
import { useState } from "react";
import { useParams } from "react-router";
import { Button } from "@/shared/ui/Button";
import { DocumentTitle } from "@/shared/ui/DocumentTitle";
import { EmptyState } from "@/shared/ui/EmptyState";
import { ChatList } from "@/features/chats/components/ChatList";
import styles from "@/features/chats/components/chats.module.css";
import { Conversation } from "@/features/chats/components/Conversation";
import { NewChatDialog } from "@/features/chats/components/NewChatDialog";

/**
 * The chats: the list on the left and the open conversation on the right, like in Teams. Phones show one of them:
 * the list, or the conversation with a way back.
 * @returns The page.
 */
export function ChatsPage() {
    const { t } = useLingui();
    const { chatId } = useParams();
    const [creating, setCreating] = useState(false);

    return (
        <div className={styles.page} data-open={chatId ? "" : undefined}>
            <ChatList onNew={() => setCreating(true)} />
            <section className={styles.pane}>
                {chatId ? (
                    <Conversation key={chatId} chatId={chatId} />
                ) : (
                    <>
                        <DocumentTitle>{t`Чати`}</DocumentTitle>
                        <EmptyState icon={MessagesSquare} title={t`Оберіть чат`} text={t`Або почніть нову розмову з колегою чи групою.`}>
                            <Button icon={SquarePen} onClick={() => setCreating(true)}>
                                {t`Новий чат`}
                            </Button>
                        </EmptyState>
                    </>
                )}
            </section>
            {creating && <NewChatDialog open onClose={() => setCreating(false)} />}
        </div>
    );
}
