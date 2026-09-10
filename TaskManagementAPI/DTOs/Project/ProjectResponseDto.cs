// DTOs/Project/ProjectResponseDto.cs
using TaskManagementAPI.DTOs.Task;
using TaskManagementAPI.Enums;

namespace TaskManagementAPI.DTOs.Project;

public class ProjectResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ProjectStatusEnum Status { get; set; } = 0;
    public DateTime? StatusUpdateAt { get; set; }
}