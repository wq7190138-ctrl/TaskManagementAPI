// Repositories/Interfaces/IProjectRepository.cs
using TaskManagementAPI.Entities;

namespace TaskManagementAPI.Repositories.Interfaces;

public interface IProjectRepository
{
    // 获取所有项目（含任务）
    Task<List<TbProject>> GetAllAsync();

    // 根据 ID 获取项目（含任务）
    Task<TbProject?> GetByIdAsync(int id);

    // 根据 Name 获取项目（含任务）
    Task<List<TbProject>> GetByProjectNameAsync(string name);

    // 创建项目
    Task<TbProject> CreateAsync(TbProject project);

    // 更新项目
    Task<TbProject> UpdateAsync(TbProject project);

    // 删除项目
    Task DeleteAsync(int id);

    // 判断项目是否存在
    Task<bool> ExistsAsync(int id);
}