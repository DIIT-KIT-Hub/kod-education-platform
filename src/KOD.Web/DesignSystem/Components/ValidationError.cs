using System.Linq.Expressions;
using KOD.Domain.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// The validation messages of one field, translated into the interface language. Use instead of
/// <see cref="ValidationMessage{TValue}"/>: data annotation messages are Ukrainian constants, so they are translated when shown.
/// </summary>
/// <typeparam name="TValue">The field type.</typeparam>
public sealed class ValidationError<TValue> : ComponentBase, IDisposable
{
    /// <summary>
    /// The handler subscribed to the edit context.
    /// </summary>
    private readonly EventHandler<ValidationStateChangedEventArgs> _onChanged;

    /// <summary>
    /// The edit context the handler is subscribed to.
    /// </summary>
    private EditContext? _subscribed;

    /// <summary>
    /// The field whose messages are shown.
    /// </summary>
    private FieldIdentifier _field;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationError{TValue}"/> class.
    /// </summary>
    public ValidationError() => _onChanged = (_, _) => StateHasChanged();

    /// <summary>
    /// Gets or sets the field, e.g. <c>() =&gt; Model.Name</c>.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public Expression<Func<TValue>>? For { get; set; }

    /// <summary>
    /// Gets or sets the edit context of the enclosing form.
    /// </summary>
    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <inheritdoc />
    public void Dispose() => Unsubscribe();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (EditContext is null || For is null)
        {
            throw new InvalidOperationException($"{nameof(ValidationError<>)} requires a cascading EditContext and the {nameof(For)} parameter.");
        }

        _field = FieldIdentifier.Create(For);
        if (!ReferenceEquals(_subscribed, EditContext))
        {
            Unsubscribe();
            _subscribed = EditContext;
            _subscribed.OnValidationStateChanged += _onChanged;
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        foreach (var message in EditContext!.GetValidationMessages(_field))
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "validation-message");
            builder.AddContent(2, Texts.T(message));
            builder.CloseElement();
        }
    }

    /// <summary>
    /// Stops listening to the edit context.
    /// </summary>
    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.OnValidationStateChanged -= _onChanged;
            _subscribed = null;
        }
    }
}
