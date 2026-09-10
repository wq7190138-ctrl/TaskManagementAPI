namespace TaskManagementAPI.Services.Redis
{
    public interface IRedisSetService
    {
        // 添加标签
        Task AddTagsAsync(string key, params string[] tags);
        // 移除标签
        Task RemoveTagsAsync(string key, params string[] tags);
        // 获取所有标签
        Task<HashSet<string>> GetTagsAsync(string key);
        // 检查是否存在某标签
        Task<bool> HasTagAsync(string key, string tag);
        // 两个集合的交集（共同标签）
        Task<HashSet<string>> IntersectAsync(string key1, string key2);
        // 两个集合的并集
        Task<HashSet<string>> UnionAsync(string key1, string key2);
        // 两个集合的差集（key1 有但 key2 没有）
        Task<HashSet<string>> DifferenceAsync(string key1, string key2);
    }
}
