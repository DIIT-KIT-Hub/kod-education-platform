using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace ODC.IntegrationTests.Hosting;

/// <summary>
/// Gives every test client its own remote address (the in-memory test server has none), so the per-address rate limits
/// of the sign-in and activation endpoints count each client separately, as they would for real browsers.
/// </summary>
internal sealed class ClientAddressStartupFilter : IStartupFilter
{
    /// <summary>
    /// The request header that carries the address of the test client.
    /// </summary>
    public const string HeaderName = "X-Test-Client-Address";

    /// <inheritdoc />
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        => app =>
        {
            app.Use(static (context, nextMiddleware) =>
            {
                if (context.Request.Headers.TryGetValue(HeaderName, out var value) && IPAddress.TryParse(value.ToString(), out var address))
                {
                    context.Connection.RemoteIpAddress = address;
                    context.Request.Headers.Remove(HeaderName);
                }

                return nextMiddleware(context);
            });
            next(app);
        };
}
