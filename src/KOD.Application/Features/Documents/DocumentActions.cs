namespace KOD.Application.Features.Documents;

/// <summary>
/// The actions the current user may perform on a document.
/// </summary>
/// <param name="CanEdit">Change the details, the route or the file (author; draft or rejected).</param>
/// <param name="CanSubmit">Send for signing.</param>
/// <param name="CanWithdraw">Return to drafts.</param>
/// <param name="CanSign">Sign now.</param>
/// <param name="CanReject">Decline to sign now.</param>
/// <param name="CanArchive">Move to the archive.</param>
/// <param name="CanDelete">Delete.</param>
public sealed record DocumentActions(
    bool CanEdit,
    bool CanSubmit,
    bool CanWithdraw,
    bool CanSign,
    bool CanReject,
    bool CanArchive,
    bool CanDelete);
