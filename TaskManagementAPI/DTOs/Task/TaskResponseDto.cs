// DTOs/Task/TaskResponseDto.cs
using TaskManagementAPI.DTOs.Comment;
using TaskManagementAPI.Enums;

namespace TaskManagementAPI.DTOs.Task;

public class TaskResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatusEnum Status { get; set; }
    public TaskPriorityEnum Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int ProjectId { get; set; }
}