using Microsoft.AspNetCore.SignalR;
using ODC.Web.Security;

namespace ODC.Web.Realtime;

/// <summary>
/// Identifies the SignalR connections of a user by the subject of the access token (the user identifier), so a chat
/// message reaches every page the user has open.
/// </summary>
internal sealed class SubjectUserIdProvider : IUserIdProvider
{
    /// <inheritdoc />
    public string? GetUserId(HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return connection.User.UserId?.ToString();
    }
}
