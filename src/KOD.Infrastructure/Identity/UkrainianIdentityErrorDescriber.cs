using Microsoft.AspNetCore.Identity;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Ukrainian messages for ASP.NET Core Identity validation errors.
/// </summary>
internal sealed class UkrainianIdentityErrorDescriber : IdentityErrorDescriber
{
    /// <inheritdoc />
    public override IdentityError DefaultError() => Describe(nameof(DefaultError), "Сталася невідома помилка.");

    /// <inheritdoc />
    public override IdentityError ConcurrencyFailure() => Describe(nameof(ConcurrencyFailure), "Дані змінено іншим користувачем, спробуйте ще раз.");

    /// <inheritdoc />
    public override IdentityError PasswordMismatch() => Describe(nameof(PasswordMismatch), "Неправильний поточний пароль.");

    /// <inheritdoc />
    public override IdentityError InvalidToken() => Describe(nameof(InvalidToken), "Недійсний або прострочений код.");

    /// <inheritdoc />
    public override IdentityError InvalidEmail(string? email) => Describe(nameof(InvalidEmail), $"Адреса «{email}» некоректна.");

    /// <inheritdoc />
    public override IdentityError InvalidUserName(string? userName) => Describe(nameof(InvalidUserName), $"Логін «{userName}» некоректний.");

    /// <inheritdoc />
    public override IdentityError DuplicateEmail(string email) => Describe(nameof(DuplicateEmail), $"Адреса «{email}» вже використовується.");

    /// <inheritdoc />
    public override IdentityError DuplicateUserName(string userName) => Describe(nameof(DuplicateUserName), $"Логін «{userName}» вже використовується.");

    /// <inheritdoc />
    public override IdentityError PasswordTooShort(int length) => Describe(nameof(PasswordTooShort), $"Пароль має містити щонайменше {length} символів.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Describe(nameof(PasswordRequiresUniqueChars), $"Пароль має містити щонайменше {uniqueChars} різних символів.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresNonAlphanumeric() => Describe(nameof(PasswordRequiresNonAlphanumeric), "Пароль має містити спеціальний символ.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresDigit() => Describe(nameof(PasswordRequiresDigit), "Пароль має містити цифру.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresLower() => Describe(nameof(PasswordRequiresLower), "Пароль має містити малу латинську літеру.");

    /// <inheritdoc />
    public override IdentityError PasswordRequiresUpper() => Describe(nameof(PasswordRequiresUpper), "Пароль має містити велику латинську літеру.");

    /// <inheritdoc />
    public override IdentityError RecoveryCodeRedemptionFailed() => Describe(nameof(RecoveryCodeRedemptionFailed), "Резервний код недійсний.");

    /// <summary>
    /// Creates an error.
    /// </summary>
    /// <param name="code">The error code.</param>
    /// <param name="description">The user-facing description.</param>
    /// <returns>The error.</returns>
    private static IdentityError Describe(string code, string description) => new() { Code = code, Description = description };
}
