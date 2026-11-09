import newsManageImage from "../../images/uk/news-manage.webp";
import { Figure, Ui } from "../../components/Manual";

/**
 * The manual page "news-list" (Ukrainian).
 * @returns The page content.
 */
export function Content() {
    return (
        <>
            <p>
                У стрічці новин натисніть <Ui>Керування</Ui> (або <Ui>Адміністрування</Ui> → <Ui>Новини</Ui>). Тут видно публікації
                організацій, новинами яких Ви керуєте (адміністратор усіх організацій бачить усі), зокрема чернетки — їх бачать лише
                редактори.
            </p>
            <Figure src={newsManageImage} alt="Керування новинами" caption="Керування новинами: чернетки, опубліковані й закріплені." />
            <ul>
                <li>Пошук — за заголовком або анонсом.</li>
                <li>
                    Колонки: <strong>Заголовок</strong> (і категорія), <strong>Статус</strong> («Чернетка» / «Опубліковано», «Закріплено»),{" "}
                    <strong>Опубліковано</strong>, <strong>Змінено</strong>, <strong>Організація</strong> («Увесь кабінет» — для загальних
                    новин), <strong>Автор</strong>, кнопка <Ui>Редагувати</Ui>.
                </li>
                <li>
                    <Ui>Нова публікація</Ui> — створити новину.
                </li>
            </ul>
        </>
    );
}
