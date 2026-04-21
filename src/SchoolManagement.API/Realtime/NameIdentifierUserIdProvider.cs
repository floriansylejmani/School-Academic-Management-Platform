using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SchoolManagement.API.Realtime;

public sealed class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
