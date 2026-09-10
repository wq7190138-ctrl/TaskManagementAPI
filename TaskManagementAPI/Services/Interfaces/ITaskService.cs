// Services/Interfaces/ITaskService.cs
using TaskManagementAPI.DTOs.Task;

namespace TaskManagementAPI.Services.Interfaces;

public interface ITaskService
{
    // 获取项目下的所有任务
    Task<List<TaskResponseDto>> GetTasksByProjectIdAsync(int projectId);

    // 根据 ID 获取任务
    Task<TaskResponseDto?> GetTaskByIdAsync(int id);

    // 创建任务
    Task<TaskResponseDto> CreateTaskAsync(int projectId, TaskCreateDto dto);

    // 更新任务
    Task<TaskResponseDto?> UpdateTaskAsync(int id, TaskUpdateDto dto);

    // 更新任务状态
    Task<TaskResponseDto?> UpdateTaskStatusAsync(int id, TaskUpdateStatusDto dto);

    // 删除任务
    Task DeleteTaskAsync(int id);
}