using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Features.Documents;
using KOD.Domain.Documents;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace KOD.Web.Components.Pages.Documents;

/// <summary>
/// Creates a document: file upload, details and the signing route (interactive server component).
/// </summary>
public sealed partial class DocumentCreate : IDisposable
{
    /// <summary>
    /// Debounces signer search input.
    /// </summary>
    private CancellationTokenSource? _searchCancellation;

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
    /// Gets or sets the form model.
    /// </summary>
    private CreateModel Model { get; set; } = new();

    /// <summary>
    /// Gets or sets the units the user may create documents in.
    /// </summary>
    private IReadOnlyList<UnitOption> Units { get; set; } = [];

    /// <summary>
    /// Gets or sets the chosen file.
    /// </summary>
    private IBrowserFile? SelectedFile { get; set; }

    /// <summary>
    /// Gets or sets the signer search term.
    /// </summary>
    private string SearchTerm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the signer suggestions.
    /// </summary>
    private IReadOnlyList<UserLookupItem> Suggestions { get; set; } = [];

    /// <summary>
    /// Gets the signers in order.
    /// </summary>
    private List<UserLookupItem> Signers { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the form is being saved.
    /// </summary>
    private bool Busy { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        Units = await Documents.GetAuthorUnitsAsync(CancellationToken.None);
        if (Units.Count == 1)
        {
            Model.UnitId = Units[0].Id;
        }
    }

    /// <summary>
    /// Stores the chosen file.
    /// </summary>
    /// <param name="args">The file selection.</param>
    private void SelectFile(InputFileChangeEventArgs args)
    {
        ErrorMessage = null;
        var file = args.File;
        if (FileRules.ResolveDocumentType(file.Name) is null)
        {
            ErrorMessage = "Цей тип файлу не підтримується.";
            SelectedFile = null;
        }
        else if (file.Size > FileRules.MaxDocumentSize)
        {
            ErrorMessage = $"Розмір файлу не може перевищувати {FileRules.MaxDocumentSize / 1024 / 1024} МБ.";
            SelectedFile = null;
        }
        else
        {
            SelectedFile = file;
            if (string.IsNullOrWhiteSpace(Model.Title))
            {
                Model.Title = Path.GetFileNameWithoutExtension(file.Name);
            }
        }
    }

    /// <summary>
    /// Searches signers with a short debounce.
    /// </summary>
    /// <param name="args">The input event.</param>
    /// <returns>A task that completes when suggestions are updated.</returns>
    private async Task SearchAsync(ChangeEventArgs args)
    {
        SearchTerm = args.Value?.ToString() ?? string.Empty;
        if (_searchCancellation is not null)
        {
            await _searchCancellation.CancelAsync();
            _searchCancellation.Dispose();
        }

        _searchCancellation = new CancellationTokenSource();
        var token = _searchCancellation.Token;
        try
        {
            await Task.Delay(250, token);
            var found = await Documents.SearchSignersAsync(SearchTerm, token);
            Suggestions = [.. found.Where(f => Signers.TrueForAll(s => s.Id != f.Id))];
        }
        catch (OperationCanceledException)
        {
            // A newer keystroke superseded this search.
        }
    }

    /// <summary>
    /// Adds a signer to the end of the route.
    /// </summary>
    /// <param name="signer">The signer.</param>
    private void AddSigner(UserLookupItem signer)
    {
        if (Signers.TrueForAll(s => s.Id != signer.Id))
        {
            Signers.Add(signer);
        }

        Suggestions = [];
        SearchTerm = string.Empty;
    }

    /// <summary>
    /// Moves a signer up or down.
    /// </summary>
    /// <param name="index">The current position.</param>
    /// <param name="offset">-1 to move up, +1 to move down.</param>
    private void Move(int index, int offset)
    {
        var target = index + offset;
        if (target < 0 || target >= Signers.Count)
        {
            return;
        }

        (Signers[index], Signers[target]) = (Signers[target], Signers[index]);
    }

    /// <summary>
    /// Creates the document.
    /// </summary>
    /// <returns>A task that completes when the document is saved.</returns>
    private async Task SaveAsync()
    {
        if (SelectedFile is null)
        {
            ErrorMessage = "Оберіть файл документа.";
            return;
        }

        Busy = true;
        ErrorMessage = null;
        try
        {
            await using var stream = SelectedFile.OpenReadStream(FileRules.MaxDocumentSize);
            var result = await Documents.CreateAsync(
                new CreateDocumentCommand(
                    Model.Title,
                    Model.Description,
                    Model.Kind,
                    Model.UnitId ?? Guid.Empty,
                    Model.Mode,
                    [.. Signers.Select(static s => s.Id)],
                    Model.Submit && Signers.Count > 0,
                    new FileUpload(stream, SelectedFile.Name, SelectedFile.ContentType, SelectedFile.Size)),
                CancellationToken.None);

            if (result.IsSuccess)
            {
                var message = Model.Submit ? "Документ створено й надіслано на підпис" : "Чернетку збережено";
                Navigation.NavigateTo($"/documents/{result.Value}?toast={Uri.EscapeDataString(message)}");
                return;
            }

            ErrorMessage = result.Error.Message;
        }
        catch (IOException)
        {
            ErrorMessage = "Не вдалося прочитати файл. Спробуйте ще раз.";
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// The document form.
    /// </summary>
    private sealed class CreateModel
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть назву документа.")]
        [StringLength(Document.TitleMaxLength)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [StringLength(Document.DescriptionMaxLength)]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the kind.
        /// </summary>
        public DocumentKind Kind { get; set; } = DocumentKind.Memo;

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        [Required(ErrorMessage = "Оберіть підрозділ.")]
        public Guid? UnitId { get; set; }

        /// <summary>
        /// Gets or sets the signing order.
        /// </summary>
        public SigningMode Mode { get; set; } = SigningMode.Sequential;

        /// <summary>
        /// Gets or sets a value indicating whether to submit for signing right away.
        /// </summary>
        public bool Submit { get; set; }
    }
}
