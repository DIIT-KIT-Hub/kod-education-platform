import { Bell, CheckCheck, Trash2 } from "lucide-react";
import { Icon } from "@/shared/ui/Icon";
import { Figure, Ui } from "../../components/Manual";
import notificationsPanelImage from "../../images/uk/notifications-panel.webp";
import notificationsPageImage from "../../images/uk/notifications-page.webp";

/**
 * The manual page "notifications-panel" (Ukrainian).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                Дзвіночок <Icon icon={Bell} /> (у меню ліворуч на комп'ютері, у верхній панелі на телефоні й планшеті) відкриває панель з
                останніми сповіщеннями. Непрочитані виділені крапкою; клік по сповіщенню відкриває документ, новину, чат чи звернення.{" "}
                <Ui>Прочитати всі</Ui> <Icon icon={CheckCheck} /> позначає всі прочитаними; <Ui>Усі сповіщення</Ui> веде до повного списку.
            </p>
            <Figure src={notificationsPanelImage} alt="Панель сповіщень" caption="Панель сповіщень відкривається дзвіночком у меню." />
            <Figure src={notificationsPageImage} alt="Сторінка сповіщень" caption="Сторінка «Сповіщення» з усією історією." />
            <p>
                На сторінці <Ui>Сповіщення</Ui> фільтр <Ui>Усі сповіщення</Ui> / <Ui>Непрочитані</Ui> обмежує список, кнопки біля кожного
                сповіщення позначають його прочитаним або видаляють <Icon icon={Trash2} />, а <Ui>Видалити прочитані</Ui> очищує список від
                прочитаних.
            </p>
        </>
    );
}
