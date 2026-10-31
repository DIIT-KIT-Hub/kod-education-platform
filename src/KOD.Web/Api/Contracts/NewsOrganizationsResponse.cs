using KOD.Application.Common.Models;

namespace KOD.Web.Api.Contracts;

/// <summary>
/// Where the user may publish news.
/// </summary>
/// <param name="Organizations">The organizations whose news the user manages.</param>
/// <param name="CabinetWide">Whether the user may also publish for the whole cabinet (no organization).</param>
public sealed record NewsOrganizationsResponse(IReadOnlyList<UnitOption> Organizations, bool CabinetWide);
