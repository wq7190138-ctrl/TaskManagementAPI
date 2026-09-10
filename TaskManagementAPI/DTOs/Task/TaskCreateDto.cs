// DTOs/Task/TaskCreateDto.cs
using System.ComponentModel.DataAnnotations;
using TaskManagementAPI.Enums;

namespace TaskManagementAPI.DTOs.Task;

public class TaskCreateDto
{
    [Required(ErrorMessage = "任务标题不能为空")]
    [MaxLength(200, ErrorMessage = "任务标题不能超过200个字符")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "任务描述不能超过500个字符")]
    public string? Description { get; set; }

    public TaskPriorityEnum Priority { get; set; } = TaskPriorityEnum.Normal;

    public DateTime? DueDate { get; set; }
}