using ODC.Web.Security;
using ODC.Web.Shared.Identity;
using ODC.Web.Shared.Kernel;

namespace ODC.Web.Messaging;

/// <summary>
/// Checks the <see cref="RequirePermissionAttribute"/>s of the request type before the handler runs: the user needs one
/// of the listed permissions of every attribute. Endpoints check the same permissions, so this is defence in depth
/// (OWASP API5, broken function level authorization) for requests sent from anywhere else. Object-level checks (may this
/// user see this document?) stay in the handlers.
/// </summary>
/// <typeparam name="TRequest">The request.</typeparam>
/// <typeparam name="TResponse">The answer.</typeparam>
/// <param name="inner">The next step.</param>
/// <param name="currentUser">The current user.</param>
internal sealed class AuthorizationDecorator<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> inner, ICurrentUser currentUser)
    : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        foreach (var permissions in RequiredPermissions.Of(typeof(TRequest)))
        {
            if (!currentUser.IsAuthenticated || !permissions.Any(currentUser.Has))
            {
                return Task.FromResult(ResultFailure<TResponse>.Create is { } fail
                    ? fail(Error.Forbidden())
                    : throw new UnauthorizedAccessException($"{RequestNames.Of(typeof(TRequest))} needs one of: {string.Join(", ", permissions)}."));
            }
        }

        return inner.HandleAsync(request, cancellationToken);
    }
}
