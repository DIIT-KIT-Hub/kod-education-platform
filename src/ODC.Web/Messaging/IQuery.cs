namespace ODC.Web.Messaging;

/// <summary>
/// A request that only reads. Its handler must not change state (marking things read is the one accepted side effect).
/// </summary>
/// <typeparam name="TResponse">The handler's answer.</typeparam>
#pragma warning disable CA1040 // Marker interface: commands and queries are told apart by the pipeline and in reviews.
public interface IQuery<TResponse> : IRequest<TResponse>;
#pragma warning restore CA1040
