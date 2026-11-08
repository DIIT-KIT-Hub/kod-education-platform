using ODC.Web.Shared.Models;

namespace ODC.Web.Features.News;

/// <summary>
/// Where the user may publish news.
/// </summary>
/// <param name="Organizations">The organizations whose news the user manages.</param>
/// <param name="CabinetWide">Whether the user may also publish for the whole cabinet (no organization).</param>
public sealed record NewsOrganizationsResponse(IReadOnlyList<UnitOption> Organizations, bool CabinetWide);
