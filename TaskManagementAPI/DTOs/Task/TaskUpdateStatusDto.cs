// DTOs/Task/TaskUpdateStatusDto.cs
using TaskManagementAPI.Enums;

namespace TaskManagementAPI.DTOs.Task;

public class TaskUpdateStatusDto
{
    public TaskStatusEnum Status { get; set; }
}