using KOD.Application.Features.News.Discussion;
using KOD.Domain.News;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace KOD.Web.Components.Shared;

/// <summary>
/// The reactions to an article or a comment: a chip per chosen emotion (the reader's own is pressed; pressing it again
/// removes it) and a menu with all emotions. Used inside interactive components.
/// </summary>
public partial class ReactionBar
{
    /// <summary>
    /// Gets or sets the reactions.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public ReactionSummary Summary { get; set; } = ReactionSummary.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the current user may react.
    /// </summary>
    [Parameter]
    public bool CanReact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the bar belongs to a comment (smaller, no caption).
    /// </summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>
    /// Gets or sets the handler of a chosen emotion.
    /// </summary>
    [Parameter]
    public EventCallback<ReactionKind> OnReact { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the menu of emotions is open.
    /// </summary>
    private bool Open { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a reaction is being saved.
    /// </summary>
    private bool Busy { get; set; }

    /// <summary>
    /// Saves an emotion and closes the menu.
    /// </summary>
    /// <param name="kind">The emotion.</param>
    /// <returns>A task that completes when saved.</returns>
    private async Task ReactAsync(ReactionKind kind)
    {
        Open = false;
        Busy = true;
        try
        {
            await OnReact.InvokeAsync(kind);
        }
        finally
        {
            Busy = false;
        }
    }

    /// <summary>
    /// Closes the menu with the Escape key.
    /// </summary>
    /// <param name="args">The key event.</param>
    private void CloseOnEscape(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            Open = false;
        }
    }
}
