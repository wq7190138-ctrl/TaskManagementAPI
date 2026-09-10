namespace TaskManagementAPI.Services.Redis
{
    /// <summary>
    /// Redis List
    /// 适用场景：消息队列、操作日志、历史记录栈、最新消息列表。
    /// </summary>
    public interface IRedisListService
    {
        Task PushLogAsync(string listKey, string logEntry);
        Task<List<string>> GetRecentLogsAsync(string listKey, int count);
        Task<string> PopLogAsync(string listKey);
    }
}
