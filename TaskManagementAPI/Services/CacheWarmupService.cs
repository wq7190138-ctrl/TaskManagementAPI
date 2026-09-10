using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.DTOs.Task;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Services.Redis;

namespace TaskManagementAPI.Services
{
    public class CacheWarmupService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<CacheWarmupService> _logger;

        public CacheWarmupService(IServiceProvider services, ILogger<CacheWarmupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🔥 开始执行缓存预热...");

            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var redis = scope.ServiceProvider.GetRequiredService<IRedisService>();

            try
            {
                // 1. 预热项目列表
                var projects = await dbContext.TbProjects.ToListAsync(cancellationToken);
                await WarmupProjectsAsync(projects, redis);

                // 2. 预热任务数据
                var tasks = await dbContext.TbTasks.ToListAsync(cancellationToken);
                await WarmupTasksAsync(tasks, redis);

                _logger.LogInformation("✅ 缓存预热完成！");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 缓存预热失败");
            }
        }

        private async Task WarmupProjectsAsync(List<TbProject> projects, IRedisService redis)
        {
            var batch = new Dictionary<string, string>();
            foreach (var p in projects)
            {
                batch[$"project:{p.Id}"] = JsonSerializer.Serialize(p);
            }
            await redis.BatchSetAsync(batch);
            _logger.LogInformation("✅ 预热项目缓存：{Count} 条", projects.Count);
        }

        private async Task WarmupTasksAsync(List<TbTask> tasks, IRedisService redis)
        {
            var grouped = tasks.GroupBy(t => t.ProjectId);
            foreach (var group in grouped)
            {
                var dtos = group.Select(t => new TaskForCacheDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    DueDate = t.DueDate,
                    ProjectId = t.ProjectId
                }).ToList();

                var key = $"projects:{group.Key}:tasks:all";
                var value = JsonSerializer.Serialize(dtos);
                await redis.SetObjectAsync(key, value, TimeSpan.FromMinutes(5));
            }
            _logger.LogInformation("✅ 预热任务缓存：{Count} 个项目", grouped.Count());
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
