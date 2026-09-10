using System.Threading.Tasks;

namespace TaskManagementAPI.Services.Redis;

public interface IRedisService
{
    /// <summary>
    /// 尝试获取分布式锁
    /// </summary>
    /// <param name="lockKey">锁的键名</param>
    /// <param name="token">持有者标识，用于释放时验证</param>
    /// <param name="expiry">锁超时时间</param>
    /// <returns>是否获取成功</returns>
    Task<bool> LockTakeAsync(string lockKey, string token, TimeSpan expiry);

    /// <summary>
    /// 释放分布式锁（只有持有者才能释放）
    /// </summary>
    Task<bool> LockReleaseAsync(string lockKey, string token);

    /// <summary>
    /// 读取String类缓存
    /// </summary>
    /// <param name="key">缓存key</param>
    /// <returns></returns>
    Task<string?> GetStringAsync(string key);

    /// <summary>
    /// 写入String累缓存
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);

    /// <summary>
    /// 判断缓存key存在
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task<bool> KeyExistsAsync(string key);

    /// <summary>
    /// 清除缓存
    /// </summary>
    /// <param name="key">缓存key</param>
    /// <returns></returns>
    Task RemoveAsync(string key);

    /// <summary>
    /// 读取缓存（读取String缓存并反序列化）
    /// </summary>
    /// <typeparam name="T">缓存数据模型</typeparam>
    /// <param name="key">缓存key</param>
    /// <returns></returns>
    Task<T?> GetObjectAsync<T>(string key) where T : class;

    /// <summary>
    /// 写入缓存（序列化后写入String缓存）
    /// </summary>
    /// <typeparam name="T">缓存数据模型</typeparam>
    /// <param name="key">缓存key</param>
    /// <param name="value">缓存值</param>
    /// <param name="expiry">超时</param>
    /// <returns></returns>
    Task SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;

    /// <summary>
    /// 根据缓存key前缀清除缓存
    /// </summary>
    /// <param name="prefix">缓存key前缀</param>
    /// <returns></returns>
    Task RemoveByPrefixAsync(string prefix);

    /// <summary>
    /// 批量写入
    /// </summary>
    /// <param name="keyValues"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    Task BatchSetAsync(Dictionary<string, string> keyValues);

    /// <summary>
    /// 批量读取
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    Task<Dictionary<string, string>> BatchGetAsync(IEnumerable<string> keys);

}