using KOD.Application.Features.News;
using KOD.Domain.Access;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace KOD.Web.Components.Pages.News;

/// <summary>
/// A news article. The body is Markdown rendered to sanitized HTML (raw HTML disabled, unsafe URLs removed).
/// </summary>
public partial class NewsArticlePage
{
    /// <summary>
    /// Gets or sets the slug.
    /// </summary>
    [Parameter]
    public string Slug { get; set; } = string.Empty;

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
    /// Gets or sets the article.
    /// </summary>
    private NewsArticleDto? Article { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user manages news.
    /// </summary>
    private bool CanManage { get; set; }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        CanManage = (await AuthenticationState).User.HasPermission(Permissions.News.Manage);
        var result = await News.GetBySlugAsync(Slug, CancellationToken.None);
        Article = result.IsSuccess ? result.Value : null;
    }
}
