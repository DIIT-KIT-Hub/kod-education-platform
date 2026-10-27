using KOD.Application.Common.Models;
using KOD.Domain.Documents;

namespace KOD.Application.Features.Documents;

/// <summary>
/// The current data of a document opened in the editor.
/// </summary>
/// <param name="Id">The identifier.</param>
/// <param name="RegistrationNumber">The registration number.</param>
/// <param name="Title">The title.</param>
/// <param name="Description">The description.</param>
/// <param name="Kind">The kind.</param>
/// <param name="Status">The life-cycle state (draft or rejected).</param>
/// <param name="Unit">The unit (shown even when it is no longer among the author's units).</param>
/// <param name="SigningMode">The signing order.</param>
/// <param name="Signers">The signers in signing order.</param>
/// <param name="File">The current file.</param>
/// <param name="SignedBy">The signers whose signatures on the current file are kept while the file is not replaced.</param>
public sealed record DocumentEditModel(
    Guid Id,
    string RegistrationNumber,
    string Title,
    string? Description,
    DocumentKind Kind,
    DocumentStatus Status,
    UnitOption Unit,
    SigningMode SigningMode,
    IReadOnlyList<UserLookupItem> Signers,
    DocumentFileInfo File,
    IReadOnlyList<Guid> SignedBy);
