// Entities/TbTask.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagementAPI.Enums;

namespace TaskManagementAPI.Entities;

[Table("tb_Tasks")]
public class TbTask
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Todo;
    public TaskPriorityEnum Priority { get; set; } = TaskPriorityEnum.Normal;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }

    // 外键
    public int ProjectId { get; set; }

    // 导航属性：任务属于一个项目
    [ForeignKey(nameof(ProjectId))]
    public TbProject? Project { get; set; }

    // 导航属性：一个任务有多个评论
    public ICollection<TbComment> Comments { get; set; } = new List<TbComment>();
}