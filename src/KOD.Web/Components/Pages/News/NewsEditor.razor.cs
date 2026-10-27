using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Files;
using KOD.Application.Common.Models;
using KOD.Application.Features.News;
using KOD.Application.Features.Organization;
using KOD.Domain.Access;
using KOD.Domain.Localization;
using KOD.Domain.News;
using KOD.Web.DesignSystem;
using KOD.Web.DesignSystem.Components;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace KOD.Web.Components.Pages.News;

/// <summary>
/// Creates or edits a news article (interactive server component with cover upload).
/// </summary>
public partial class NewsEditor
{
    /// <summary>
    /// Gets or sets the article identifier; <see langword="null"/> when creating.
    /// </summary>
    [Parameter]
    public Guid? Id { get; set; }

    /// <summary>
    /// Gets or sets the loaded article. Persisted from prerendering (like <see cref="OrganizationOptions"/>), so the
    /// interactive render shows the same form at once instead of a skeleton while loading it again.
    /// </summary>
    [PersistentState]
    public NewsArticleDto? Existing { get; set; }

    /// <summary>
    /// Gets or sets the organizations whose news the current user manages; <see langword="null"/> until loaded.
    /// </summary>
    [PersistentState]
    public IReadOnlyList<UnitOption>? OrganizationOptions { get; set; }

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the news service.
    /// </summary>
    [Inject]
    private INewsService News { get; set; } = default!;

    /// <summary>
    /// Gets or sets the organization service.
    /// </summary>
    [Inject]
    private IOrganizationService Organization { get; set; } = default!;

    /// <summary>
    /// Gets or sets the navigation manager.
    /// </summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Gets or sets the form model; setting it also creates a new <see cref="Context"/>.
    /// </summary>
    private EditorModel Model { get; set => Context = new EditContext(field = value); } = default!;

    /// <summary>
    /// Gets or sets the validation context of <see cref="Model"/>.
    /// </summary>
    private EditContext Context { get; set; } = default!;

    /// <summary>
    /// Gets or sets the rich text editor of the body.
    /// </summary>
    private RichTextEditor? Editor { get; set; }

    /// <summary>
    /// Gets or sets the chosen cover image.
    /// </summary>
    private IBrowserFile? CoverFile { get; set; }

    /// <summary>
    /// Gets the organizations whose news the current user manages.
    /// </summary>
    private IReadOnlyList<UnitOption> Organizations => OrganizationOptions ?? [];

    /// <summary>
    /// Gets or sets a value indicating whether the current user manages the news of every organization (and of the whole cabinet).
    /// </summary>
    private bool CabinetWide { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the data is loaded.
    /// </summary>
    private bool Loaded { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether an operation is running.
    /// </summary>
    private bool Busy { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized() => Model = new EditorModel();

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        CabinetWide = (await AuthenticationState).User.HasPermission(Permissions.Administration.AllOrganizations);
        OrganizationOptions ??= [.. (await Organization.GetOptionsAsync(Permissions.News.Manage, CancellationToken.None)).Where(static u => u.IsOrganization)];
        Model.OrganizationId ??= CabinetWide || Organizations.Count == 0 ? null : Organizations[0].Id;
        if (Id is { } id)
        {
            if (Existing?.Id != id)
            {
                var result = await News.GetAsync(id, CancellationToken.None);
                if (!result.IsSuccess)
                {
                    Navigation.NavigateTo("/news/manage");
                    return;
                }

                Existing = result.Value;
            }

            Model = new EditorModel
            {
                Title = Existing.Title,
                Summary = Existing.Summary,
                Body = Existing.BodyHtml,
                Category = Existing.Category,
                IsPinned = Existing.IsPinned,
                AllowComments = Existing.AllowComments,
                Publish = Existing.PublishedAt is not null,
                OrganizationId = Existing.Organization?.Id,
            };
        }

        Loaded = true;
    }

    /// <summary>
    /// Stores the chosen cover image.
    /// </summary>
    /// <param name="args">The file selection.</param>
    private void SelectCover(InputFileChangeEventArgs args)
    {
        ErrorMessage = args.File.Size > FileRules.MaxImageSize ? Texts.T("Зображення не може бути більшим за {0} МБ.", FileRules.MaxImageSize / 1024 / 1024) : null;
        CoverFile = ErrorMessage is null ? args.File : null;
    }

    /// <summary>
    /// Reads the body from the editor, validates the form and saves it.
    /// </summary>
    /// <returns>A task that completes when saved or rejected.</returns>
    private async Task SubmitAsync()
    {
        if (Editor is not null)
        {
            if (await Editor.IsUploadingAsync())
            {
                ErrorMessage = Texts.T("Дочекайтеся, поки завантажаться фото.");
                return;
            }

            Model.Body = await Editor.GetHtmlAsync();
        }

        if (Context.Validate())
        {
            await SaveAsync();
        }
    }

    /// <summary>
    /// Saves the article.
    /// </summary>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveAsync()
    {
        Busy = true;
        ErrorMessage = null;
        try
        {
            await using var cover = CoverFile?.OpenReadStream(FileRules.MaxImageSize);
            var upload = CoverFile is null || cover is null ? null : new FileUpload(cover, CoverFile.Name, CoverFile.ContentType, CoverFile.Size);
            var command = new SaveNewsCommand(Model.Title, Model.Summary, Model.Body, Model.Category, Model.IsPinned, Model.Publish, upload, Model.RemoveCover, Model.OrganizationId, Model.AllowComments);

            if (Id is { } id)
            {
                var result = await News.UpdateAsync(id, command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }
            else
            {
                var result = await News.CreateAsync(command, CancellationToken.None);
                ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            }

            if (ErrorMessage is null)
            {
                Navigation.NavigateTo($"/news/manage?toast={Uri.EscapeDataString(Texts.T("Публікацію збережено"))}");
            }
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// Deletes the article after confirmation.
    /// </summary>
    /// <returns>A task that completes when deleted.</returns>
    private async Task DeleteAsync()
    {
        if (Id is not { } id || !await JS.ConfirmAsync(Texts.T("Видалити публікацію остаточно?")))
        {
            return;
        }

        var result = await News.DeleteAsync(id, CancellationToken.None);
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/news/manage?toast={Uri.EscapeDataString(Texts.T("Публікацію видалено"))}");
            return;
        }

        ErrorMessage = result.Error.Message;
    }

    /// <summary>
    /// The editor form.
    /// </summary>
    private sealed class EditorModel
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть заголовок.")]
        [StringLength(NewsArticle.TitleMaxLength)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the lead.
        /// </summary>
        [Required(ErrorMessage = "Вкажіть анонс.")]
        [StringLength(NewsArticle.SummaryMaxLength)]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTML body (read from the editor when the form is submitted).
        /// </summary>
        [Required(ErrorMessage = "Напишіть текст новини.")]
        [StringLength(NewsArticle.BodyMaxLength)]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        public NewsCategory Category { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the article is pinned.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether readers may comment.
        /// </summary>
        public bool AllowComments { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the article is published.
        /// </summary>
        public bool Publish { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to remove the cover.
        /// </summary>
        public bool RemoveCover { get; set; }

        /// <summary>
        /// Gets or sets the organization of the article; <see langword="null"/> for news of the whole cabinet.
        /// </summary>
        public Guid? OrganizationId { get; set; }
    }
}
