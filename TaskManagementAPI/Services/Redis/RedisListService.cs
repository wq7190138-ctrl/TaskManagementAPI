using StackExchange.Redis;

namespace TaskManagementAPI.Services.Redis
{
    /// <summary>
    /// Redis List
    /// 适用场景：消息队列、操作日志、历史记录栈、最新消息列表
    /// 记录用户对任务的“新增”、“修改”、“删除”操作。
    /// </summary>
    public class RedisListService : IRedisListService
    {
        private readonly IDatabase _redis;

        public RedisListService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        // 推入日志（左侧）
        public async Task PushLogAsync(string listKey, string logEntry)
        {
            await _redis.ListLeftPushAsync(listKey, logEntry);
            // 限制最大长度，防止内存溢出（比如只保留 100 条）
            await _redis.ListTrimAsync(listKey, 0, 99);
        }

        // 获取最近 N 条日志
        public async Task<List<string>> GetRecentLogsAsync(string listKey, int count)
        {
            var values = await _redis.ListRangeAsync(listKey, 0, count - 1);
            return values.Select(v => v.ToString()).ToList();
        }

        // 弹出日志（队列消费）
        public async Task<string> PopLogAsync(string listKey)
        {
            var value = await _redis.ListLeftPopAsync(listKey);
            return value.ToString();
        }
    }
}