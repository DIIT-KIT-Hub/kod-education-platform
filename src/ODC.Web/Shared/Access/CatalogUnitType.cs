using ODC.Web.Shared.Domain.Organization;
using ODC.Web.Shared.Models;
using ODC.Web.Shared.Text;

namespace ODC.Web.Shared.Access;

/// <summary>
/// A unit type of the master database's catalog, as the organizations' side needs it.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="Code">The stable code.</param>
/// <param name="Name">The stored (untranslated) name.</param>
/// <param name="Icon">The icon.</param>
/// <param name="IsOrganization">Whether units of this type are organizations.</param>
/// <param name="SortOrder">The display order.</param>
internal sealed record CatalogUnitType(Guid Id, string Code, string Name, string Icon, bool IsOrganization, int SortOrder)
{
    /// <summary>
    /// The reference of a unit's type, or a neutral one when the type is not in the catalog.
    /// </summary>
    /// <param name="types">The catalog.</param>
    /// <param name="typeId">The type.</param>
    /// <returns>The reference.</returns>
    public static UnitTypeRef RefOf(IReadOnlyDictionary<Guid, CatalogUnitType> types, Guid typeId)
    {
        ArgumentNullException.ThrowIfNull(types);
        return types.TryGetValue(typeId, out var type) ? type.ToRef() : new UnitTypeRef(typeId, string.Empty, UnitType.DefaultIcon, IsOrganization: false);
    }

    /// <summary>
    /// The reference shown with a unit (the name translated).
    /// </summary>
    /// <returns>The reference.</returns>
    public UnitTypeRef ToRef() => new(Id, StoredText.Display(Name) ?? Name, Icon, IsOrganization);
}
