using KOD.Domain.Localization;
using Microsoft.AspNetCore.Identity;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Messages for ASP.NET Core Identity validation errors in the interface language (Ukrainian source texts translated by <see cref="Texts"/>).
/// </summary>
internal sealed class UkrainianIdentityErrorDescriber : IdentityErrorDescriber
{
    /// <inheritdoc />
    public override IdentityError DefaultError() => Describe(nameof(DefaultError), Texts.T("Сталася невідома помилка."));

    /// <inheritdoc />
    public override IdentityError ConcurrencyFailure() => Describe(nameof(ConcurrencyFailure), Texts.T("Дані змінено іншим користувачем, спробуйте ще раз."));

    /// <inheritdoc />
    public override IdentityError PasswordMismatch() => Describe(nameof(PasswordMismatch), Texts.T("Неправильний поточний пароль."));

    /// <inheritdoc />
    public override IdentityError InvalidToken() => Describe(nameof(InvalidToken), Texts.T("Недійсний або прострочений код."));

    /// <inheritdoc />
    public override IdentityError InvalidEmail(string? email) => Describe(nameof(InvalidEmail), Texts.T("Адреса «{0}» некоректна.", email));

    /// <inheritdoc />
    public override IdentityError InvalidUserName(string? userName) => Describe(nameof(InvalidUserName), Texts.T("Логін «{0}» некоректний.", userName));

    /// <inheritdoc />
    public override IdentityError DuplicateEmail(string email) => Describe(nameof(DuplicateEmail), Texts.T("Адреса «{0}» вже використовується.", email));

    /// <inheritdoc />
    public override IdentityError DuplicateUserName(string userName) => Describe(nameof(DuplicateUserName), Texts.T("Логін «{0}» вже використовується.", userName));

    /// <inheritdoc />
    public override IdentityError PasswordTooShort(int length) => Describe(nameof(PasswordTooShort), Texts.T("Пароль має містити щонайменше {0} символів.", length));

    /// <inheritdoc />
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Describe(nameof(PasswordRequiresUniqueChars), Texts.T("Пароль має містити щонайменше {0} різних символів.", uniqueChars));

    /// <inheritdoc />
    public override IdentityError PasswordRequiresNonAlphanumeric() => Describe(nameof(PasswordRequiresNonAlphanumeric), Texts.T("Пароль має містити спеціальний символ."));

    /// <inheritdoc />
    public override IdentityError PasswordRequiresDigit() => Describe(nameof(PasswordRequiresDigit), Texts.T("Пароль має містити цифру."));

    /// <inheritdoc />
    public override IdentityError PasswordRequiresLower() => Describe(nameof(PasswordRequiresLower), Texts.T("Пароль має містити малу латинську літеру."));

    /// <inheritdoc />
    public override IdentityError PasswordRequiresUpper() => Describe(nameof(PasswordRequiresUpper), Texts.T("Пароль має містити велику латинську літеру."));

    /// <inheritdoc />
    public override IdentityError RecoveryCodeRedemptionFailed() => Describe(nameof(RecoveryCodeRedemptionFailed), Texts.T("Резервний код недійсний."));

    /// <summary>
    /// Creates an error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The user-facing description.</param>
    /// <returns>The error.</returns>
    private static IdentityError Describe(string code, string description) => new() { Code = code, Description = description };
}
