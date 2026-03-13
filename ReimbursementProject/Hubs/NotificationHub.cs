using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ReimbursementProject.Hubs
{
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var empId = httpContext?.User?.FindFirst("EmpID")?.Value;
            if (!string.IsNullOrEmpty(empId))
            {
                Groups.AddToGroupAsync(Context.ConnectionId, $"EMP_{empId}");
            }
            return base.OnConnectedAsync();
        }

        // Optional method to call from backend
        public async Task SendNotification(string empId)
        {
            await Clients.Group($"EMP_{empId}").SendAsync("Notify");
        }
    }
}
