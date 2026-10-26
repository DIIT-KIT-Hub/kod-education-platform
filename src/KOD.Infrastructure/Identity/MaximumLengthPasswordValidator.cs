using KOD.Domain.Localization;
using KOD.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KOD.Infrastructure.Identity;

/// <summary>
/// Rejects passwords longer than <see cref="PasswordSecurityOptions.MaximumLength"/> characters.
/// </summary>
/// <param name="options">The options.</param>
internal sealed class MaximumLengthPasswordValidator(IOptions<PasswordSecurityOptions> options) : IPasswordValidator<User>
{
    /// <inheritdoc />
    public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password)
    {
        var limit = options.Value.MaximumLength;
        return Task.FromResult(password is not null && password.Length > limit
            ? IdentityResult.Failed(new IdentityError { Code = "PasswordTooLong", Description = Texts.T("Пароль не може бути довшим за {0} символів.", limit) })
            : IdentityResult.Success);
    }
}
