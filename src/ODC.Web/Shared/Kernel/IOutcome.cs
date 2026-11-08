namespace ODC.Web.Shared.Kernel;

/// <summary>
/// The common view of <see cref="Result"/> and <see cref="Result{T}"/> for code that does not know the value type (the
/// request pipeline).
/// </summary>
public interface IOutcome
{
    /// <summary>
    /// Gets the error, or null on success.
    /// </summary>
    Error? Error { get; }
}
