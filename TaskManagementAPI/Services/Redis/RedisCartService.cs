
using StackExchange.Redis;

namespace TaskManagementAPI.Services.Redis
{
    /// <summary>
    /// Redis Hash 数据类型 模拟购物车
    /// 适用场景：存储对象（如用户信息）、购物车、配置项
    /// </summary>
    public class RedisCartService : IRedisCartService
    {
        private readonly IDatabase _db;
        private readonly string _keyPrefix = "cart:";
        public RedisCartService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        private string GetCartKey(string userId) => $"{_keyPrefix}{userId}";

        /// <summary>
        /// 加入购物车
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task AddItemAsync(string userId, int productId, int quantity)
        {
            var key = GetCartKey(userId);
            // HINCRBY：如果 field 不存在则创建，存在则增加
            await _db.HashIncrementAsync(key, productId.ToString(), quantity);
        }

        /// <summary>
        /// 删除商品
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public async Task RemoveItemAsync(string userId, int productId)
        {
            var key = GetCartKey(userId);
            await _db.HashDeleteAsync(key, productId.ToString());
        }

        /// <summary>
        /// 更新商品数量
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="productId"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task UpdateItemQuantityAsync(string userId, int productId, int quantity)
        {
            var key = GetCartKey(userId);
            if (quantity <= 0)
            {
                await RemoveItemAsync(userId, productId);
            }
            else
            {
                await _db.HashSetAsync(key, productId.ToString(), quantity);
            }
        }

        /// <summary>
        /// 获取购物车内容
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Dictionary<int, int>> GetCartAsync(string userId)
        {
            var key = GetCartKey(userId);
            var entries = await _db.HashGetAllAsync(key);
            var cart = new Dictionary<int, int>();
            foreach (var entry in entries)
            {
                if (int.TryParse(entry.Name, out var productId) && int.TryParse(entry.Value, out var quantity))
                {
                    cart[productId] = quantity;
                }
            }
            return cart;
        }

        /// <summary>
        /// 清空购物车
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task ClearCartAsync(string userId)
        {
            var key = GetCartKey(userId);
            await _db.KeyDeleteAsync(key);
        }

        /// <summary>
        /// 获取商品数量
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<int> GetItemCountAsync(string userId)
        {
            var key = GetCartKey(userId);
            return (int)await _db.HashLengthAsync(key);
        }
    }
}