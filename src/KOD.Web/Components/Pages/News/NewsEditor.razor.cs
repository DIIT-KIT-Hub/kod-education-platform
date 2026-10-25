using System.ComponentModel.DataAnnotations;
using KOD.Application.Common.Files;
using KOD.Application.Features.News;
using KOD.Domain.News;
using Microsoft.AspNetCore.Components;
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
    /// Gets or sets the news service.
    /// </summary>
    [Inject]
    private INewsService News { get; set; } = default!;

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
    /// Gets or sets the form model.
    /// </summary>
    private EditorModel Model { get; set; } = new();

    /// <summary>
    /// Gets or sets the loaded article.
    /// </summary>
    private NewsArticleDto? Existing { get; set; }

    /// <summary>
    /// Gets or sets the chosen cover image.
    /// </summary>
    private IBrowserFile? CoverFile { get; set; }

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
    protected override async Task OnParametersSetAsync()
    {
        if (Id is { } id)
        {
            var result = await News.GetAsync(id, CancellationToken.None);
            if (!result.IsSuccess)
            {
                Navigation.NavigateTo("/news/manage");
                return;
            }

            Existing = result.Value;
            Model = new EditorModel
            {
                Title = Existing.Title,
                Summary = Existing.Summary,
                Body = Existing.Body,
                Category = Existing.Category,
                IsPinned = Existing.IsPinned,
                Publish = Existing.PublishedAt is not null,
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
        ErrorMessage = args.File.Size > FileRules.MaxImageSize ? "Зображення не може бути більшим за 5 МБ." : null;
        CoverFile = ErrorMessage is null ? args.File : null;
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
            var command = new SaveNewsCommand(Model.Title, Model.Summary, Model.Body, Model.Category, Model.IsPinned, Model.Publish, upload, Model.RemoveCover);

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
                Navigation.NavigateTo($"/news/manage?toast={Uri.EscapeDataString("Публікацію збережено")}");
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
        if (Id is not { } id || !await JS.InvokeAsync<bool>("confirm", "Видалити публікацію остаточно?"))
        {
            return;
        }

        var result = await News.DeleteAsync(id, CancellationToken.None);
        if (result.IsSuccess)
        {
            Navigation.NavigateTo($"/news/manage?toast={Uri.EscapeDataString("Публікацію видалено")}");
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
        /// Gets or sets the Markdown body.
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
        /// Gets or sets a value indicating whether the article is published.
        /// </summary>
        public bool Publish { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to remove the cover.
        /// </summary>
        public bool RemoveCover { get; set; }
    }
}
