// Repositories/Implementations/TaskRepository.cs
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Redis;

namespace TaskManagementAPI.Repositories.Implementations;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;
    private readonly IRedisService _redis;

    public TaskRepository(AppDbContext context, IRedisService redis)
    {
        _context = context;
        _redis = redis;
    }

    public async Task<List<TbTask>> GetByProjectIdAsync(int projectId)
    {
        return await _context.TbTasks
            .Where(t => t.ProjectId == projectId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TbTask?> GetByIdAsync(int id)
    {
        return await _context.TbTasks.FindAsync(id);
    }

    public async Task CreateAsync(TbTask task)
    {
        _context.TbTasks.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TbTask task)
    {
        _context.TbTasks.Update(task);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(TbTask task)
    {
        _context.TbTasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.TbTasks.AnyAsync(t => t.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(int projectId, string title)
    {
        return await _context.TbTasks.AnyAsync(t => t.Title == title && t.ProjectId == projectId);
    }
}