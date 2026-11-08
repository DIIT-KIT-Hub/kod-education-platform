namespace ODC.Web.Messaging;

/// <summary>
/// A use case input handled by exactly one <see cref="IRequestHandler{TRequest, TResponse}"/>. Implement
/// <see cref="ICommand{TResponse}"/> (changes state) or <see cref="IQuery{TResponse}"/> (only reads) instead of this
/// interface.
/// </summary>
/// <typeparam name="TResponse">The handler's answer, usually a <c>Result</c> or <c>Result&lt;T&gt;</c>.</typeparam>
#pragma warning disable CA1040 // Marker interface: it carries the response type for the compiler.
public interface IRequest<TResponse>;
#pragma warning restore CA1040
