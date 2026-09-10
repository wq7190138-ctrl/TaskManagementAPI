// DTOs/StatsResponseDto.cs
namespace TaskManagementAPI.DTOs;

public class StatsResponseDto
{
    public int TotalProjects { get; set; }
    public int TotalTasks { get; set; }
    public int TodoCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int UrgentCount { get; set; }
}