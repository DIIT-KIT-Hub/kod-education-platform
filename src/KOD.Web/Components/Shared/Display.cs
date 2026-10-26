using System.Globalization;
using KOD.Application.Features.Users;
using KOD.Domain.Access;
using KOD.Domain.Documents;
using KOD.Domain.Localization;
using KOD.Domain.News;
using KOD.Web.DesignSystem.Components;

namespace KOD.Web.Components.Shared;

/// <summary>
/// User-facing texts (in the interface language), icons and tones for domain values, plus date formatting in Kyiv time.
/// </summary>
public static class Display
{
    /// <summary>
    /// The time zone of the cabinet (Kyiv).
    /// </summary>
    private static readonly TimeZoneInfo Kyiv = KOD.Application.Common.Text.KyivTime.Zone;

    /// <summary>
    /// Gets the culture of the current request (dates, numbers).
    /// </summary>
    public static CultureInfo Culture => CultureInfo.CurrentCulture;

    /// <summary>
    /// Formats a moment as a local date and time (e.g. «25.10.2026, 14:05», «25 Oct 2026, 14:05»).
    /// </summary>
    /// <param name="moment">The moment.</param>
    /// <returns>The text, or «—» when empty.</returns>
    public static string DateTime(DateTimeOffset? moment)
        => moment is { } value ? TimeZoneInfo.ConvertTime(value, Kyiv).ToString(Languages.Current == Languages.English ? "d MMM yyyy, HH:mm" : "dd.MM.yyyy, HH:mm", Culture) : "—";

    /// <summary>
    /// Formats a moment as a local date (e.g. «25 жовтня 2026», «25 October 2026»).
    /// </summary>
    /// <param name="moment">The moment.</param>
    /// <returns>The text, or «—» when empty.</returns>
    public static string Date(DateTimeOffset? moment)
        => moment is { } value ? TimeZoneInfo.ConvertTime(value, Kyiv).ToString("d MMMM yyyy", Culture) : "—";

    /// <summary>
    /// Formats a size in bytes («1,4 МБ», «1.4 MB»).
    /// </summary>
    /// <param name="bytes">The size.</param>
    /// <returns>The text.</returns>
    public static string FileSize(long bytes) => bytes switch
    {
        < 1024 => Texts.T("{0} Б", bytes),
        < 1024 * 1024 => Texts.T("{0:0.#} КБ", bytes / 1024d),
        _ => Texts.T("{0:0.#} МБ", bytes / 1024d / 1024d),
    };

    /// <summary>
    /// Returns the name of a document kind.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The text.</returns>
    public static string Text(DocumentKind kind) => kind switch
    {
        DocumentKind.Order => Texts.In("document-kind", "Наказ"),
        DocumentKind.Directive => Texts.In("document-kind", "Розпорядження"),
        DocumentKind.Memo => Texts.In("document-kind", "Службова записка"),
        DocumentKind.Application => Texts.In("document-kind", "Заява"),
        DocumentKind.Report => Texts.In("document-kind", "Звіт"),
        DocumentKind.Minutes => Texts.In("document-kind", "Протокол"),
        DocumentKind.Contract => Texts.In("document-kind", "Договір"),
        DocumentKind.Certificate => Texts.In("document-kind", "Довідка"),
        DocumentKind.Regulation => Texts.In("document-kind", "Положення / інструкція"),
        DocumentKind.Letter => Texts.In("document-kind", "Лист"),
        DocumentKind.Act => Texts.In("document-kind", "Акт"),
        _ => Texts.In("document-kind", "Інший документ"),
    };

    /// <summary>
    /// Returns the name of a document state.
    /// </summary>
    /// <param name="status">The state.</param>
    /// <returns>The text.</returns>
    public static string Text(DocumentStatus status) => status switch
    {
        DocumentStatus.Draft => Texts.T("Чернетка"),
        DocumentStatus.InSigning => Texts.T("На підписанні"),
        DocumentStatus.Signed => Texts.T("Підписано"),
        DocumentStatus.Rejected => Texts.T("Відхилено"),
        _ => Texts.T("В архіві"),
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
        SignerStatus.Signed => Texts.T("Підписав(-ла)"),
        SignerStatus.Rejected => Texts.T("Відмовив(-ла)"),
        _ => Texts.T("Очікується підпис"),
    };

    /// <summary>
    /// Returns the name of a signing mode.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <returns>The text.</returns>
    public static string Text(SigningMode mode) => mode == SigningMode.Sequential ? Texts.T("Послідовно") : Texts.T("Паралельно");

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
    public static string Text(SignaturePackaging packaging) => packaging == SignaturePackaging.Enveloped ? Texts.T("з даними (внутрішній)") : Texts.T("окремо від даних (зовнішній)");

    /// <summary>
    /// Returns the name of a key storage type.
    /// </summary>
    /// <param name="media">The key storage type.</param>
    /// <returns>The text.</returns>
    public static string Text(KeyMediaType media) => media switch
    {
        KeyMediaType.Hardware => Texts.T("Апаратний носій"),
        KeyMediaType.Cloud => Texts.T("Хмарний ключ"),
        _ => Texts.T("Файловий ключ"),
    };

    /// <summary>
    /// Returns the name of a news category.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The text.</returns>
    public static string Text(NewsCategory category) => category switch
    {
        NewsCategory.Announcement => Texts.T("Оголошення"),
        NewsCategory.Event => Texts.T("Подія"),
        NewsCategory.Training => Texts.T("Навчання та розвиток"),
        NewsCategory.Projects => Texts.T("Проєкти та досягнення"),
        _ => Texts.T("Новини"),
    };

    /// <summary>
    /// Returns the name of a module.
    /// </summary>
    /// <param name="module">The module.</param>
    /// <returns>The text.</returns>
    public static string Text(AppModule module) => module switch
    {
        AppModule.Documents => Texts.T("Документообіг"),
        AppModule.Signing => Texts.T("Електронний підпис"),
        AppModule.News => Texts.T("Новини"),
        AppModule.Structure => Texts.T("Структура"),
        AppModule.Users => Texts.T("Користувачі"),
        AppModule.Administration => Texts.T("Адміністрування"),
        _ => Texts.T("Аудит"),
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
        UserStatusFilter.Active => Texts.T("Активні"),
        UserStatusFilter.Blocked => Texts.T("Заблоковані"),
        _ => Texts.T("Усі"),
    };

    /// <summary>
    /// Returns a description of an audit action code.
    /// </summary>
    /// <param name="action">The action code.</param>
    /// <returns>The text.</returns>
    public static string AuditAction(string action) => action switch
    {
        "auth.sign-in" => Texts.In("audit", "Вхід у систему"),
        "auth.sign-in-failed" => Texts.In("audit", "Невдала спроба входу"),
        "auth.locked-out" => Texts.In("audit", "Блокування після спроб входу"),
        "auth.sign-out" => Texts.In("audit", "Вихід із системи"),
        "auth.authenticator-enrolled" => Texts.In("audit", "Підключено автентифікатор"),
        "auth.authenticator-reset" => Texts.In("audit", "Скинуто автентифікатор"),
        "auth.recovery-codes" => Texts.In("audit", "Нові резервні коди"),
        "auth.password-changed" => Texts.In("audit", "Зміна пароля"),
        "auth.password-reset" => Texts.In("audit", "Скидання пароля"),
        "auth.session-revoked" => Texts.In("audit", "Завершення сесій"),
        "users.created" => Texts.In("audit", "Створено користувача"),
        "users.updated" => Texts.In("audit", "Змінено користувача"),
        "users.status-changed" => Texts.In("audit", "Зміна статусу користувача"),
        "users.imported" => Texts.In("audit", "Імпорт користувачів"),
        "users.exported" => Texts.In("audit", "Експорт користувачів"),
        "roles.changed" => Texts.In("audit", "Зміна ролі"),
        "roles.deleted" => Texts.In("audit", "Видалення ролі"),
        "structure.changed" => Texts.In("audit", "Зміна структури"),
        "structure.type-changed" => Texts.In("audit", "Зміна типу підрозділу"),
        "structure.type-deleted" => Texts.In("audit", "Видалення типу підрозділу"),
        "documents.created" => Texts.In("audit", "Створено документ"),
        "documents.updated" => Texts.In("audit", "Документ змінено"),
        "documents.submitted" => Texts.In("audit", "Документ на підписання"),
        "documents.withdrawn" => Texts.In("audit", "Документ відкликано"),
        "documents.signed" => Texts.In("audit", "Документ підписано"),
        "documents.rejected" => Texts.In("audit", "Відмова в підписанні"),
        "documents.archived" => Texts.In("audit", "Документ в архіві"),
        "documents.deleted" => Texts.In("audit", "Документ видалено"),
        "news.changed" => Texts.In("audit", "Зміна новини"),
        "news.deleted" => Texts.In("audit", "Видалення новини"),
        _ => action,
    };
}
