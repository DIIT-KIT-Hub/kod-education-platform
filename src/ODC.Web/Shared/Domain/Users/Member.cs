using ODC.Web.Shared.Domain.Access;
using ODC.Web.Shared.Kernel;
using ODC.Web.Shared.Localization;

namespace ODC.Web.Shared.Domain.Users;

/// <summary>
/// A person as the database of an organization knows them: a copy of the account's profile (the account itself, its
/// password and sign-in data live in the master database, <see cref="User"/>), the positions held in the organization
/// and the individual permission adjustments. Authors, signers, chat members and notification actors of the
/// organization's data refer to members, so the organization's queries never need the master database.
/// </summary>
/// <remarks>
/// The profile is copied from the account whenever the account changes (<c>MemberSync</c>); never edit it here.
/// <see cref="Id"/> equals the account's identifier.
/// </remarks>
public sealed class Member
{
    /// <summary>
    /// The positions held in the organization.
    /// </summary>
    private readonly List<StaffPosition> _positions = [];

    /// <summary>
    /// The individual permission adjustments.
    /// </summary>
    private readonly List<UserPermissionOverride> _permissionOverrides = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Member"/> class from an account's profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    public Member(MemberProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Id = profile.Id;
        Email = profile.Email;
        LastName = profile.LastName;
        FirstName = profile.FirstName;
        Copy(profile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Member"/> class for EF Core materialization.
    /// </summary>
    private Member()
    {
        Email = string.Empty;
        LastName = string.Empty;
        FirstName = string.Empty;
    }

    /// <summary>
    /// Gets the identifier (the account's identifier).
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// Gets the e-mail address.
    /// </summary>
    public string Email { get; private set; }

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
    /// Gets the contact phone number.
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// Gets the profile photo.
    /// </summary>
    public Guid? PhotoId { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the account may sign in.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the account waits for its owner to activate it.
    /// </summary>
    public bool AwaitsActivation { get; private set; }

    /// <summary>
    /// Gets the last sign-in (lists sort by it).
    /// </summary>
    public DateTimeOffset? LastSignInAt { get; private set; }

    /// <summary>
    /// Gets the full name: «Прізвище Ім'я По батькові».
    /// </summary>
    public string FullName => PersonName.Full(LastName, FirstName, MiddleName);

    /// <summary>
    /// Gets the short name with initials: «Прізвище І. П.».
    /// </summary>
    public string ShortName => PersonName.Short(LastName, FirstName, MiddleName);

    /// <summary>
    /// Gets the positions held in the organization.
    /// </summary>
    public IReadOnlyCollection<StaffPosition> Positions => _positions;

    /// <summary>
    /// Gets the individual permission adjustments.
    /// </summary>
    public IReadOnlyCollection<UserPermissionOverride> PermissionOverrides => _permissionOverrides;

    /// <summary>
    /// Takes the current profile of the account.
    /// </summary>
    /// <param name="profile">The profile.</param>
    /// <returns><see langword="true"/> when something changed.</returns>
    public bool Copy(MemberProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (profile.Id != Id)
        {
            throw new InvalidOperationException("The profile belongs to another account.");
        }

        var changed = Email != profile.Email
            || LastName != profile.LastName
            || FirstName != profile.FirstName
            || MiddleName != profile.MiddleName
            || PhoneNumber != profile.PhoneNumber
            || PhotoId != profile.PhotoId
            || IsActive != profile.IsActive
            || AwaitsActivation != profile.AwaitsActivation
            || LastSignInAt != profile.LastSignInAt;
        Email = profile.Email;
        LastName = profile.LastName;
        FirstName = profile.FirstName;
        MiddleName = profile.MiddleName;
        PhoneNumber = profile.PhoneNumber;
        PhotoId = profile.PhotoId;
        IsActive = profile.IsActive;
        AwaitsActivation = profile.AwaitsActivation;
        LastSignInAt = profile.LastSignInAt;
        return changed;
    }

    /// <summary>
    /// Replaces the positions held in the organization.
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
