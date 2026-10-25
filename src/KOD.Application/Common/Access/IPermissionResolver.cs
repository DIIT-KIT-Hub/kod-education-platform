namespace KOD.Application.Common.Access;

/// <summary>
/// Computes the effective permissions of a user.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>
    /// Returns (roles of all positions ∪ individual grants) − individual denials.
    /// Holders of the administrator role get every permission.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The effective permission codes.</returns>
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken);
}
