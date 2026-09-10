using Microsoft.AspNetCore.SignalR;

namespace TaskManagementAPI.Hubs
{
    public class CacheNotificationHub : Hub
    {
        // 客户端可以调用这个方法加入特定组（如果需要对不同数据分组）
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        // 广播方法：供服务层调用，通知所有客户端缓存失效
        public async Task NotifyCacheInvalidation(string cacheKey)
        {
            await Clients.All.SendAsync("CacheInvalidated", cacheKey);
        }
    }
}
