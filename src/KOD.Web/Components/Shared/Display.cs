using System.Globalization;
using KOD.Application.Features.Users;
using KOD.DesignSystem.Components;
using KOD.Domain.Access;
using KOD.Domain.Documents;
using KOD.Domain.News;
using KOD.Domain.Organization;

namespace KOD.Web.Components.Shared;

/// <summary>
/// User-facing (Ukrainian) texts, icons and tones for domain values, plus date formatting in Kyiv time.
/// </summary>
public static class Display
{
    /// <summary>
    /// The Ukrainian culture.
    /// </summary>
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>
    /// The time zone of the institution.
    /// </summary>
    private static readonly TimeZoneInfo Kyiv = KOD.Application.Common.Text.KyivTime.Zone;

    /// <summary>
    /// Formats a moment as a local date and time (e.g. «25.10.2026, 14:05»).
    /// </summary>
    /// <param name="moment">The moment.</param>
    /// <returns>The text, or «—» when empty.</returns>
    public static string DateTime(DateTimeOffset? moment)
        => moment is { } value ? TimeZoneInfo.ConvertTime(value, Kyiv).ToString("dd.MM.yyyy, HH:mm", Culture) : "—";

    /// <summary>
    /// Formats a moment as a local date (e.g. «25 жовтня 2026»).
    /// </summary>
    /// <param name="moment">The moment.</param>
    /// <returns>The text, or «—» when empty.</returns>
    public static string Date(DateTimeOffset? moment)
        => moment is { } value ? TimeZoneInfo.ConvertTime(value, Kyiv).ToString("d MMMM yyyy", Culture) : "—";

    /// <summary>
    /// Formats a size in bytes («1,4 МБ»).
    /// </summary>
    /// <param name="bytes">The size.</param>
    /// <returns>The text.</returns>
    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} Б",
        < 1024 * 1024 => string.Create(Culture, $"{bytes / 1024d:0.#} КБ"),
        _ => string.Create(Culture, $"{bytes / 1024d / 1024d:0.#} МБ"),
    };

    /// <summary>
    /// Returns the name of a document kind.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The text.</returns>
    public static string Text(DocumentKind kind) => kind switch
    {
        DocumentKind.Order => "Наказ",
        DocumentKind.Directive => "Розпорядження",
        DocumentKind.Memo => "Службова записка",
        DocumentKind.Application => "Заява",
        DocumentKind.Report => "Звіт",
        DocumentKind.Minutes => "Протокол",
        DocumentKind.Contract => "Договір",
        DocumentKind.Certificate => "Довідка",
        DocumentKind.Curriculum => "Навчальний план / силабус",
        DocumentKind.Letter => "Лист",
        _ => "Інший документ",
    };

    /// <summary>
    /// Returns the name of a document state.
    /// </summary>
    /// <param name="status">The state.</param>
    /// <returns>The text.</returns>
    public static string Text(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => "Чернетка",
        DocumentStatus.InSigning => "На підписанні",
        DocumentStatus.Signed => "Підписано",
        DocumentStatus.Rejected => "Відхилено",
        _ => "В архіві",
    };

    /// <summary>
    /// Returns the badge tone of a document state.
    /// </summary>
    /// <param name="status">The state.</param>
    /// <returns>The tone.</returns>
    public static Tone ToneOf(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => Tone.Dashed,
        DocumentStatus.InSigning => Tone.Outline,
        DocumentStatus.Signed => Tone.Inverse,
        DocumentStatus.Rejected => Tone.Danger,
        _ => Tone.Neutral,
    };

    /// <summary>
    /// Returns the icon of a document state.
    /// </summary>
    /// <param name="status">The state.</param>
    /// <returns>The icon name.</returns>
    public static string IconOf(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => "pencil",
        DocumentStatus.InSigning => "hourglass",
        DocumentStatus.Signed => "badge-check",
        DocumentStatus.Rejected => "circle-x",
        _ => "archive",
    };

    /// <summary>
    /// Returns the name of a signer state.
    /// </summary>
    /// <param name="status">The state.</param>
    /// <returns>The text.</returns>
    public static string Text(SignerStatus status) => status switch
    {
        SignerStatus.Signed => "Підписав(-ла)",
        SignerStatus.Rejected => "Відмовив(-ла)",
        _ => "Очікується підпис",
    };

    /// <summary>
    /// Returns the name of a signing mode.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <returns>The text.</returns>
    public static string Text(SigningMode mode) => mode == SigningMode.Sequential ? "Послідовно" : "Паралельно";

    /// <summary>
    /// Returns the name of a signature format.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>The text.</returns>
    public static string Text(SignatureFormat format) => format switch
    {
        SignatureFormat.XAdES => "XAdES",
        SignatureFormat.PAdES => "PAdES",
        SignatureFormat.AsicS => "ASiC-S",
        SignatureFormat.AsicE => "ASiC-E",
        _ => "CAdES",
    };

    /// <summary>
    /// Returns the name of a signature packaging.
    /// </summary>
    /// <param name="packaging">The packaging.</param>
    /// <returns>The text.</returns>
    public static string Text(SignaturePackaging packaging) => packaging == SignaturePackaging.Enveloped ? "з даними (внутрішній)" : "окремо від даних (зовнішній)";

    /// <summary>
    /// Returns the name of a key storage type.
    /// </summary>
    /// <param name="media">The key storage type.</param>
    /// <returns>The text.</returns>
    public static string Text(KeyMediaType media) => media switch
    {
        KeyMediaType.Hardware => "Апаратний носій",
        KeyMediaType.Cloud => "Хмарний ключ",
        _ => "Файловий ключ",
    };

    /// <summary>
    /// Returns the name of a unit type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The text.</returns>
    public static string Text(OrganizationUnitType type) => type switch
    {
        OrganizationUnitType.University => "Заклад вищої освіти",
        OrganizationUnitType.Institute => "Навчально-науковий інститут",
        OrganizationUnitType.Faculty => "Факультет",
        OrganizationUnitType.Department => "Кафедра",
        OrganizationUnitType.Division => "Відділ / служба",
        OrganizationUnitType.Laboratory => "Лабораторія / центр",
        _ => "Відокремлений підрозділ",
    };

    /// <summary>
    /// Returns the icon of a unit type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The icon name.</returns>
    public static string IconOf(OrganizationUnitType type) => type switch
    {
        OrganizationUnitType.University => "landmark",
        OrganizationUnitType.Institute or OrganizationUnitType.Faculty => "graduation-cap",
        OrganizationUnitType.Department => "book-open",
        OrganizationUnitType.Laboratory => "flask-conical",
        OrganizationUnitType.Branch => "building-2",
        _ => "briefcase",
    };

    /// <summary>
    /// Returns the name of a news category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The text.</returns>
    public static string Text(NewsCategory category) => category switch
    {
        NewsCategory.Announcement => "Оголошення",
        NewsCategory.Event => "Подія",
        NewsCategory.Education => "Освітній процес",
        NewsCategory.Science => "Наука",
        _ => "Новини",
    };

    /// <summary>
    /// Returns the name of a module.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>The text.</returns>
    public static string Text(AppModule module) => module switch
    {
        AppModule.Documents => "Документообіг",
        AppModule.Signing => "Електронний підпис",
        AppModule.News => "Новини",
        AppModule.Structure => "Структура",
        AppModule.Users => "Користувачі",
        AppModule.Administration => "Адміністрування",
        _ => "Аудит",
    };

    /// <summary>
    /// Returns the icon of a module.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>The icon name.</returns>
    public static string IconOf(AppModule module) => module switch
    {
        AppModule.Documents => "file-text",
        AppModule.Signing => "signature",
        AppModule.News => "newspaper",
        AppModule.Structure => "network",
        AppModule.Users => "users",
        AppModule.Administration => "shield",
        _ => "history",
    };

    /// <summary>
    /// Returns the name of an account state filter.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <returns>The text.</returns>
    public static string Text(UserStatusFilter filter) => filter switch
    {
        UserStatusFilter.Active => "Активні",
        UserStatusFilter.Blocked => "Заблоковані",
        _ => "Усі",
    };

    /// <summary>
    /// Returns a description of an audit action code.
    /// </summary>
    /// <param name="action">The action code.</param>
    /// <returns>The text.</returns>
    public static string AuditAction(string action) => action switch
    {
        "auth.sign-in" => "Вхід у систему",
        "auth.sign-in-failed" => "Невдала спроба входу",
        "auth.locked-out" => "Блокування після спроб входу",
        "auth.sign-out" => "Вихід із системи",
        "auth.authenticator-enrolled" => "Підключено автентифікатор",
        "auth.authenticator-reset" => "Скинуто автентифікатор",
        "auth.recovery-codes" => "Нові резервні коди",
        "auth.password-changed" => "Зміна пароля",
        "auth.password-reset" => "Скидання пароля",
        "auth.session-revoked" => "Завершення сесій",
        "users.created" => "Створено користувача",
        "users.updated" => "Змінено користувача",
        "users.status-changed" => "Зміна статусу користувача",
        "users.imported" => "Імпорт користувачів",
        "users.exported" => "Експорт користувачів",
        "roles.changed" => "Зміна ролі",
        "roles.deleted" => "Видалення ролі",
        "structure.changed" => "Зміна структури",
        "documents.created" => "Створено документ",
        "documents.updated" => "Документ змінено",
        "documents.submitted" => "Документ на підписання",
        "documents.withdrawn" => "Документ відкликано",
        "documents.signed" => "Документ підписано",
        "documents.rejected" => "Відмова в підписанні",
        "documents.archived" => "Документ в архіві",
        "documents.deleted" => "Документ видалено",
        "news.changed" => "Зміна новини",
        "news.deleted" => "Видалення новини",
        _ => action,
    };
}
