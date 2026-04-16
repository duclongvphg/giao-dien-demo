using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace giao_dien_demo.Hubs
{
    public class DashboardHub : Hub
    {
        public async Task UpdateEmployeeCount(int total)
        {
            await Clients.All.SendAsync("UpdateEmployeeCount", total);
        }

        public async Task SendNotification(string content)
        {
            await Clients.All.SendAsync(
                "ReceiveNotification",
                content
            );
        }

        public async Task RecallNotification(string content)
        {
            await Clients.All.SendAsync(
                "NotificationRecalled",
                content
            );
        }
    }
}