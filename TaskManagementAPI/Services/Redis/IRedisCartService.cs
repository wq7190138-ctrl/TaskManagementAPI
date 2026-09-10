namespace TaskManagementAPI.Services.Redis
{
    /// <summary>
    /// Redis Hash 数据类型 模拟购物车
    /// 适用场景：存储对象（如用户信息）、购物车、配置项
    /// </summary>
    public interface IRedisCartService
    {
        Task AddItemAsync(string userId, int productId, int quantity);
        Task RemoveItemAsync(string userId, int productId);
        Task UpdateItemQuantityAsync(string userId, int productId, int quantity);
        Task<Dictionary<int, int>> GetCartAsync(string userId);
        Task ClearCartAsync(string userId);
        Task<int> GetItemCountAsync(string userId);
    }
}