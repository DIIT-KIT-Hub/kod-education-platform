namespace ODC.Web.Messaging;

/// <summary>
/// A request that changes state (creates, edits, deletes, sends).
/// </summary>
/// <typeparam name="TResponse">The handler's answer.</typeparam>
#pragma warning disable CA1040 // Marker interface: commands and queries are told apart by the pipeline and in reviews.
public interface ICommand<TResponse> : IRequest<TResponse>;
#pragma warning restore CA1040
