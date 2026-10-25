using System.Collections.Frozen;
using P = KOD.Domain.Access.Permissions;

namespace KOD.Web.Components.Pages.Help;

/// <summary>
/// The structure of the user manual: chapters and topics in reading order. The table of contents, the headings and the
/// access badges are all built from this catalog; chapter components only supply the text.
/// </summary>
public static class HelpContents
{
    /// <summary>
    /// The chapters in reading order.
    /// </summary>
    private static readonly HelpChapter[] AllChapters =
    [
        new(
            "intro",
            "Початок роботи",
            "sparkles",
            "Що таке КОД, як влаштований інтерфейс і від чого залежить, що ви бачите.",
            [],
            [
                new("intro-about", "Що таке Кабінет освітніх даних"),
                new("intro-interface", "Інтерфейс: меню, тема, сповіщення"),
                new("intro-access", "Ролі та доступ"),
                new("intro-glossary", "Словник термінів"),
            ]),
        new(
            "account",
            "Вхід і безпека облікового запису",
            "shield-check",
            "Перший вхід, двофакторна автентифікація, резервні коди, паролі та сесії.",
            [],
            [
                new("account-first-login", "Перший вхід у систему"),
                new("account-sign-in", "Звичайний вхід"),
                new("account-recovery", "Вхід за резервним кодом"),
                new("account-lockout", "Невдалі спроби та блокування"),
                new("account-forgot", "Забули пароль або змінили телефон"),
                new("account-change-password", "Зміна пароля"),
                new("account-sessions", "Сесії та вихід із системи"),
                new("account-security-tips", "Поради з безпеки"),
            ]),
        new(
            "home",
            "Головна сторінка",
            "house",
            "Короткий огляд: що чекає на вашу дію, ключові показники та свіжі новини.",
            [],
            [
                new("home-overview", "Що показує головна сторінка"),
                new("home-indicators", "Показники документів", [P.Documents.View]),
            ]),
        new(
            "documents",
            "Документи",
            "file-text",
            "Створення документів, маршрут підписання, підписання, відмова та архівування.",
            [P.Documents.View],
            [
                new("documents-list", "Реєстр документів: вкладки, пошук, фільтри"),
                new("documents-lifecycle", "Статуси та життєвий цикл документа"),
                new("documents-create", "Створення документа", [P.Documents.Create]),
                new("documents-route", "Маршрут підписання", [P.Documents.Create]),
                new("documents-card", "Картка документа"),
                new("documents-edit", "Редагування чернетки та відхиленого документа", [P.Documents.Create]),
                new("documents-sign", "Підписання документа", [P.Documents.Sign]),
                new("documents-reject", "Відмова в підписанні", [P.Documents.Sign]),
                new("documents-author", "Надсилання, відкликання та повторне надсилання", [P.Documents.Create]),
                new("documents-files", "Завантаження файлу та підписів"),
                new("documents-archive", "Архівування та видалення", [P.Documents.Create, P.Documents.Manage]),
                new("documents-visibility", "Чиї документи ви бачите"),
            ]),
        new(
            "signing",
            "Підписання та перевірка файлів",
            "signature",
            "Кваліфікований електронний підпис будь-яких файлів у браузері та перевірка підписів.",
            [P.Signing.Use],
            [
                new("signing-how", "Як працює електронний підпис у КОД"),
                new("signing-keys", "Ключі та надавачі"),
                new("signing-wizard", "Підписання файлів: покроковий майстер", [P.Signing.Use]),
                new("signing-formats", "Формати та рівні підпису"),
                new("signing-verify", "Перевірка підпису", [P.Signing.Use]),
                new("signing-problems", "Повідомлення про помилки та що робити"),
            ]),
        new(
            "news",
            "Новини",
            "newspaper",
            "Новини та оголошення закладу: читання, створення, публікація.",
            [P.News.View, P.News.Manage],
            [
                new("news-read", "Читання новин", [P.News.View]),
                new("news-list", "Керування публікаціями", [P.News.Manage]),
                new("news-editor", "Створення та редагування публікації", [P.News.Manage]),
                new("news-formatting", "Форматування тексту (Markdown)", [P.News.Manage]),
            ]),
        new(
            "structure",
            "Структура",
            "network",
            "Організаційна структура закладу: підрозділи та їхні працівники.",
            [P.Structure.View, P.Structure.Manage],
            [
                new("structure-view", "Дерево структури та типи підрозділів", [P.Structure.View]),
                new("structure-unit", "Сторінка підрозділу та працівники", [P.Structure.View]),
                new("structure-manage", "Керування підрозділами", [P.Structure.Manage]),
            ]),
        new(
            "profile",
            "Профіль",
            "user",
            "Особисті дані, посади, дозволи, безпека облікового запису та сесії.",
            [],
            [
                new("profile-overview", "Мій профіль"),
                new("profile-security", "Безпека: пароль, 2FA, резервні коди"),
                new("profile-sessions", "Активні сесії"),
            ]),
        new(
            "admin",
            "Адміністрування",
            "shield",
            "Користувачі, імпорт та експорт, ролі й дозволи, журнал аудиту.",
            [P.Users.View, P.Roles.Manage, P.Structure.Manage, P.News.Manage, P.Audit.View],
            [
                new("admin-overview", "Панель адміністрування"),
                new("admin-users-list", "Список користувачів", [P.Users.View]),
                new("admin-users-create", "Створення облікового запису", [P.Users.Manage]),
                new("admin-users-edit", "Особисті дані та посади", [P.Users.Manage]),
                new("admin-users-overrides", "Індивідуальні дозволи", [P.Roles.Manage]),
                new("admin-users-security", "Безпека облікового запису: блокування, пароль, 2FA, сесії", [P.Users.Manage, P.Users.Security]),
                new("admin-import", "Імпорт користувачів", [P.Users.Import]),
                new("admin-export", "Експорт користувачів", [P.Users.Export]),
                new("admin-roles", "Ролі та матриця дозволів", [P.Roles.Manage]),
                new("admin-audit", "Журнал аудиту", [P.Audit.View]),
                new("admin-hierarchy", "Межі повноважень адміністраторів"),
            ]),
        new(
            "roles",
            "Ролі та дозволи",
            "key-round",
            "Довідник усіх дозволів і стандартних ролей закладу.",
            [],
            [
                new("roles-permissions", "Перелік дозволів"),
                new("roles-system", "Стандартні ролі"),
            ]),
        new(
            "faq",
            "Запитання та відповіді",
            "circle-help",
            "Відповіді на часті запитання та що робити, коли щось не працює.",
            [],
            [
                new("faq-common", "Часті запитання"),
                new("faq-support", "Куди звертатися по допомогу"),
            ]),
    ];

    /// <summary>
    /// The chapters and their numbers by anchor.
    /// </summary>
    private static readonly FrozenDictionary<string, (HelpChapter Chapter, int Number)> ChapterIndex =
        AllChapters.Select(static (c, i) => (Chapter: c, Number: i + 1)).ToFrozenDictionary(static e => e.Chapter.Id, StringComparer.Ordinal);

    /// <summary>
    /// The topics by anchor.
    /// </summary>
    private static readonly FrozenDictionary<string, HelpTopic> TopicIndex =
        AllChapters.SelectMany(static c => c.Topics).ToFrozenDictionary(static t => t.Id, StringComparer.Ordinal);

    /// <summary>
    /// Gets the chapters in reading order.
    /// </summary>
    public static IReadOnlyList<HelpChapter> Chapters => AllChapters;

    /// <summary>
    /// Gets a chapter by its anchor.
    /// </summary>
    /// <param name="id">The anchor.</param>
    /// <returns>The chapter.</returns>
    public static HelpChapter Chapter(string id) => ChapterIndex[id].Chapter;

    /// <summary>
    /// Gets the number of a chapter.
    /// </summary>
    /// <param name="id">The anchor.</param>
    /// <returns>The 1-based number.</returns>
    public static int NumberOf(string id) => ChapterIndex[id].Number;

    /// <summary>
    /// Gets a topic by its anchor.
    /// </summary>
    /// <param name="id">The anchor.</param>
    /// <returns>The topic.</returns>
    public static HelpTopic Topic(string id) => TopicIndex[id];
}
