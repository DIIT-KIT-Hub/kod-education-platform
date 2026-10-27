using KOD.Application.Features.News.Discussion;
using KOD.Domain.Localization;
using KOD.Domain.News;
using KOD.Web.DesignSystem;
using KOD.Web.Security;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace KOD.Web.Components.Pages.News;

/// <summary>
/// Reactions and comments under an article (interactive island of a static page). The discussion loaded while
/// prerendering is persisted, so the circuit shows it at once; changes by other readers arrive through
/// <see cref="INewsDiscussionEvents"/> and refresh the panel.
/// </summary>
/// <remarks>
/// The database context belongs to the circuit, and a change notification (also of this panel's own change) arrives
/// while an action may still be running, so every service call goes through <see cref="_gate"/>.
/// </remarks>
public sealed partial class NewsDiscussionPanel : IDisposable
{
    /// <summary>
    /// Lets one service call at a time use the circuit's database context.
    /// </summary>
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>
    /// The subscription to changes of the article.
    /// </summary>
    private IDisposable? _subscription;

    /// <summary>
    /// Gets or sets the article.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Gets or sets the discussion (persisted from prerendering).
    /// </summary>
    [PersistentState]
    public NewsDiscussion? Discussion { get; set; }

    /// <summary>
    /// Gets or sets the authentication state.
    /// </summary>
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    /// <summary>
    /// Gets or sets the discussion service.
    /// </summary>
    [Inject]
    private INewsDiscussionService Service { get; set; } = default!;

    /// <summary>
    /// Gets or sets the change notifications.
    /// </summary>
    [Inject]
    private INewsDiscussionEvents Events { get; set; } = default!;

    /// <summary>
    /// Gets or sets the clock.
    /// </summary>
    [Inject]
    private TimeProvider Clock { get; set; } = default!;

    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Gets or sets how many top-level comments are shown.
    /// </summary>
    private int Limit { get; set; } = INewsDiscussionService.PageSize;

    /// <summary>
    /// Gets or sets the text of a new comment.
    /// </summary>
    private string NewComment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comment being answered.
    /// </summary>
    private Guid? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the text of the reply.
    /// </summary>
    private string ReplyText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the comment being edited.
    /// </summary>
    private Guid? EditingId { get; set; }

    /// <summary>
    /// Gets or sets the new text of the edited comment.
    /// </summary>
    private string EditText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether an action runs.
    /// </summary>
    private bool Busy { get; set; }

    /// <summary>
    /// Gets or sets the error of the last action.
    /// </summary>
    private string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the current user's name.
    /// </summary>
    private string MyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current user's photo.
    /// </summary>
    private Guid? MyPhotoId { get; set; }

    /// <summary>
    /// Gets the current moment (relative times of comments).
    /// </summary>
    private DateTimeOffset Now => Clock.GetUtcNow();

    /// <inheritdoc />
    public void Dispose()
    {
        _subscription?.Dispose();
        _gate.Dispose();
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationState).User;
        MyName = user.FullName;
        MyPhotoId = user.PhotoId;

        if (Discussion is null)
        {
            await ReloadAsync();
        }

        if (RendererInfo.IsInteractive)
        {
            _subscription = Events.Subscribe(ArticleId, () => InvokeAsync(async () =>
            {
                await ExclusiveAsync(ReloadAsync);
                StateHasChanged();
            }));
        }
    }

    /// <summary>
    /// Loads the discussion again.
    /// </summary>
    /// <returns>A task that completes when loaded.</returns>
    private async Task ReloadAsync()
    {
        var result = await Service.GetAsync(ArticleId, Limit, CancellationToken.None);
        if (result.IsSuccess)
        {
            Discussion = result.Value;
        }
    }

    /// <summary>
    /// Sets or removes a reaction to the article or a comment.
    /// </summary>
    /// <param name="commentId">The comment, or <see langword="null"/> for the article.</param>
    /// <param name="kind">The emotion.</param>
    /// <returns>A task that completes when saved.</returns>
    private async Task ReactAsync(Guid? commentId, ReactionKind kind)
    {
        await ExclusiveAsync(async () =>
        {
            var result = await Service.ReactAsync(ArticleId, commentId, kind, CancellationToken.None);
            ErrorMessage = result.IsSuccess ? null : result.Error.Message;
            await ReloadAsync();
        });
    }

    /// <summary>
    /// Sends a new comment or a reply.
    /// </summary>
    /// <param name="parentId">The comment answered, if any.</param>
    /// <returns>A task that completes when sent.</returns>
    private async Task CommentAsync(Guid? parentId)
    {
        await RunAsync(async () =>
        {
            var result = await Service.CommentAsync(ArticleId, parentId, parentId is null ? NewComment : ReplyText, CancellationToken.None);
            if (!result.IsSuccess)
            {
                return result.Error.Message;
            }

            if (parentId is null)
            {
                NewComment = string.Empty;
            }
            else
            {
                ReplyText = string.Empty;
                ReplyTo = null;
            }

            return null;
        });
    }

    /// <summary>
    /// Opens the reply form under a thread (a reply to a reply answers in the same thread).
    /// </summary>
    /// <param name="comment">The comment answered.</param>
    private void StartReply(NewsCommentDto comment)
    {
        EditingId = null;
        ReplyTo = comment.ParentId ?? comment.Id;
        ReplyText = comment.ParentId is null ? string.Empty : $"{comment.Author.FullName.Split(' ').Skip(1).FirstOrDefault() ?? comment.Author.FullName}, ";
    }

    /// <summary>
    /// Opens the edit form of a comment.
    /// </summary>
    /// <param name="comment">The comment.</param>
    private void StartEdit(NewsCommentDto comment)
    {
        ReplyTo = null;
        EditingId = comment.Id;
        EditText = comment.Body;
    }

    /// <summary>
    /// Saves the edited comment.
    /// </summary>
    /// <param name="commentId">The comment.</param>
    /// <returns>A task that completes when saved.</returns>
    private async Task SaveEditAsync(Guid commentId)
    {
        await RunAsync(async () =>
        {
            var result = await Service.EditCommentAsync(ArticleId, commentId, EditText, CancellationToken.None);
            if (!result.IsSuccess)
            {
                return result.Error.Message;
            }

            EditingId = null;
            return null;
        });
    }

    /// <summary>
    /// Deletes a comment after confirmation.
    /// </summary>
    /// <param name="commentId">The comment.</param>
    /// <returns>A task that completes when deleted.</returns>
    private async Task DeleteAsync(Guid commentId)
    {
        if (!await JS.ConfirmAsync(Texts.T("Видалити коментар?")))
        {
            return;
        }

        await RunAsync(async () =>
        {
            var result = await Service.DeleteCommentAsync(ArticleId, commentId, CancellationToken.None);
            return result.IsSuccess ? null : result.Error.Message;
        });
    }

    /// <summary>
    /// Shows older comments.
    /// </summary>
    /// <returns>A task that completes when loaded.</returns>
    private async Task ShowMoreAsync()
    {
        Limit += INewsDiscussionService.PageSize;
        await RunAsync(static () => Task.FromResult<string?>(null));
    }

    /// <summary>
    /// Runs an action with the busy state, shows its error and reloads the discussion.
    /// </summary>
    /// <param name="action">The action; returns an error message or <see langword="null"/>.</param>
    /// <returns>A task that completes when done.</returns>
    private async Task RunAsync(Func<Task<string?>> action)
    {
        Busy = true;
        try
        {
            await ExclusiveAsync(async () =>
            {
                ErrorMessage = await action();
                await ReloadAsync();
            });
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// Runs database work after the work started before it has finished.
    /// </summary>
    /// <param name="work">The work.</param>
    /// <returns>A task that completes when done.</returns>
    private async Task ExclusiveAsync(Func<Task> work)
    {
        await _gate.WaitAsync();
        try
        {
            await work();
        }
        finally
        {
            _gate.Release();
        }
    }
}
