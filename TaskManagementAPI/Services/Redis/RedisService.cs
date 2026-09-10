using System.Text.Json;
using StackExchange.Redis;

namespace TaskManagementAPI.Services.Redis;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<bool> LockTakeAsync(string lockKey, string token, TimeSpan expiry)
    {
        // SET key token NX EX seconds
        // 本项目太小，没有更新缓存的需求，所以keepTtl参数没有必要用
        var acquired = await _db.StringSetAsync(lockKey, token, expiry, When.NotExists);
        return acquired;
    }

    public async Task<bool> LockReleaseAsync(string lockKey, string token)
    {
        var db = _redis.GetDatabase();
        // Lua 脚本：检查 value 是否等于 token，是则删除
        var script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end
        ";
        var result = await db.ScriptEvaluateAsync(script, new RedisKey[] { lockKey }, new RedisValue[] { token });
        return (int)result == 1;
    }

    public async Task<string?> GetStringAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        if (expiry.HasValue)
        {
            await _db.StringSetAsync(key, value, expiry.Value);

        }
        else
        {
            await _db.StringSetAsync(key, value);
        }
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<T?> GetObjectAsync<T>(string key) where T : class
    {
        var json = await GetStringAsync(key);
        if (string.IsNullOrEmpty(json))
            return null;
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        var json = JsonSerializer.Serialize(value);
        await SetStringAsync(key, json, expiry);
        //await _db.StringSetAsync(key, json, expiry, true, When.NotExists);
    }
    public async Task RemoveByPrefixAsync(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
            return;

        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            // 通过 ConnectionMultiplexer 获取 IServer 实例[reference:2]
            var server = _redis.GetServer(endpoint);

            // 使用 KeysAsync 进行模糊匹配，内部使用 SCAN 命令，比 KEYS 更安全[reference:4]
            // 注意：生产环境大数据量下，频繁使用 SCAN 仍可能有性能开销[reference:6]
            await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
            {
                await _db.KeyDeleteAsync(key);
            }
        }
    }

    /// <summary>
    /// 批量写入（使用 Pipeline）
    /// </summary>
    public async Task BatchSetAsync(Dictionary<string, string> keyValues)
    {
        var entries = keyValues
            .Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value))
            .ToArray();

        await _db.StringSetAsync(entries, When.Always);
    }

    /// <summary>
    /// 批量读取（使用 Pipeline）
    /// </summary>
    public async Task<Dictionary<string, string>> BatchGetAsync(IEnumerable<string> keys)
    {
        var batch = _db.CreateBatch();
        var tasks = new Dictionary<string, Task<RedisValue>>();

        foreach (var key in keys)
        {
            var task = batch.StringGetAsync(key);
            tasks[key] = task;
        }

        batch.Execute();
        await Task.WhenAll(tasks.Values);

        return tasks.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Result.ToString()
        );
    }
}