using ODC.Web.Features.Documents;

namespace ODC.Web.Features.Home;

/// <summary>
/// A document of the home page with the organization it lives in (the home page gathers the documents waiting for the
/// user from all their organizations).
/// </summary>
/// <param name="Document">The document.</param>
/// <param name="OrganizationId">Its organization.</param>
/// <param name="OrganizationCode">The code of its organization (its page is at <c>/o/{code}/documents/{id}</c>).</param>
public sealed record DashboardDocument(DocumentListItem Document, Guid OrganizationId, string OrganizationCode);
