using System.Globalization;
using ODC.Domain.Localization;
using ODC.Domain.Users;

namespace ODC.Application.Common.Email;

/// <summary>
/// E-mails about the account itself: the invitation of a new account, the notice of a password reset and the one-time
/// activation code. None of them carries a password; the owner chooses one on the activation page.
/// </summary>
public static class AccountEmails
{
    /// <summary>
    /// The path of the activation page in the client application.
    /// </summary>
    public const string ActivationPath = "/account/activate";

    /// <summary>
    /// Builds the invitation to activate a new account.
    /// </summary>
    /// <param name="user">The new user.</param>
    /// <param name="siteUrl">The public address of the cabinet.</param>
    /// <returns>The e-mail in the user's language.</returns>
    public static EmailMessage Invitation(User user, string siteUrl)
    {
        ArgumentNullException.ThrowIfNull(user);
        return EmailLayout.InLanguage(user.Language, () => EmailLayout.Render(user.Email!, user.FullName, new EmailContent(
            Texts.T("Ваш обліковий запис у КОД створено"),
            Texts.T("Запрошення"),
            Texts.T("Вітаємо, {0}!", user.FirstName),
            Texts.T("Для Вас створено обліковий запис у Кабінеті організаційних документів. Активуйте його: підтвердьте пошту одноразовим кодом, придумайте пароль, підключіть автентифікатор і додайте фото."),
            [new(Texts.T("Логін"), user.Email!)],
            null,
            Texts.T("Активувати обліковий запис"),
            ActivationUrl(siteUrl),
            Texts.T("Ви отримали цей лист, бо адміністратор створив для Вас обліковий запис у КОД. Пароль ніхто, крім Вас, не знає і не надсилає. Якщо Ви не очікували цього листа, просто проігноруйте його."))));
    }

    /// <summary>
    /// Builds the notice that an administrator reset the password.
    /// </summary>
    /// <param name="user">The user.</param>
    /// <param name="siteUrl">The public address of the cabinet.</param>
    /// <returns>The e-mail in the user's language.</returns>
    public static EmailMessage PasswordReset(User user, string siteUrl)
    {
        ArgumentNullException.ThrowIfNull(user);
        return EmailLayout.InLanguage(user.Language, () => EmailLayout.Render(user.Email!, user.FullName, new EmailContent(
            Texts.T("Пароль до КОД скинуто"),
            Texts.T("Безпека"),
            Texts.T("Задайте новий пароль"),
            Texts.T("Адміністратор скинув пароль Вашого облікового запису, усі входи завершено. Щоб знову увійти, пройдіть активацію: підтвердьте пошту одноразовим кодом і придумайте новий пароль."),
            [new(Texts.T("Логін"), user.Email!)],
            null,
            Texts.T("Задати новий пароль"),
            ActivationUrl(siteUrl),
            Texts.T("Якщо Ви не просили скинути пароль, зверніться до адміністратора."))));
    }

    /// <summary>
    /// Builds the e-mail with a one-time activation code.
    /// </summary>
    /// <param name="user">The user who activates the account.</param>
    /// <param name="code">The code.</param>
    /// <param name="lifetime">How long the code is valid.</param>
    /// <param name="siteUrl">The public address of the cabinet.</param>
    /// <returns>The e-mail in the user's language.</returns>
    public static EmailMessage ActivationCode(User user, string code, TimeSpan lifetime, string siteUrl)
    {
        ArgumentNullException.ThrowIfNull(user);
        var minutes = ((int)Math.Ceiling(lifetime.TotalMinutes)).ToString(CultureInfo.InvariantCulture);

        // The code stays out of the subject, which lock screens and mail previews show to anyone nearby.
        return EmailLayout.InLanguage(user.Language, () => EmailLayout.Render(user.Email!, user.FullName, new EmailContent(
            Texts.T("Код активації облікового запису КОД"),
            Texts.T("Код підтвердження"),
            Texts.T("Ваш код активації"),
            Texts.T("Введіть цей код на сторінці активації, щоб підтвердити, що пошта належить Вам. Код діє {0} хв і підходить лише один раз.", minutes),
            [],
            null,
            Texts.T("Відкрити сторінку активації"),
            ActivationUrl(siteUrl),
            Texts.T("Нікому не повідомляйте цей код: працівники КОД ніколи його не запитують. Якщо Ви не починали активацію, просто проігноруйте лист — без коду обліковим записом ніхто не скористається."),
            code)));
    }

    /// <summary>
    /// Builds the address of the activation page.
    /// </summary>
    /// <param name="siteUrl">The public address of the cabinet.</param>
    /// <returns>The address.</returns>
    private static string ActivationUrl(string siteUrl) => siteUrl.TrimEnd('/') + ActivationPath;
}
