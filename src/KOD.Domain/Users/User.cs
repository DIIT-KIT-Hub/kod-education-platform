using KOD.Domain.Access;
using KOD.Domain.Common;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Identity;

namespace KOD.Domain.Users;

/// <summary>
/// A person who can sign in to the cabinet: a staff member, an external user or an administrator.
/// Accounts are created only by administrators (there is no self-registration).
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
    /// The positions held by the user.
    /// </summary>
    private readonly List<StaffPosition> _positions = [];

    /// <summary>
    /// The individual permission adjustments.
    /// </summary>
    private readonly List<UserPermissionOverride> _permissionOverrides = [];

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
        MustChangePassword = true;
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
    public string FullName => MiddleName is null ? $"{LastName} {FirstName}" : $"{LastName} {FirstName} {MiddleName}";

    /// <summary>
    /// Gets the short name with initials: «Прізвище І. П.».
    /// </summary>
    public string ShortName => MiddleName is null
        ? $"{LastName} {FirstName[0]}."
        : $"{LastName} {FirstName[0]}. {MiddleName[0]}.";

    /// <summary>
    /// Gets a value indicating whether the account may sign in.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether the user must replace a temporary password at the next sign-in.
    /// </summary>
    public bool MustChangePassword { get; private set; }

    /// <summary>
    /// Gets the moment of the last successful sign-in.
    /// </summary>
    public DateTimeOffset? LastSignInAt { get; private set; }

    /// <summary>
    /// Gets the chosen interface language (<see cref="Languages"/>), or <see langword="null"/> when the user has not chosen one.
    /// </summary>
    public string? Language { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets the positions held by the user.
    /// </summary>
    public IReadOnlyCollection<StaffPosition> Positions => _positions;

    /// <summary>
    /// Gets the individual permission adjustments.
    /// </summary>
    public IReadOnlyCollection<UserPermissionOverride> PermissionOverrides => _permissionOverrides;

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
    /// Marks whether the user must change the password at the next sign-in.
    /// </summary>
    /// <param name="mustChange">The new value.</param>
    public void RequirePasswordChange(bool mustChange) => MustChangePassword = mustChange;

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
    /// Replaces the positions held by the user.
    /// </summary>
    /// <param name="positions">The new positions (unit, role, title, primary flag).</param>
    public void SetPositions(IEnumerable<(Guid UnitId, Guid RoleId, string? Title, bool IsPrimary)> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        var list = positions.DistinctBy(static p => (p.UnitId, p.RoleId)).ToList();
        if (list.Count(static p => p.IsPrimary) > 1)
        {
            throw new DomainException(Texts.T("Основною може бути лише одна посада."));
        }

        _positions.RemoveAll(existing => !list.Exists(p => p.UnitId == existing.OrganizationUnitId && p.RoleId == existing.RoleId));
        foreach (var (unitId, roleId, title, isPrimary) in list)
        {
            var existing = _positions.Find(p => p.OrganizationUnitId == unitId && p.RoleId == roleId);
            if (existing is null)
            {
                _positions.Add(new StaffPosition(Id, unitId, roleId, title, isPrimary));
            }
            else
            {
                existing.Update(title, isPrimary);
            }
        }

        if (_positions.Count > 0 && !_positions.Exists(static p => p.IsPrimary))
        {
            _positions[0].Update(_positions[0].Title, isPrimary: true);
        }
    }

    /// <summary>
    /// Replaces the individual permission adjustments.
    /// </summary>
    /// <param name="granted">Permissions granted in addition to the roles.</param>
    /// <param name="denied">Permissions denied regardless of the roles.</param>
    public void SetPermissionOverrides(IEnumerable<string> granted, IEnumerable<string> denied)
    {
        ArgumentNullException.ThrowIfNull(granted);
        ArgumentNullException.ThrowIfNull(denied);

        var grantSet = granted.ToHashSet(StringComparer.Ordinal);
        var denySet = denied.ToHashSet(StringComparer.Ordinal);
        if (grantSet.Overlaps(denySet))
        {
            throw new DomainException(Texts.T("Дозвіл не може бути одночасно наданий і заборонений."));
        }

        var unknown = grantSet.Concat(denySet).FirstOrDefault(static p => !Permissions.Codes.Contains(p));
        if (unknown is not null)
        {
            throw new DomainException(Texts.T("Невідомий дозвіл «{0}».", unknown));
        }

        _permissionOverrides.Clear();
        _permissionOverrides.AddRange(grantSet.Select(p => new UserPermissionOverride(Id, p, isGranted: true)));
        _permissionOverrides.AddRange(denySet.Select(p => new UserPermissionOverride(Id, p, isGranted: false)));
    }
}
