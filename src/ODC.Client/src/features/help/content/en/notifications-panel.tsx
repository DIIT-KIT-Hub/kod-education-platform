import { Bell, CheckCheck, Trash2 } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Figure, Ui } from "../../components/Manual";
import notificationsPanelImage from "../../images/en/notifications-panel.webp";
import notificationsPageImage from "../../images/en/notifications-page.webp";

/**
 * The manual page "notifications-panel" (English).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                The bell <Icon icon={Bell} /> (in the left menu on a computer, in the top bar on phones and tablets) opens a panel with the
                latest notifications. Unread ones are marked with a dot; clicking a notification opens the document, the article, the chat
                or the request. <Ui>Mark all read</Ui> <Icon icon={CheckCheck} /> marks all of them read; <Ui>All notifications</Ui> leads
                to the full list.
            </p>
            <Figure
                src={notificationsPanelImage}
                alt="The notification panel"
                caption="The notification panel opens with the bell in the menu."
            />
            <Figure src={notificationsPageImage} alt="The notifications page" caption="The “Notifications” page with the whole history." />
            <p>
                On the <Ui>Notifications</Ui> page the <Ui>All notifications</Ui> / <Ui>Unread</Ui> filter narrows the list, the buttons
                next to each notification mark it read or delete it <Icon icon={Trash2} />, and <Ui>Delete read</Ui> clears the read ones.
            </p>
        </>
    );
}
