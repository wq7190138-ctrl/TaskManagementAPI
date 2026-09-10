// Repositories/Interfaces/ICommentRepository.cs
using TaskManagementAPI.Entities;

namespace TaskManagementAPI.Repositories.Interfaces;

public interface ICommentRepository
{
    // 获取任务下的所有评论
    Task<List<TbComment>> GetByTaskIdAsync(int taskId);

    // 创建评论
    Task<TbComment> CreateAsync(TbComment comment);

    // 删除评论
    Task DeleteAsync(int id);
}