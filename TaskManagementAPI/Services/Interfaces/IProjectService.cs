// Services/Interfaces/IProjectService.cs
using TaskManagementAPI.DTOs.Project;
using TaskManagementAPI.DTOs.Task;

namespace TaskManagementAPI.Services.Interfaces;

public interface IProjectService
{
    // 获取所有项目
    Task<List<ProjectResponseDto>> GetAllProjectsAsync();
    Task<List<ProjectResponseDto>> GetProjectByProjectNameAsync(string name);

    // 根据 ID 获取项目
    Task<ProjectResponseDto?> GetProjectByIdAsync(int id);

    // 创建项目
    Task<ProjectResponseDto> CreateProjectAsync(ProjectCreateDto dto);

    // 更新项目
    Task<ProjectResponseDto?> UpdateProjectAsync(int id, ProjectUpdateDto dto);

    // 更新项目状态
    Task<ProjectResponseDto?> UpdateProjectStatusAsync(int id, ProjectUpdateStatusDto dto);

    // 删除项目
    Task<bool> DeleteProjectAsync(int id);

    // 获取项目统计
    Task<int> GetProjectTaskCountAsync(int projectId);
}