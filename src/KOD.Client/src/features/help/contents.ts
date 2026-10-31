import { msg } from "@lingui/core/macro";
import type { MessageDescriptor } from "@lingui/core";
import {
    CircleHelp,
    FileText,
    House,
    KeyRound,
    Network,
    Newspaper,
    Shield,
    ShieldCheck,
    Signature,
    Sparkles,
    User,
    type LucideIcon,
} from "lucide-react";

/**
 * A topic of the manual (a subsection with its own anchor).
 */
export interface HelpTopic {
    /** The anchor (unique on the page). */
    readonly id: string;
    /** The title in the heading and the table of contents. */
    readonly title: MessageDescriptor;
    /** Permissions of which one is needed to use the feature; empty for everyone. */
    readonly permissions: readonly string[];
}

/**
 * A chapter of the manual: one module of the cabinet.
 */
export interface HelpChapter {
    /** The anchor (unique on the page). */
    readonly id: string;
    /** The title. */
    readonly title: MessageDescriptor;
    /** The icon. */
    readonly icon: LucideIcon;
    /** One sentence about the module. */
    readonly summary: MessageDescriptor;
    /** Permissions of which one opens the module; empty for everyone. */
    readonly permissions: readonly string[];
    /** The topics in reading order. */
    readonly topics: readonly HelpTopic[];
}

/**
 * The table of contents of the manual. The chapters of both languages use these anchors.
 */
export const helpChapters: readonly HelpChapter[] = [
    {
        id: "intro",
        title: msg({ message: "Початок роботи", context: "help" }),
        icon: Sparkles,
        summary: msg({ message: "Що таке КОД, як влаштований інтерфейс і від чого залежить, що ви бачите.", context: "help" }),
        permissions: [],
        topics: [
            { id: "intro-about", title: msg({ message: "Що таке Кабінет організаційних документів", context: "help" }), permissions: [] },
            { id: "intro-interface", title: msg({ message: "Інтерфейс: меню, тема, сповіщення", context: "help" }), permissions: [] },
            { id: "intro-access", title: msg({ message: "Ролі та доступ", context: "help" }), permissions: [] },
            { id: "intro-glossary", title: msg({ message: "Словник термінів", context: "help" }), permissions: [] },
        ],
    },
    {
        id: "account",
        title: msg({ message: "Вхід і безпека облікового запису", context: "help" }),
        icon: ShieldCheck,
        summary: msg({ message: "Перший вхід, двофакторна автентифікація, резервні коди, паролі та вихід із системи.", context: "help" }),
        permissions: [],
        topics: [
            { id: "account-first-login", title: msg({ message: "Перший вхід у систему", context: "help" }), permissions: [] },
            { id: "account-sign-in", title: msg({ message: "Звичайний вхід", context: "help" }), permissions: [] },
            { id: "account-recovery", title: msg({ message: "Вхід за резервним кодом", context: "help" }), permissions: [] },
            { id: "account-lockout", title: msg({ message: "Невдалі спроби та блокування", context: "help" }), permissions: [] },
            { id: "account-forgot", title: msg({ message: "Забули пароль або змінили телефон", context: "help" }), permissions: [] },
            { id: "account-change-password", title: msg({ message: "Зміна пароля", context: "help" }), permissions: [] },
            { id: "account-sessions", title: msg({ message: "Тривалість входу та вихід із системи", context: "help" }), permissions: [] },
            { id: "account-security-tips", title: msg({ message: "Поради з безпеки", context: "help" }), permissions: [] },
        ],
    },
    {
        id: "home",
        title: msg({ message: "Головна сторінка", context: "help" }),
        icon: House,
        summary: msg({ message: "Короткий огляд: що чекає на вашу дію, ключові показники та свіжі новини.", context: "help" }),
        permissions: [],
        topics: [
            { id: "home-overview", title: msg({ message: "Що показує головна сторінка", context: "help" }), permissions: [] },
            { id: "home-indicators", title: msg({ message: "Показники документів", context: "help" }), permissions: ["documents.view"] },
        ],
    },
    {
        id: "documents",
        title: msg({ message: "Документи", context: "help" }),
        icon: FileText,
        summary: msg({ message: "Створення документів, маршрут підписання, підписання, відмова та архівування.", context: "help" }),
        permissions: ["documents.view"],
        topics: [
            {
                id: "documents-list",
                title: msg({ message: "Реєстр документів: вкладки, пошук, фільтри", context: "help" }),
                permissions: [],
            },
            { id: "documents-lifecycle", title: msg({ message: "Статуси та життєвий цикл документа", context: "help" }), permissions: [] },
            { id: "documents-create", title: msg({ message: "Створення документа", context: "help" }), permissions: ["documents.create"] },
            { id: "documents-route", title: msg({ message: "Маршрут підписання", context: "help" }), permissions: ["documents.create"] },
            { id: "documents-card", title: msg({ message: "Картка документа", context: "help" }), permissions: [] },
            {
                id: "documents-edit",
                title: msg({ message: "Редагування чернетки та відхиленого документа", context: "help" }),
                permissions: ["documents.create"],
            },
            { id: "documents-sign", title: msg({ message: "Підписання документа", context: "help" }), permissions: ["documents.sign"] },
            { id: "documents-reject", title: msg({ message: "Відмова в підписанні", context: "help" }), permissions: ["documents.sign"] },
            {
                id: "documents-author",
                title: msg({ message: "Надсилання, відкликання та повторне надсилання", context: "help" }),
                permissions: ["documents.create"],
            },
            { id: "documents-files", title: msg({ message: "Завантаження файлу та підписів", context: "help" }), permissions: [] },
            {
                id: "documents-archive",
                title: msg({ message: "Архівування та видалення", context: "help" }),
                permissions: ["documents.create", "documents.manage"],
            },
            { id: "documents-visibility", title: msg({ message: "Чиї документи ви бачите", context: "help" }), permissions: [] },
        ],
    },
    {
        id: "signing",
        title: msg({ message: "Підписання та перевірка файлів", context: "help" }),
        icon: Signature,
        summary: msg({ message: "Кваліфікований електронний підпис будь-яких файлів у браузері та перевірка підписів.", context: "help" }),
        permissions: ["signing.use"],
        topics: [
            { id: "signing-how", title: msg({ message: "Як працює електронний підпис у КОД", context: "help" }), permissions: [] },
            { id: "signing-keys", title: msg({ message: "Ключі та надавачі", context: "help" }), permissions: [] },
            {
                id: "signing-wizard",
                title: msg({ message: "Підписання файлів: покроковий майстер", context: "help" }),
                permissions: ["signing.use"],
            },
            { id: "signing-formats", title: msg({ message: "Формати та рівні підпису", context: "help" }), permissions: [] },
            { id: "signing-verify", title: msg({ message: "Перевірка підпису", context: "help" }), permissions: ["signing.use"] },
            { id: "signing-problems", title: msg({ message: "Повідомлення про помилки та що робити", context: "help" }), permissions: [] },
        ],
    },
    {
        id: "news",
        title: msg({ message: "Новини", context: "help" }),
        icon: Newspaper,
        summary: msg({ message: "Новини та оголошення: читання, реакції й коментарі, створення, публікація.", context: "help" }),
        permissions: ["news.view", "news.manage"],
        topics: [
            { id: "news-read", title: msg({ message: "Читання новин", context: "help" }), permissions: ["news.view"] },
            { id: "news-discussion", title: msg({ message: "Реакції та коментарі", context: "help" }), permissions: ["news.view"] },
            { id: "news-list", title: msg({ message: "Керування публікаціями", context: "help" }), permissions: ["news.manage"] },
            {
                id: "news-editor",
                title: msg({ message: "Створення та редагування публікації", context: "help" }),
                permissions: ["news.manage"],
            },
            {
                id: "news-formatting",
                title: msg({ message: "Форматування тексту, фото та галереї", context: "help" }),
                permissions: ["news.manage"],
            },
        ],
    },
    {
        id: "structure",
        title: msg({ message: "Структура", context: "help" }),
        icon: Network,
        summary: msg({
            message: "Організації, їхні підрозділи та працівники; власні типи підрозділів для будь-якої сфери.",
            context: "help",
        }),
        permissions: ["structure.view", "structure.manage"],
        topics: [
            {
                id: "structure-view",
                title: msg({ message: "Дерево організацій і підрозділів", context: "help" }),
                permissions: ["structure.view"],
            },
            {
                id: "structure-organizations",
                title: msg({ message: "Кілька організацій в одному кабінеті", context: "help" }),
                permissions: ["structure.view"],
            },
            {
                id: "structure-unit",
                title: msg({ message: "Сторінка підрозділу та працівники", context: "help" }),
                permissions: ["structure.view"],
            },
            {
                id: "structure-manage",
                title: msg({ message: "Керування організаціями та підрозділами", context: "help" }),
                permissions: ["structure.manage"],
            },
            { id: "structure-types", title: msg({ message: "Типи підрозділів", context: "help" }), permissions: ["structure.manage"] },
        ],
    },
    {
        id: "profile",
        title: msg({ message: "Профіль", context: "help" }),
        icon: User,
        summary: msg({
            message: "Особисті дані, фото, посади, дозволи, безпека облікового запису та входи на інших пристроях.",
            context: "help",
        }),
        permissions: [],
        topics: [
            { id: "profile-overview", title: msg({ message: "Мій профіль", context: "help" }), permissions: [] },
            { id: "profile-photo", title: msg({ message: "Фото профілю", context: "help" }), permissions: [] },
            { id: "profile-security", title: msg({ message: "Безпека: пароль, 2FA, резервні коди", context: "help" }), permissions: [] },
            { id: "profile-sessions", title: msg({ message: "Вихід на інших пристроях", context: "help" }), permissions: [] },
        ],
    },
    {
        id: "admin",
        title: msg({ message: "Адміністрування", context: "help" }),
        icon: Shield,
        summary: msg({ message: "Користувачі, імпорт та експорт, ролі й дозволи, структура, журнал аудиту.", context: "help" }),
        permissions: ["users.view", "roles.manage", "structure.manage", "news.manage", "audit.view"],
        topics: [
            { id: "admin-overview", title: msg({ message: "Панель адміністрування", context: "help" }), permissions: [] },
            {
                id: "admin-scope",
                title: msg({ message: "Адміністрування в межах організацій", context: "help" }),
                permissions: ["users.view", "structure.manage", "news.manage"],
            },
            { id: "admin-users-list", title: msg({ message: "Список користувачів", context: "help" }), permissions: ["users.view"] },
            {
                id: "admin-users-create",
                title: msg({ message: "Створення облікового запису", context: "help" }),
                permissions: ["users.manage"],
            },
            { id: "admin-users-edit", title: msg({ message: "Особисті дані та посади", context: "help" }), permissions: ["users.manage"] },
            {
                id: "admin-users-overrides",
                title: msg({ message: "Індивідуальні дозволи", context: "help" }),
                permissions: ["roles.manage"],
            },
            {
                id: "admin-users-security",
                title: msg({ message: "Безпека облікового запису: блокування, пароль, 2FA, сесії", context: "help" }),
                permissions: ["users.manage", "users.security"],
            },
            { id: "admin-import", title: msg({ message: "Імпорт користувачів", context: "help" }), permissions: ["users.import"] },
            { id: "admin-export", title: msg({ message: "Експорт користувачів", context: "help" }), permissions: ["users.export"] },
            { id: "admin-roles", title: msg({ message: "Ролі та матриця дозволів", context: "help" }), permissions: ["roles.manage"] },
            { id: "admin-audit", title: msg({ message: "Журнал аудиту", context: "help" }), permissions: ["audit.view"] },
            { id: "admin-hierarchy", title: msg({ message: "Межі повноважень адміністраторів", context: "help" }), permissions: [] },
        ],
    },
    {
        id: "roles",
        title: msg({ message: "Ролі та дозволи", context: "help" }),
        icon: KeyRound,
        summary: msg({ message: "Довідник усіх дозволів і стандартних ролей.", context: "help" }),
        permissions: [],
        topics: [
            { id: "roles-permissions", title: msg({ message: "Перелік дозволів", context: "help" }), permissions: [] },
            { id: "roles-system", title: msg({ message: "Стандартні ролі", context: "help" }), permissions: [] },
        ],
    },
    {
        id: "faq",
        title: msg({ message: "Запитання та відповіді", context: "help" }),
        icon: CircleHelp,
        summary: msg({ message: "Відповіді на часті запитання та що робити, коли щось не працює.", context: "help" }),
        permissions: [],
        topics: [
            { id: "faq-common", title: msg({ message: "Часті запитання", context: "help" }), permissions: [] },
            { id: "faq-support", title: msg({ message: "Куди звертатися по допомогу", context: "help" }), permissions: [] },
        ],
    },
];

/**
 * A chapter by its anchor, with its number.
 * @param id The anchor.
 * @returns The chapter and its 1-based number.
 */
export function chapterOf(id: string): { readonly chapter: HelpChapter; readonly number: number } {
    const index = helpChapters.findIndex((chapter) => chapter.id === id);
    const chapter = helpChapters[index];
    if (!chapter) {
        throw new Error(`Unknown manual chapter ${id}.`);
    }

    return { chapter, number: index + 1 };
}

/**
 * A topic by its anchor.
 * @param id The anchor.
 * @returns The topic.
 */
export function topicOf(id: string): HelpTopic {
    const topic = helpChapters.flatMap((chapter) => chapter.topics).find((item) => item.id === id);
    if (!topic) {
        throw new Error(`Unknown manual topic ${id}.`);
    }

    return topic;
}
