using TaskManagementAPI.Enums;

namespace TaskManagementAPI.DTOs.Task
{
    public class TaskForCacheDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public TaskStatusEnum Status { get; set; }
        public TaskPriorityEnum Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public int ProjectId { get; set; }
    }
}
