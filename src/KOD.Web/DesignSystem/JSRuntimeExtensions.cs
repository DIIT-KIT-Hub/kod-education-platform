using Microsoft.JSInterop;

namespace KOD.Web.DesignSystem;

/// <summary>
/// Design system behaviors of <c>kod-ds.js</c> for interactive components.
/// </summary>
public static class JSRuntimeExtensions
{
    /// <param name="js">The JavaScript runtime of the circuit.</param>
    extension(IJSRuntime js)
    {
        /// <summary>
        /// Asks the user to confirm an action in the shared confirmation dialog (the same one as <c>data-confirm</c> forms).
        /// </summary>
        /// <param name="message">The question.</param>
        /// <returns><see langword="true"/> when confirmed.</returns>
        /// <remarks>
        /// The call has no interop timeout: the dialog waits for the user as long as needed.
        /// </remarks>
        public ValueTask<bool> ConfirmAsync(string message)
            => js.InvokeAsync<bool>("kodDs.confirm", CancellationToken.None, message);
    }
}
