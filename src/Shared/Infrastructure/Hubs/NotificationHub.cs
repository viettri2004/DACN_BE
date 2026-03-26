using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace Shared.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);

                var roles = Context.User?.FindAll(ClaimTypes.Role).Select(r => r.Value);
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, role);
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = Context.User?.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
                
                var roles = Context.User?.FindAll(ClaimTypes.Role).Select(r => r.Value);
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        await Groups.RemoveFromGroupAsync(Context.ConnectionId, role);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
