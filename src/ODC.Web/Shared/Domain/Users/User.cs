using Microsoft.AspNetCore.Identity;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Shared.Domain.Users;

/// <summary>
/// A person who can sign in to the cabinet: a staff member, an external user or an administrator.
/// Accounts are created only by administrators (there is no self-registration) without a password; the owner activates
/// the account from the sign-in page (<see cref="AwaitsActivation"/>).
/// </summary>
public sealed class User : IdentityUser<Guid>, IAuditable
{
    /// <summary>
    /// Maximum length of name parts.
    /// </summary>
    public const int NamePartMaxLength = 64;

    /// <summary>
    /// Maximum length of an e-mail address.
    /// </summary>
    public const int EmailMaxLength = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="email">The e-mail address, also used as the login.</param>
    /// <param name="lastName">The last name (прізвище).</param>
    /// <param name="firstName">The first name (ім'я).</param>
    /// <param name="middleName">The patronymic (по батькові).</param>
    public User(string email, string lastName, string firstName, string? middleName)
    {
        Id = Guid.CreateVersion7();
        var normalizedEmail = Guard.Text(email, EmailMaxLength, "Електронна пошта").ToLowerInvariant();
        Email = normalizedEmail;
        UserName = normalizedEmail;
        LastName = Guard.Text(lastName, NamePartMaxLength, "Прізвище");
        FirstName = Guard.Text(firstName, NamePartMaxLength, "Ім'я");
        MiddleName = Guard.OptionalText(middleName, NamePartMaxLength, "По батькові");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class for EF Core materialization.
    /// </summary>
    private User()
    {
        LastName = string.Empty;
        FirstName = string.Empty;
    }

    /// <summary>
    /// Gets the last name.
    /// </summary>
    public string LastName { get; private set; }

    /// <summary>
    /// Gets the first name.
    /// </summary>
    public string FirstName { get; private set; }

    /// <summary>
    /// Gets the patronymic.
    /// </summary>
    public string? MiddleName { get; private set; }

    /// <summary>
    /// Gets the full name in the official Ukrainian order: «Прізвище Ім'я По батькові».
    /// </summary>
    public string FullName => PersonName.Full(LastName, FirstName, MiddleName);

    /// <summary>
    /// Gets the short name with initials: «Прізвище І. П.».
    /// </summary>
    public string ShortName => PersonName.Short(LastName, FirstName, MiddleName);

    /// <summary>
    /// Gets a value indicating whether the account may sign in.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether the account waits for its owner to activate it: it has no password yet (a new
    /// account, or an administrator reset the password). The owner proves the e-mail with a one-time code and chooses
    /// a password; nobody else ever knows it.
    /// </summary>
    public bool AwaitsActivation => PasswordHash is null;

    /// <summary>
    /// Gets the moment of the last successful sign-in.
    /// </summary>
    public DateTimeOffset? LastSignInAt { get; private set; }

    /// <summary>
    /// Gets the chosen interface language (<see cref="Languages"/>), or <see langword="null"/> when the user has not chosen one.
    /// </summary>
    public string? Language { get; private set; }

    /// <summary>
    /// Gets the profile photo (a <see cref="Media.MediaFile"/>), or <see langword="null"/> until the user uploads one.
    /// The cabinet cannot be used without a photo.
    /// </summary>
    public Guid? PhotoId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Updates the personal data.
    /// </summary>
    /// <param name="lastName">The last name.</param>
    /// <param name="firstName">The first name.</param>
    /// <param name="middleName">The patronymic.</param>
    /// <param name="phoneNumber">The contact phone number.</param>
    public void UpdatePersonalData(string lastName, string firstName, string? middleName, string? phoneNumber)
    {
        LastName = Guard.Text(lastName, NamePartMaxLength, "Прізвище");
        FirstName = Guard.Text(firstName, NamePartMaxLength, "Ім'я");
        MiddleName = Guard.OptionalText(middleName, NamePartMaxLength, "По батькові");
        PhoneNumber = Guard.OptionalText(phoneNumber, 32, "Телефон");
    }

    /// <summary>
    /// Activates or blocks the account.
    /// </summary>
    /// <param name="isActive">The new state.</param>
    public void SetActive(bool isActive) => IsActive = isActive;

    /// <summary>
    /// Records a successful sign-in.
    /// </summary>
    /// <param name="moment">The sign-in moment.</param>
    public void RecordSignIn(DateTimeOffset moment) => LastSignInAt = moment;

    /// <summary>
    /// Sets the interface language.
    /// </summary>
    /// <param name="language">A supported language code (<see cref="Languages"/>).</param>
    public void SetLanguage(string language)
        => Language = Languages.Normalize(language) ?? throw new DomainException(Texts.T("Непідтримувана мова інтерфейсу."));

    /// <summary>
    /// Sets the profile photo.
    /// </summary>
    /// <param name="photoId">The uploaded avatar image.</param>
    public void SetPhoto(Guid photoId) => PhotoId = photoId;
}
