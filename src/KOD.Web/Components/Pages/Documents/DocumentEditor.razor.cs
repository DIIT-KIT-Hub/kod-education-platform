using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Features.Documents;
using KOD.Domain.Documents;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace KOD.Web.Components.Pages.Documents;

/// <summary>
/// Creates a document or changes a draft / rejected one: file, details and the signing route (interactive server component).
/// </summary>
public sealed partial class DocumentEditor : IDisposable
{
    /// <summary>
    /// Debounces signer search input.
    /// </summary>
    private CancellationTokenSource? _searchCancellation;

    /// <summary>
    /// Gets or sets the identifier of the document being edited; <see langword="null"/> when creating.
    /// </summary>
    [Parameter]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the document being edited. Persisted from prerendering (like <see cref="AuthorUnits"/>), so the
    /// interactive render shows the same form at once instead of a skeleton while loading it again.
    /// </summary>
    [PersistentState]
    public DocumentEditModel? Existing { get; set; }

    /// <summary>
    /// Gets or sets the units the current user may author documents in; <see langword="null"/> until loaded.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<UnitOption>? AuthorUnits { get; set; }

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
    /// Gets a value indicating whether an existing document is edited.
    /// </summary>
    private bool IsEdit => Id is not null;

    /// <summary>
    /// Gets or sets the reason the document cannot be edited.
    /// </summary>
    private string? LoadError { get; set; }

    /// <summary>
    /// Gets or sets the form model.
    /// </summary>
    private EditorModel Model { get; set; } = new();

    /// <summary>
    /// Gets or sets the units the user may assign the document to.
    /// </summary>
    private IReadOnlyList<UnitOption> Units { get; set; } = [];

    /// <summary>
    /// Gets or sets the chosen (new) file.
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

    /// <summary>
    /// Gets the signers whose signatures are kept while the file is not replaced.
    /// </summary>
    private IEnumerable<UserLookupItem> KeptSigners
        => SelectedFile is not null || Existing is null ? [] : Signers.Where(s => Existing.SignedBy.Contains(s.Id));

    /// <summary>
    /// Gets the number of existing signatures that saving will remove.
    /// </summary>
    private int SignaturesToRemove
        => Existing is null ? 0 : SelectedFile is not null ? Existing.SignedBy.Count : Existing.SignedBy.Count(id => Signers.TrueForAll(s => s.Id != id));

    /// <inheritdoc />
    public void Dispose()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        AuthorUnits ??= await Documents.GetAuthorUnitsAsync(CancellationToken.None);
        Units = AuthorUnits;
        if (Id is not { } id)
        {
            if (Units.Count == 1)
            {
                Model.UnitId = Units[0].Id;
            }

            return;
        }

        if (Existing?.Id != id)
        {
            var result = await Documents.GetForEditAsync(id, CancellationToken.None);
            if (!result.IsSuccess)
            {
                LoadError = result.Error.Message;
                return;
            }

            Existing = result.Value;
        }

        Model.Title = Existing.Title;
        Model.Description = Existing.Description;
        Model.Kind = Existing.Kind;
        Model.UnitId = Existing.Unit.Id;
        Model.Mode = Existing.SigningMode;
        Signers.AddRange(Existing.Signers);
        if (Units.All(u => u.Id != Existing.Unit.Id))
        {
            Units = [Existing.Unit, .. Units];
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
            ErrorMessage = Texts.T("Цей тип файлу не підтримується.");
            SelectedFile = null;
        }
        else if (file.Size > FileRules.MaxDocumentSize)
        {
            ErrorMessage = Texts.T("Розмір файлу не може перевищувати {0} МБ.", FileRules.MaxDocumentSize / 1024 / 1024);
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
    /// Creates the document or saves the changes.
    /// </summary>
    /// <returns>A task that completes when the document is saved.</returns>
    private async Task SaveAsync()
    {
        if (!IsEdit && SelectedFile is null)
        {
            ErrorMessage = Texts.T("Оберіть файл документа.");
            return;
        }

        Busy = true;
        ErrorMessage = null;
        Stream? stream = null;
        try
        {
            stream = SelectedFile?.OpenReadStream(FileRules.MaxDocumentSize);
            var upload = SelectedFile is null ? null : new FileUpload(stream!, SelectedFile.Name, SelectedFile.ContentType, SelectedFile.Size);
            var submit = Model.Submit && Signers.Count > 0;
            Guid documentId;
            if (Existing is { } existing)
            {
                var result = await Documents.UpdateAsync(
                    new UpdateDocumentCommand(existing.Id, Model.Title, Model.Description, Model.Kind, Model.UnitId ?? Guid.Empty, Model.Mode, [.. Signers.Select(static s => s.Id)], submit, upload),
                    CancellationToken.None);
                if (!result.IsSuccess)
                {
                    ErrorMessage = result.Error.Message;
                    return;
                }

                documentId = existing.Id;
            }
            else
            {
                var result = await Documents.CreateAsync(
                    new CreateDocumentCommand(Model.Title, Model.Description, Model.Kind, Model.UnitId ?? Guid.Empty, Model.Mode, [.. Signers.Select(static s => s.Id)], submit, upload!),
                    CancellationToken.None);
                if (!result.IsSuccess)
                {
                    ErrorMessage = result.Error.Message;
                    return;
                }

                documentId = result.Value;
            }

            var message = (IsEdit, submit) switch
            {
                (true, true) => Texts.T("Зміни збережено, документ надіслано на підписання"),
                (true, false) => Texts.T("Зміни збережено"),
                (false, true) => Texts.T("Документ створено й надіслано на підпис"),
                _ => Texts.T("Чернетку збережено"),
            };
            Navigation.NavigateTo($"/documents/{documentId}?toast={Uri.EscapeDataString(message)}");
        }
        catch (IOException)
        {
            ErrorMessage = Texts.T("Не вдалося прочитати файл. Спробуйте ще раз.");
        }
        finally
        {
            if (stream is not null)
            {
                await stream.DisposeAsync();
            }

            Busy = false;
        }
    }

    /// <summary>
    /// The document form.
    /// </summary>
    private sealed class EditorModel
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
