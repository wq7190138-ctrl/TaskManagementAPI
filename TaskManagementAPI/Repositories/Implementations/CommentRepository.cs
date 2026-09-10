// Repositories/Implementations/CommentRepository.cs
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Repositories.Interfaces;

namespace TaskManagementAPI.Repositories.Implementations;

public class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _context;

    public CommentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TbComment>> GetByTaskIdAsync(int taskId)
    {
        return await _context.TbComments
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<TbComment> CreateAsync(TbComment comment)
    {
        _context.TbComments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task DeleteAsync(int id)
    {
        var comment = await _context.TbComments.FindAsync(id);
        if (comment != null)
        {
            _context.TbComments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
}