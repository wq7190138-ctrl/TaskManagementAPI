using StackExchange.Redis;

namespace TaskManagementAPI.Services.Redis
{
    public class RedisSetService : IRedisSetService
    {
        private readonly IDatabase _redis;

        public RedisSetService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task AddTagsAsync(string key, params string[] tags)
        {
            var values = tags.Select(t => (RedisValue)t).ToArray();
            await _redis.SetAddAsync(key, values);
        }

        public async Task RemoveTagsAsync(string key, params string[] tags)
        {
            var values = tags.Select(t => (RedisValue)t).ToArray();
            await _redis.SetRemoveAsync(key, values);
        }

        public async Task<HashSet<string>> GetTagsAsync(string key)
        {
            var values = await _redis.SetMembersAsync(key);
            return values.Select(v => v.ToString()).ToHashSet();
        }

        public async Task<bool> HasTagAsync(string key, string tag)
        {
            return await _redis.SetContainsAsync(key, tag);
        }

        public async Task<HashSet<string>> IntersectAsync(string key1, string key2)
        {
            var values = await _redis.SetCombineAsync(SetOperation.Intersect, key1, key2);
            return values.Select(v => v.ToString()).ToHashSet();
        }

        public async Task<HashSet<string>> UnionAsync(string key1, string key2)
        {
            var values = await _redis.SetCombineAsync(SetOperation.Union, key1, key2);
            return values.Select(v => v.ToString()).ToHashSet();
        }

        public async Task<HashSet<string>> DifferenceAsync(string key1, string key2)
        {
            var values = await _redis.SetCombineAsync(SetOperation.Difference, key1, key2);
            return values.Select(v => v.ToString()).ToHashSet();
        }
    }
}
