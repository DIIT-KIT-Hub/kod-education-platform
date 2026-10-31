import { msg } from "@lingui/core/macro";
import type { MessageDescriptor } from "@lingui/core";
import type {
    AppModule,
    DocumentKind,
    DocumentStatus,
    NewsCategory,
    ReactionKind,
    SignaturePackaging,
    SignerStatus,
    SigningMode,
    UserStatusFilter,
} from "@/shared/api/schema";

/**
 * The names of document kinds (the context separates "Довідка" = Certificate from "Довідка" = Help).
 */
export const documentKindNames: Record<DocumentKind, MessageDescriptor> = {
    Order: msg({ message: "Наказ", context: "document-kind" }),
    Directive: msg({ message: "Розпорядження", context: "document-kind" }),
    Memo: msg({ message: "Службова записка", context: "document-kind" }),
    Application: msg({ message: "Заява", context: "document-kind" }),
    Report: msg({ message: "Звіт", context: "document-kind" }),
    Minutes: msg({ message: "Протокол", context: "document-kind" }),
    Contract: msg({ message: "Договір", context: "document-kind" }),
    Certificate: msg({ message: "Довідка", context: "document-kind" }),
    Regulation: msg({ message: "Положення / інструкція", context: "document-kind" }),
    Letter: msg({ message: "Лист", context: "document-kind" }),
    Act: msg({ message: "Акт", context: "document-kind" }),
    Other: msg({ message: "Інший документ", context: "document-kind" }),
};

/**
 * The names of the modules (groups of permissions).
 */
export const moduleNames: Record<AppModule, MessageDescriptor> = {
    Documents: msg`Документообіг`,
    Signing: msg`Електронний підпис`,
    News: msg`Новини`,
    Structure: msg`Структура`,
    Users: msg`Користувачі`,
    Administration: msg`Адміністрування`,
    Audit: msg`Аудит`,
};

/**
 * The names of document states.
 */
export const documentStatusNames: Record<DocumentStatus, MessageDescriptor> = {
    Draft: msg`Чернетка`,
    InSigning: msg`На підписанні`,
    Signed: msg`Підписано`,
    Rejected: msg`Відхилено`,
    Archived: msg`В архіві`,
};

/**
 * The names of news categories.
 */
export const newsCategoryNames: Record<NewsCategory, MessageDescriptor> = {
    News: msg`Новини`,
    Announcement: msg`Оголошення`,
    Event: msg`Подія`,
    Training: msg`Навчання та розвиток`,
    Projects: msg`Проєкти та досягнення`,
};

/**
 * The states of a signer on a route.
 */
export const signerStatusNames: Record<SignerStatus, MessageDescriptor> = {
    Pending: msg`Очікується підпис`,
    Signed: msg`Підписав(-ла)`,
    Rejected: msg`Відмовив(-ла)`,
};

/**
 * The orders of signing.
 */
export const signingModeNames: Record<SigningMode, MessageDescriptor> = {
    Sequential: msg`Послідовно`,
    Parallel: msg`Паралельно`,
};

/**
 * The packagings of a signature.
 */
export const packagingNames: Record<SignaturePackaging, MessageDescriptor> = {
    Detached: msg`окремо від даних (зовнішній)`,
    Enveloped: msg`з даними (внутрішній)`,
};

/**
 * The names of signature formats (not translated).
 */
export const formatNames = { CAdES: "CAdES", XAdES: "XAdES", PAdES: "PAdES", AsicS: "ASiC-S", AsicE: "ASiC-E" } as const;

/**
 * The reactions in the order of the picker: their names (for assistive technology and tooltips) and emojis.
 */
export const reactions: readonly { readonly kind: ReactionKind; readonly name: MessageDescriptor; readonly emoji: string }[] = [
    { kind: "Like", name: msg`Подобається`, emoji: "👍" },
    { kind: "Dislike", name: msg`Не подобається`, emoji: "👎" },
    { kind: "Love", name: msg`Любов`, emoji: "❤️" },
    { kind: "Clap", name: msg`Оплески`, emoji: "👏" },
    { kind: "Celebrate", name: msg`Вітаю`, emoji: "🎉" },
    { kind: "Laugh", name: msg`Смішно`, emoji: "😄" },
    { kind: "Wow", name: msg`Вражає`, emoji: "😮" },
    { kind: "Thinking", name: msg`Сумнівно`, emoji: "🤔" },
    { kind: "Sad", name: msg`Сумно`, emoji: "😢" },
    { kind: "Angry", name: msg`Обурює`, emoji: "😡" },
];

/**
 * The account states of the user filter.
 */
export const userStatusNames: Record<UserStatusFilter, MessageDescriptor> = {
    All: msg`Усі статуси`,
    Active: msg`Активні`,
    Blocked: msg`Заблоковані`,
};

/**
 * The categories of the audit log filter (prefixes of the event codes).
 */
export const auditCategories: readonly { readonly prefix: string; readonly name: MessageDescriptor }[] = [
    { prefix: "auth.", name: msg`Автентифікація` },
    { prefix: "documents.", name: msg`Документи` },
    { prefix: "users.", name: msg`Користувачі` },
    { prefix: "roles.", name: msg`Ролі` },
    { prefix: "structure.", name: msg`Структура` },
    { prefix: "news.", name: msg`Новини` },
    { prefix: "signing.", name: msg`Підписання файлів` },
];

/**
 * The names of audit events by their codes (the server catalog is KOD.Domain.Audit.AuditActions); an unknown code is
 * shown as it is.
 */
export const auditActionNames: Readonly<Record<string, MessageDescriptor>> = {
    "auth.sign-in": msg({ message: "Вхід у систему", context: "audit" }),
    "auth.sign-in-failed": msg({ message: "Невдала спроба входу", context: "audit" }),
    "auth.locked-out": msg({ message: "Блокування після спроб входу", context: "audit" }),
    "auth.sign-out": msg({ message: "Вихід із системи", context: "audit" }),
    "auth.authenticator-enrolled": msg({ message: "Підключено автентифікатор", context: "audit" }),
    "auth.authenticator-reset": msg({ message: "Скинуто автентифікатор", context: "audit" }),
    "auth.recovery-codes": msg({ message: "Нові резервні коди", context: "audit" }),
    "auth.password-changed": msg({ message: "Зміна пароля", context: "audit" }),
    "auth.password-reset": msg({ message: "Скидання пароля", context: "audit" }),
    "auth.session-revoked": msg({ message: "Завершення сесій", context: "audit" }),
    "users.created": msg({ message: "Створено користувача", context: "audit" }),
    "users.updated": msg({ message: "Змінено користувача", context: "audit" }),
    "users.status-changed": msg({ message: "Зміна статусу користувача", context: "audit" }),
    "users.imported": msg({ message: "Імпорт користувачів", context: "audit" }),
    "users.exported": msg({ message: "Експорт користувачів", context: "audit" }),
    "users.photo-changed": msg({ message: "Нове фото профілю", context: "audit" }),
    "roles.changed": msg({ message: "Зміна ролі", context: "audit" }),
    "roles.deleted": msg({ message: "Видалення ролі", context: "audit" }),
    "structure.changed": msg({ message: "Зміна структури", context: "audit" }),
    "structure.type-changed": msg({ message: "Зміна типу підрозділу", context: "audit" }),
    "structure.type-deleted": msg({ message: "Видалення типу підрозділу", context: "audit" }),
    "documents.created": msg({ message: "Створено документ", context: "audit" }),
    "documents.updated": msg({ message: "Документ змінено", context: "audit" }),
    "documents.submitted": msg({ message: "Документ на підписання", context: "audit" }),
    "documents.withdrawn": msg({ message: "Документ відкликано", context: "audit" }),
    "documents.signed": msg({ message: "Документ підписано", context: "audit" }),
    "documents.rejected": msg({ message: "Відмова в підписанні", context: "audit" }),
    "documents.archived": msg({ message: "Документ в архіві", context: "audit" }),
    "documents.deleted": msg({ message: "Документ видалено", context: "audit" }),
    "news.changed": msg({ message: "Зміна новини", context: "audit" }),
    "news.deleted": msg({ message: "Видалення новини", context: "audit" }),
    "news.comment-deleted": msg({ message: "Видалення коментаря", context: "audit" }),
    "signing.files-signed": msg({ message: "Підписано файли", context: "audit" }),
};
