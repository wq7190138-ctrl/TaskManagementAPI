using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace TaskManagementAPI.Services.Redis
{
    public class CacheInvalidationSubscriber : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<CacheInvalidationSubscriber> _logger;
        private ISubscriber? _subscriber;

        public CacheInvalidationSubscriber(IServiceProvider services, ILogger<CacheInvalidationSubscriber> logger)
        {
            _services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var redis = _services.GetRequiredService<IConnectionMultiplexer>();
            _subscriber = redis.GetSubscriber();
            var channel = RedisChannel.Literal("cache_invalidate");
            _subscriber.Subscribe(channel, (ch, message) =>
            {
                var cacheKey = message.ToString();
                using var scope = _services.CreateScope();
                var memoryCache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                memoryCache.Remove(cacheKey);
                _logger.LogInformation("✅ 已清理本地 L2 缓存：{cacheKey}", cacheKey);
            });
            _logger.LogInformation("✅ 缓存失效订阅已启动");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _subscriber?.UnsubscribeAll();
            _logger.LogInformation("🛑 缓存失效订阅已停止");
            return Task.CompletedTask;
        }
    }
}
