// Repositories/Implementations/ProjectRepository.cs
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Implementations;
using TaskManagementAPI.Services.Redis;
using static TaskManagementAPI.Cache.CacheKeys;

namespace TaskManagementAPI.Repositories.Implementations;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;
    private readonly IRedisService _redis;
    private readonly ILogger<TaskService> _logger;

    public ProjectRepository(AppDbContext context, IRedisService redis, ILogger<TaskService> logger)
    {
        _context = context;
        _redis = redis;
        _logger = logger;
    }

    public async Task<List<TbProject>> GetAllAsync()
    {
        // 1. 先查 Redis 缓存
        var cached = await _redis.GetObjectAsync<List<TbProject>>(ProjectAll);
        if (cached != null && cached.Any())
        {
            Console.WriteLine($"✅ 从 Redis 缓存命中 {ProjectAll}：{cached.Count} 条数据");
            return cached;
        }

        // 2. 缓存未命中，查数据库
        Console.WriteLine("❌ 缓存未命中，查数据库...");
        var data = await _context.TbProjects
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        // 3. 存入 Redis（过期时间 60 秒）
        if (data.Any())
        {
            await _redis.SetObjectAsync(ProjectAll, data, TimeSpan.FromSeconds(60 + new Random().Next(0, 30)));
            Console.WriteLine($"✅ 数据已写入 Redis 缓存 {ProjectAll}：{data.Count} 条");
        }

        return data;
    }

    public async Task<TbProject?> GetByIdAsync(int id)
    {
        var cacheKey = ProjectId(id);
        //// 1. 先用布隆过滤器快速判断
        //if (!await _bloomFilter.ExistsAsync(cacheKey))
        //{
        //    _logger.LogDebug("布隆过滤器拦截不存在的项目 ID：{Id}", id);
        //    return null;
        //}

        // 2. Redis缓存
        var cached = await _redis.GetObjectAsync<TbProject>(cacheKey);
        if (cached != null)
        {
            Console.WriteLine($"✅ 从 Redis 缓存命中：{cacheKey}");
            return cached;
        }

        //检查是否存在"空值标记"
        var isNull = await _redis.KeyExistsAsync($"{cacheKey}:null");
        if (isNull)
        {
            return null; // 说明之前查过，数据库里没有这个 ID
        }

        var data = await _context.TbProjects
            .FirstOrDefaultAsync(p => p.Id == id);
        if (data == null)
        {
            await _redis.SetObjectAsync($"{cacheKey}:null", "1", TimeSpan.FromSeconds(60 + new Random().Next(0, 30)));
            return null;
        }

        await _redis.SetObjectAsync(cacheKey, data, TimeSpan.FromSeconds(60 + new Random().Next(0, 30)));
        Console.WriteLine($"✅ 数据已写入 Redis 缓存：{cacheKey}");

        return data;
    }

    public async Task<List<TbProject>> GetByProjectNameAsync(string name)
    {
        // 把搜索词也放进缓存 key
        var cached = await _redis.GetObjectAsync<List<TbProject>>(ProjectSearchPrefix(name.ToLower()));
        if (cached != null)
        {
            Console.WriteLine($"✅ 从 Redis 缓存命中：${ProjectSearchPrefix(name.ToLower())}");
            return cached;
        }

        var data = await _context.TbProjects
          .Where(p => p.Name.Contains(name))
          .OrderByDescending(p => p.CreatedAt)
          .ToListAsync();

        if (data.Any())
        {
            await _redis.SetObjectAsync(ProjectSearchPrefix(name.ToLower()), data, TimeSpan.FromSeconds(60 + new Random().Next(0, 30)));
            Console.WriteLine($"✅ 数据已写入 Redis 缓存：${ProjectSearchPrefix(name.ToLower())}");
        }

        return data;
    }

    public async Task<TbProject> CreateAsync(TbProject project)
    {
        _context.TbProjects.Add(project);
        await _context.SaveChangesAsync();

       // 添加到布隆过滤器
       //var cacheKey = ProjectId(project.Id);
       // await _bloomFilter.AddAsync(cacheKey);
       // _logger.LogInformation("✅ 项目 {Id} 已加入布隆过滤器", project.Id);

        try
        {
            await Task.WhenAll(
                _redis.RemoveAsync(ProjectAll),
                _redis.RemoveByPrefixAsync(ProjectSearchPrefix())
             );
            Console.WriteLine($"✅ [Create] 已清除缓存：{ProjectAll}");
            Console.WriteLine($"✅ [Create] 已清除缓存：{ProjectSearchPrefix()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [Create] 清除缓存失败（不影响业务）: {ex.Message}");
        }

        try
        {
            await _redis.SetObjectAsync(ProjectId(project.Id), project, TimeSpan.FromSeconds(60 + new Random().Next(0, 30)));
            Console.WriteLine($"✅ [Create] 数据已写入 Redis 缓存：Project {project.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [Create] 写入缓存失败（不影响业务）: {ex.Message}");
        }

        return project;
    }

    public async Task<TbProject> UpdateAsync(TbProject project)
    {
        _context.TbProjects.Update(project);
        await _context.SaveChangesAsync();

        try
        {
            await Task.WhenAll(
              _redis.RemoveAsync(ProjectAll),
              _redis.RemoveAsync(ProjectId(project.Id)),
              _redis.RemoveByPrefixAsync(ProjectSearchPrefix())
           );
            Console.WriteLine($"✅ [Update] 操作已清除缓存：{ProjectAll}");
            Console.WriteLine($"✅ [Update] 操作已清除缓存：{ProjectId(project.Id)}");
            Console.WriteLine($"✅ [Update] 操作已清除缓存：{ProjectSearchPrefix()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [Update] 清除缓存失败（不影响业务）: {ex.Message}");
        }

        return project;
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _context.TbProjects.FindAsync(id);
        if (project == null)
        {
            Console.WriteLine($"⚠️ 尝试删除不存在的项目：{id}");
            return;
        }

        _context.TbProjects.Remove(project);
        await _context.SaveChangesAsync();

        try
        {
            await Task.WhenAll(
                _redis.RemoveAsync(ProjectAll),
                _redis.RemoveAsync(ProjectId(id)),
                _redis.RemoveByPrefixAsync(ProjectSearchPrefix())
            );
            Console.WriteLine($"✅ [Delete] 操作已清除缓存：{ProjectAll}");
            Console.WriteLine($"✅ [Delete] 操作已清除缓存：{ProjectId(id)}");
            Console.WriteLine($"✅ [Delete] 操作已清除缓存：{ProjectSearchPrefix()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [Delete] 清除缓存失败（不影响业务）: {ex.Message}");
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.TbProjects.AnyAsync(p => p.Id == id);
    }
}