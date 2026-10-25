using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Results;
using KOD.Application.Features.Documents;
using KOD.DesignSystem.Components;
using KOD.Domain.Documents;
using Microsoft.AspNetCore.Components;

namespace KOD.Web.Components.Pages.Documents;

/// <summary>
/// A document card: file, details, signing route, signatures and the actions available to the current user.
/// Actions are plain form posts (Post/Redirect/Get); signing runs in the browser.
/// </summary>
public partial class DocumentDetailsPage
{
    /// <summary>
    /// Gets or sets the document identifier.
    /// </summary>
    [Parameter]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the rejection form model.
    /// </summary>
    [SupplyParameterFromForm(FormName = "document-reject")]
    private RejectModel Reject { get; set; } = default!;

    /// <summary>
    /// Gets or sets the document service.
    /// </summary>
    [Inject]
    private IDocumentService Documents { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the document.
    /// </summary>
    private DocumentDetails? Document { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        Reject ??= new RejectModel();
        var result = await Documents.GetAsync(Id, CancellationToken.None);
        Document = result.IsSuccess ? result.Value : null;
        ErrorMessage = result.IsSuccess ? null : result.Error.Message;
    }

    /// <summary>
    /// Maps a signer state to a timeline state.
    /// </summary>
    /// <param name="signer">The signer.</param>
    /// <returns>The timeline state.</returns>
    private TimelineState StateOf(DocumentSignerItem signer) => signer.Status switch
    {
        SignerStatus.Signed => TimelineState.Done,
        SignerStatus.Rejected => TimelineState.Failed,
        _ when Document?.Status == DocumentStatus.InSigning => TimelineState.Current,
        _ => TimelineState.Pending,
    };

    /// <summary>
    /// Sends the document for signing.
    /// </summary>
    /// <returns>A task that completes when the action is processed.</returns>
    private async Task SubmitAsync() => Complete(await Documents.SubmitAsync(Id, CancellationToken.None), "Документ надіслано на підписання");

    /// <summary>
    /// Returns the document to drafts.
    /// </summary>
    /// <returns>A task that completes when the action is processed.</returns>
    private async Task WithdrawAsync() => Complete(await Documents.WithdrawAsync(Id, CancellationToken.None), "Документ повернуто в чернетки");

    /// <summary>
    /// Archives the document.
    /// </summary>
    /// <returns>A task that completes when the action is processed.</returns>
    private async Task ArchiveAsync() => Complete(await Documents.ArchiveAsync(Id, CancellationToken.None), "Документ переміщено в архів");

    /// <summary>
    /// Deletes the document.
    /// </summary>
    /// <returns>A task that completes when the action is processed.</returns>
    private async Task DeleteAsync()
    {
        var result = await Documents.DeleteAsync(Id, CancellationToken.None);
        if (result.IsSuccess)
        {
            Navigation.NavigateTo("/documents?scope=Authored&toast=" + Uri.EscapeDataString("Документ видалено"));
            return;
        }

        ErrorMessage = result.Error.Message;
    }

    /// <summary>
    /// Declines to sign the document.
    /// </summary>
    /// <returns>A task that completes when the action is processed.</returns>
    private async Task RejectAsync() => Complete(await Documents.RejectAsync(Id, Reject.Reason, CancellationToken.None), "Відмову зафіксовано");

    /// <summary>
    /// Redirects to the document with a toast on success or shows the error.
    /// </summary>
    /// <param name="result">The action result.</param>
    /// <param name="message">The success message.</param>
    private void Complete(Result result, string message)
    {
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/documents/{Id}?toast={Uri.EscapeDataString(message)}");
            return;
        }

        ErrorMessage = result.Error.Message;
    }

    /// <summary>
    /// The rejection form.
    /// </summary>
    private sealed class RejectModel
    {
        /// <summary>
        /// Gets or sets the reason.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть причину відмови.")]
        [StringLength(DocumentSigner.CommentMaxLength, ErrorMessage = "Причина не може бути довшою за 1000 символів.")]
        public string Reason { get; set; } = string.Empty;
    }
}
