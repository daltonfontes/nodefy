using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nodefy.Api.Hubs;

[Authorize]
public class BoardHub : Hub
{
    public Task JoinBoard(string pipelineId)
        => Groups.AddToGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");

    public Task LeaveBoard(string pipelineId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"pipeline:{pipelineId}");
}
