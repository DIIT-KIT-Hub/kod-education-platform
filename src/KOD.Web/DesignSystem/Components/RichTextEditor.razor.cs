using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace KOD.Web.DesignSystem.Components;

/// <summary>
/// A rich text editor (Tiptap, <c>js/rich-text.js</c>) for interactive components: headings, lists, quotes, tables,
/// links, images and galleries uploaded to <see cref="UploadUrl"/>. The HTML is read on demand with
/// <see cref="GetHtmlAsync"/> through a stream, so long texts are not limited by the size of circuit messages.
/// </summary>
public partial class RichTextEditor : IAsyncDisposable
{
    /// <summary>
    /// The largest HTML read from the browser.
    /// </summary>
    private const long MaxHtmlBytes = 1024 * 1024;

    /// <summary>
    /// The component root.
    /// </summary>
    private ElementReference _host;

    /// <summary>
    /// The script module.
    /// </summary>
    private IJSObjectReference? _module;

    /// <summary>
    /// The editor created by the script.
    /// </summary>
    private IJSObjectReference? _editor;

    /// <summary>
    /// Gets or sets the starting HTML (read once, when the editor is created).
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the endpoint that stores uploaded images.
    /// </summary>
    [Parameter]
    public string UploadUrl { get; set; } = "/api/v1/media";

    /// <summary>
    /// Gets or sets the text shown in an empty editor.
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// Gets or sets the accessible name of the text area.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the element identifier (for a field label).
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the JavaScript runtime.
    /// </summary>
    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Reads the current HTML ("" when the editor is empty or not ready).
    /// </summary>
    /// <returns>The HTML.</returns>
    public async Task<string> GetHtmlAsync()
    {
        if (_editor is null)
        {
            return Value ?? string.Empty;
        }

        var stream = await _editor.InvokeAsync<IJSStreamReference>("getContent");
        await using var content = await stream.OpenReadStreamAsync(MaxHtmlBytes);
        using var reader = new StreamReader(content, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Determines whether images are still being uploaded.
    /// </summary>
    /// <returns><see langword="true"/> while an upload runs.</returns>
    public async Task<bool> IsUploadingAsync() => _editor is not null && await _editor.InvokeAsync<bool>("isUploading");

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_editor is not null)
            {
                await _editor.InvokeVoidAsync("dispose");
                await _editor.DisposeAsync();
            }

            if (_module is not null)
            {
                await _module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone together with the page.
        }

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", $"./{Assets["js/rich-text.js"]}");
            _editor = await _module.InvokeAsync<IJSObjectReference>("createEditor", _host, Value ?? string.Empty);
        }
    }
}
