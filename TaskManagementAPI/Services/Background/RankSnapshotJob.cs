using System.Text.Json;
using TaskManagementAPI.Data;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Services.Redis;

namespace TaskManagementAPI.Services.Background
{
    // BackgroundService：每天凌晨 2 点执行快照
    public class RankSnapshotJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RankSnapshotJob> _logger;

        public RankSnapshotJob(IServiceScopeFactory scopeFactory, ILogger<RankSnapshotJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1).AddHours(2);
                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);

                _logger.LogInformation("开始执行排行榜快照备份...");

                using var scope = _scopeFactory.CreateScope();
                var rankService = scope.ServiceProvider.GetRequiredService<IRedisRankService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var topList = await rankService.GetTopRankAsync("rank:project:active", 100);
                var json = JsonSerializer.Serialize(topList);

                var snapshot = new RankSnapshot
                {
                    SnapshotDate = now.Date,
                    Rankings = json
                };

                dbContext.RankSnapshots.Add(snapshot);
                await dbContext.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("排行榜快照已保存");
            }
        }
    }
}
