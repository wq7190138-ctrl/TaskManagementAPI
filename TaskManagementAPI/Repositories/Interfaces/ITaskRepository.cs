// Repositories/Interfaces/ITaskRepository.cs
using TaskManagementAPI.DTOs.Task;
using TaskManagementAPI.Entities;

namespace TaskManagementAPI.Repositories.Interfaces;

public interface ITaskRepository
{
    // 获取项目下的所有任务
    Task<List<TbTask>> GetByProjectIdAsync(int projectId);

    // 根据 ID 获取任务（含评论）
    Task<TbTask?> GetByIdAsync(int id);

    // 创建任务
    Task CreateAsync(TbTask task);

    // 更新任务
    Task UpdateAsync(TbTask task);

    // 删除任务
    Task DeleteAsync(TbTask task);

    // 判断任务是否存在
    Task<bool> ExistsAsync(int id);

    // 判断任务是否存在（任务名称）
    Task<bool> ExistsByNameAsync(int projectId, string title);
}