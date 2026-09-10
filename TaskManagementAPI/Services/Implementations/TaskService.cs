// Services/Implementations/TaskService.cs
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using TaskManagementAPI.Cache;
using TaskManagementAPI.DTOs.Task;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Hubs;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Interfaces;
using TaskManagementAPI.Services.RabbitMQ;
using TaskManagementAPI.Services.Redis;
using TaskManagementAPI.Tools;

namespace TaskManagementAPI.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IRedisService _redis;
    private readonly ILogger<TaskService> _logger;
    private readonly IRabbitMQService _rabbitMQ;
    private readonly ISubscriber _subscriber;
    private readonly IRedisRankService _rankService;
    private readonly IRedisListService _listService;
    private readonly string _logKey = "logs:task:actions";
    private readonly IHubContext<CacheNotificationHub> _hubContext;
    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IRedisService redis,
        ILogger<TaskService> logger,
        IMemoryCache memoryCache,
        IRabbitMQService rabbitMQ,
        IConnectionMultiplexer connection,
        IRedisRankService rankService,
        IRedisListService listService,
        IHubContext<CacheNotificationHub> hubContext)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _redis = redis;
        _logger = logger;
        _rabbitMQ = rabbitMQ;
        _memoryCache = memoryCache;
        _subscriber = connection.GetSubscriber();
        _rankService = rankService;
        _listService = listService;
        _hubContext = hubContext;
    }

    public async Task<List<TaskResponseDto>> GetTasksByProjectIdAsync(int projectId)
    {
        var cacheKey = CacheKeys.TaskAll(projectId);
        var memoryKey = $"memory_{cacheKey}";

        // --- 1. 先查内存缓存 (L2) ---
        if (_memoryCache.TryGetValue(memoryKey, out List<TaskResponseDto>? memory) && memory != null)
        {
            _logger.LogInformation("✅ L2缓存命中：CacheKey={memoryKey}, Count={Count}", memoryKey, memory.Count);
            return memory;
        }

        _logger.LogInformation("❌ L2缓存未命中：CacheKey={memoryKey}", memoryKey);

        // --- 2. 查 Redis (L3) ---
        var cached = await TryGetFromRedisAsync<List<TaskResponseDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("✅ L3缓存命中并回填L2：CacheKey={cacheKey}, Count={Count}", cacheKey, cached.Count);
            SetMemoryCache(memoryKey, cached, TimeSpan.FromSeconds(30));
            return cached;
        }

        _logger.LogInformation("❌ L3缓存未命中：CacheKey={cacheKey}", cacheKey);

        // --- 3. 缓存未命中，执行防击穿逻辑 ---
        return await FetchFromDatabaseWithLockAsync(projectId, cacheKey, memoryKey);
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
    {
        var cacheKey = CacheKeys.TaskId(id);
        var memoryKey = $"memory_{cacheKey}";

        if (_memoryCache.TryGetValue(memoryKey, out TaskResponseDto? memory) && memory != null)
        {
            _logger.LogInformation("✅ L2缓存命中：CacheKey={memoryKey}", memoryKey);
            return memory;
        }

        try
        {
            var cached = await _redis.GetObjectAsync<TaskResponseDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("✅ L3缓存命中并回填L2：CacheKey={cacheKey}", cacheKey);
                _memoryCache.Set(memoryKey, cached, TimeSpan.FromSeconds(30));
                return cached;
            }

            var isNull = await _redis.KeyExistsAsync($"{cacheKey}:null");
            if (isNull)
            {
                _logger.LogInformation("✅ L3缓存命中：CacheKey={cacheKey}", $"{cacheKey}:null");
                return null;
            }

            _logger.LogInformation("❌ L3缓存未命中：CacheKey={cacheKey}", cacheKey);

        }
        catch (Exception ex)
        {
            // 缓存失败，记录日志但不影响主流程（降级到查数据库）
            _logger.LogWarning(ex, "❌ Redis 缓存读取失败，降级到数据库查询。CacheKey={cacheKey}", cacheKey);
        }

        try
        {
            var task = await _taskRepository.GetByIdAsync(id);

            if (task == null)
            {
                await _redis.SetObjectAsync($"{cacheKey}:null", "1", TimeSpan.FromSeconds(60 + new Random().Next(0, 30)));
                return null;
            }

            var dto = new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId,
            };

            try
            {
                //缓存
                var expiry = TimeSpan.FromSeconds(60 + Random.Shared.Next(0, 30));
                await _redis.SetObjectAsync(cacheKey, dto, expiry);
                _logger.LogInformation("✅ L3 缓存写入成功：CacheKey={cacheKey}, Expiry={Expiry}s", cacheKey, expiry.TotalSeconds);

                _memoryCache.Set(memoryKey, dto, TimeSpan.FromSeconds(30));
                _logger.LogInformation("✅ L2 内存缓存写入成功：CacheKey={memoryKey}, Expiry=30s", cacheKey);
            }
            catch (Exception ex)
            {
                // 缓存写入失败不影响返回结果，只记录日志
                _logger.LogWarning(ex, "❌ Redis 缓存写入失败，但数据库查询成功。CacheKey={cacheKey}", cacheKey);
            }

            return dto;
        }
        catch (Exception ex)
        {
            // 数据库查询失败，记录错误并抛出（让上层全局异常处理或控制器处理）
            _logger.LogError(ex, "❌ 数据库查询失败。TaskId={TaskId}", id);
            throw; // 重新抛出异常，保留堆栈
        }
    }

    public async Task<TaskResponseDto> CreateTaskAsync(int projectId, TaskCreateDto dto)
    {
        // 验证
        if (!await _projectRepository.ExistsAsync(projectId))
            throw new KeyNotFoundException($"项目 ID {projectId} 不存在");
        if (await _taskRepository.ExistsByNameAsync(projectId, dto.Title))
            throw new InvalidOperationException($"项目 {projectId} 中已存在名为 '{dto.Title}' 的任务");

        TbTask task = new TbTask
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            CreatedAt = DateTime.UtcNow,
            DueDate = dto.DueDate,
            ProjectId = projectId
        };

        try
        {
            // 延迟双删：先删缓存，再写数据库，延迟后再删缓存，可以
            await InvalidateCache(CacheKeys.TaskAll(projectId));

            // 通过 SignalR 通知客户端
            await _hubContext.Clients.All.SendAsync("CacheInvalidated", CacheKeys.TaskAll(projectId));

            // 保存
            await _taskRepository.CreateAsync(task);

            // 清除缓存（新任务需要出现在列表中）
            try
            {
                _ = Task.Delay(500).ContinueWith(async _ =>
                {
                    await InvalidateCache(CacheKeys.TaskAll(projectId));
                    _logger.LogInformation($"✅ 延迟双删完成");
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ 清除缓存失败，不影响业务");
            }

            // 消息队列
            await PublicshMQ("task_created", new { TaskId = task.Id, ProjectId = task.ProjectId, Title = task.Title });

            await _rabbitMQ.PublishDelayedAsync(
                exchange: "order.delay.exchange",
                routingKey: "order.delay",
                message: new { TaskId = task.Id, ProjectId = task.ProjectId, Title = task.Title },
                ttlMilliseconds: 5 * 1000
            );

            //给项目加1积分
            await _rankService.IncrementScoreAsync("rank:project:active", projectId.ToString(), 1);

            // 记录操作日志
            await _listService.PushLogAsync(_logKey,
           $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | 用户创建任务：{dto.Title} (项目 {projectId})");

            _logger.LogInformation("✅ 项目{projectId} 添加任务'{title}'，积分 +1", task.ProjectId, task.Title);

            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId
            };
        }
        catch (Exception ex)
        {
            // 数据库查询失败，记录错误并抛出（让上层全局异常处理或控制器处理）
            _logger.LogError(ex, "❌ 创建任务失败");
            throw; // 重新抛出异常，保留堆栈
        }
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(int id, TaskUpdateDto dto)
    {
        var lockKey = $"task_update_{id}";
        var token = Guid.NewGuid().ToString();
        if (!await _redis.LockTakeAsync(lockKey, token, TimeSpan.FromSeconds(30)))
        {
            throw new InvalidOperationException("当前任务正在被其他请求处理，请稍后重试");
        }

        //Thread.Sleep(TimeSpan.FromSeconds(15)); //模拟长事务，测试分布式锁
        try
        {
            var task = await _taskRepository.GetByIdAsync(id);

            //深拷贝
            var oldTask = JsonSerializer.Deserialize<TbTask>(JsonSerializer.Serialize(task));
            if (task == null)
                throw new KeyNotFoundException($"Task with id {id} not found.");

            var projectId = task.ProjectId;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.Priority = dto.Priority;
            task.DueDate = dto.DueDate;
            task.UpdatedAt = DateTime.UtcNow;

            await InvalidateCache(CacheKeys.TaskAll(projectId));
            await InvalidateCache(CacheKeys.TaskId(id));
            await _hubContext.Clients.All.SendAsync("CacheInvalidated", CacheKeys.TaskAll(projectId));
            await _hubContext.Clients.All.SendAsync("CacheInvalidated", CacheKeys.TaskId(id));

            await _taskRepository.UpdateAsync(task);

            // 更新操作需要作废缓存：1. 当前项目Id下所有Task的缓存 2. GetTaskById的缓存
            try
            {
                _ = Task.Delay(500).ContinueWith(async _ =>
                {
                    await InvalidateCache(CacheKeys.TaskAll(projectId));
                    await InvalidateCache(CacheKeys.TaskId(id));
                    _logger.LogInformation($"✅ 延迟双删完成");
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ 清除缓存失败，不影响业务");
            }

            // 消息队列
            await PublicshMQ("task_updated", new { TaskId = task.Id, Updated = JsonDiff.GetDiff(oldTask, task) });

            // 记录操作日志
            await _listService.PushLogAsync(_logKey, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | 用户修改任务：{dto.Title} (项目 {projectId})：{JsonDiff.GetDiff(oldTask, task)}");

            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                CreatedAt = task.CreatedAt,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId
            };
        }
        catch (Exception ex)
        {
            // 数据库查询失败，记录错误并抛出（让上层全局异常处理或控制器处理）
            _logger.LogError(ex, "❌ 修改任务失败。TaskId={taskId}", id);
            throw; // 重新抛出异常，保留堆栈
        }
        finally
        {
            await _redis.LockReleaseAsync(lockKey, token);
        }
    }

    public async Task<TaskResponseDto?> UpdateTaskStatusAsync(int id, TaskUpdateStatusDto dto)
    {
        var lockKey = $"task_update_{id}";
        var token = Guid.NewGuid().ToString();
        if (!await _redis.LockTakeAsync(lockKey, token, TimeSpan.FromSeconds(30)))
        {
            throw new InvalidOperationException("当前任务正在被其他请求处理，请稍后重试");
        }

        //Thread.Sleep(TimeSpan.FromSeconds(5)); //模拟长事务，测试分布式锁
        try
        {
            var task = await _taskRepository.GetByIdAsync(id);
            if (task == null)
                throw new KeyNotFoundException($"Task with id {id} not found.");

            task.Status = dto.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await InvalidateCache(CacheKeys.TaskId(id));
            await InvalidateCache(CacheKeys.TaskAll(task.ProjectId));
            await _hubContext.Clients.All.SendAsync("CacheInvalidated", CacheKeys.TaskId(id));
            await _hubContext.Clients.All.SendAsync("CacheInvalidated", CacheKeys.TaskAll(task.ProjectId));
            await _taskRepository.UpdateAsync(task);

            // 延迟双删
            try
            {
                _ = Task.Delay(500).ContinueWith(async _ =>
                {
                    await InvalidateCache(CacheKeys.TaskId(id));
                    await InvalidateCache(CacheKeys.TaskAll(task.ProjectId));
                    _logger.LogInformation($"✅ 延迟双删完成");
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "❌ 清除缓存失败，不影响业务");
            }

            //给项目加2积分
            if (dto.Status == Enums.TaskStatusEnum.Completed)
            {
                await _rankService.IncrementScoreAsync("rank:project:active", task.ProjectId.ToString(), 2);
                _logger.LogInformation("✅ 任务'{title}'完成，项目{projectId} 积分 +2", task.Title, task.ProjectId);
            }
            else
            {
                await _rankService.IncrementScoreAsync("rank:project:active", task.ProjectId.ToString(), -2);
                _logger.LogInformation("✅ 任务'{title}'变更未完成，项目{projectId} 积分 -2", task.Title, task.ProjectId);
            }

            // 记录操作日志
            await _listService.PushLogAsync(_logKey,
           $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | 用户变更任务：{task.Title} (项目 {task.ProjectId}) Status变更为：{dto.Status}");


            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId
            };
        }
        catch (Exception ex)
        {
            // 数据库查询失败，记录错误并抛出（让上层全局异常处理或控制器处理）
            _logger.LogError(ex, "❌ 更新任务状态失败。TaskId={taskId}", id);
            throw; // 重新抛出异常，保留堆栈
        }
        finally
        {
            await _redis.LockReleaseAsync(lockKey, token);
        }
    }

    public async Task DeleteTaskAsync(int id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
            throw new KeyNotFoundException($"任务 ID {id} 不存在");

        var projectId = task.ProjectId;

        await InvalidateCache(CacheKeys.TaskAll(task.ProjectId));
        await InvalidateCache(CacheKeys.TaskId(id));
        await _hubContext.Clients.All.SendAsync("CacheInvalidated", CacheKeys.TaskAll(task.ProjectId));
        await _hubContext.Clients.All.SendAsync("CacheInvalidated", CacheKeys.TaskId(id));
        await _taskRepository.DeleteAsync(task);

        await _rankService.IncrementScoreAsync("rank:project:active", projectId.ToString(), -2);
        _logger.LogInformation("✅ 任务 '{title}' 删除，项目{projectId} 积分 -2", task.Title, projectId);

        // 记录操作日志
        await _listService.PushLogAsync(_logKey,
       $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | 用户删除任务：{task.Title} (项目 {task.ProjectId})");


        //清除缓存（单个任务 + 项目任务列表）
        try
        {
            _ = Task.Delay(500).ContinueWith(async _ =>
            {
                await InvalidateCache(CacheKeys.TaskAll(task.ProjectId));
                await InvalidateCache(CacheKeys.TaskId(id));
                _logger.LogInformation($"✅ 延迟双删完成");
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ 清除缓存失败，不影响业务");
        }

    }

    /// <summary>
    /// 清除过期缓存
    /// </summary>
    /// <param name="cacheKey">缓存key</param>
    private async Task InvalidateCache(string cacheKey)
    {
        // 1. 清除本地 L2 缓存
        _memoryCache.Remove($"memory_{cacheKey}");
        _logger.LogInformation($"✅ 已清除本地 L2 缓存：memory_{cacheKey}");

        // 2. 清除 Redis L3 缓存
        await _redis.RemoveAsync(cacheKey);

        // 3. 通过 Redis Pub/Sub 通知其他实例
        await _subscriber.PublishAsync(RedisChannel.Literal("cache_invalidate"), $"memory_{cacheKey}");
        _logger.LogInformation($"📢 已发送缓存失效通知：memory_{cacheKey}");
    }

    /// <summary>
    /// 尝试从 Redis 读取，失败返回 null
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <returns></returns>
    private async Task<T?> TryGetFromRedisAsync<T>(string key) where T : class
    {
        try
        {
            return await _redis.GetObjectAsync<T>(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Redis 读取失败，降级处理。Key={Key}", key);
            return null;
        }
    }

    /// <summary>
    /// 设置内存缓存，忽略异常
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="expiry"></param>
    private void SetMemoryCache(string key, object value, TimeSpan expiry)
    {
        try
        {
            _memoryCache.Set(key, value, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 内存缓存写入失败。Key={Key}", key);
        }
    }

    /// <summary>
    /// 带分布式锁的数据库查询
    /// </summary>
    /// <param name="projectId"></param>
    /// <param name="cacheKey"></param>
    /// <param name="memoryKey"></param>
    /// <returns></returns>
    private async Task<List<TaskResponseDto>> FetchFromDatabaseWithLockAsync(int projectId, string cacheKey, string memoryKey)
    {
        var lockKey = $"lock:{cacheKey}";
        var token = Guid.NewGuid().ToString();
        var lockExpiry = TimeSpan.FromSeconds(5); // 锁超时时间（业务应在此时间内完成）

        // 尝试获取分布式锁
        if (await _redis.LockTakeAsync(lockKey, token, lockExpiry))
        {
            _logger.LogInformation("✅ 获取锁，开始查询数据库。CacheKey={cacheKey}", cacheKey);
            try
            {
                // 获取锁后，再次检查缓存（双重检查，防止其他实例已更新）
                var cachedAgain = await TryGetFromRedisAsync<List<TaskResponseDto>>(cacheKey);
                if (cachedAgain != null)
                {
                    _logger.LogInformation("✅ 双重检查：L3缓存已存在，直接返回。CacheKey={cacheKey}", cacheKey);
                    SetMemoryCache(memoryKey, cachedAgain, TimeSpan.FromSeconds(30));
                    return cachedAgain;
                }

                // 查询数据库
                var tasks = await _taskRepository.GetByProjectIdAsync(projectId);
                var dtos = tasks.Select(t => new TaskResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    DueDate = t.DueDate,
                    ProjectId = t.ProjectId,
                }).ToList();

                // 写入缓存
                _logger.LogInformation("⏳ 模拟耗时操作 5 秒...");
                //Thread.Sleep(TimeSpan.FromSeconds(5));
                await WriteCacheAsync(cacheKey, memoryKey, dtos);

                return dtos;
            }
            finally
            {
                // 释放锁
                await _redis.LockReleaseAsync(lockKey, token);
                _logger.LogInformation("✅ 释放锁。CacheKey={cacheKey}", cacheKey);
            }
        }
        else
        {
            // 未能获取锁，等待并重试读取缓存（最多等待 3 秒）
            _logger.LogInformation("⏳ 未能获取锁，等待其他实例完成数据加载... CacheKey={cacheKey}", cacheKey);
            for (int i = 0; i < 6; i++) // 尝试 6 次，每次等待 500ms
            {
                await Task.Delay(500);
                var cachedAfterWait = await TryGetFromRedisAsync<List<TaskResponseDto>>(cacheKey);
                if (cachedAfterWait != null)
                {
                    _logger.LogInformation("✅ 等待后从L3缓存命中。CacheKey={cacheKey}", cacheKey);
                    SetMemoryCache(memoryKey, cachedAfterWait, TimeSpan.FromSeconds(30));
                    return cachedAfterWait;
                }
            }

            // 等待超时，降级为直接查询数据库（但风险较大，记录警告）
            _logger.LogWarning("⚠️ 等待缓存超时，直接查询数据库。CacheKey={cacheKey}", cacheKey);
            var tasks = await _taskRepository.GetByProjectIdAsync(projectId);
            var dtos = tasks.Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                DueDate = t.DueDate,
                ProjectId = t.ProjectId,
            }).ToList();

            // 尝试写入缓存（无论成功与否）
            await WriteCacheAsync(cacheKey, memoryKey, dtos);
            return dtos;
        }
    }

    // 写入缓存（忽略异常）
    private async Task WriteCacheAsync(string cacheKey, string memoryKey, List<TaskResponseDto> data)
    {
        try
        {
            var redisExpiry = TimeSpan.FromSeconds(60 + Random.Shared.Next(0, 30));
            await _redis.SetObjectAsync(cacheKey, data, redisExpiry);
            _logger.LogInformation("✅ L3 Redis缓存写入成功：CacheKey={cacheKey}, Count={Count}", cacheKey, data.Count);
            SetMemoryCache(memoryKey, data, TimeSpan.FromSeconds(30));
            _logger.LogInformation("✅ L2内存缓存写入成功：CacheKey={memoryKey}, Count={Count}", memoryKey, data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ 缓存写入失败，但数据已从数据库获取。CacheKey={cacheKey}", cacheKey);
        }
    }

    private async Task PublicshMQ<T>(string queue, T message)
    {

        // MQ消息
        try
        {
            await _rabbitMQ.PublishWithConfirmAsync(queue, message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ 发送 RabbitMQ 消息失败，不影响业务");
        }
    }
}