// Entities/TbProject.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TaskManagementAPI.Enums;

namespace TaskManagementAPI.Entities;

[Table("tb_Projects")]
public class TbProject
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ProjectStatusEnum Status { get; set; } = ProjectStatusEnum.Todo;

    public DateTime? StatusUpdateAt { get; set; }
    // 导航属性：一个项目有多个任务
    public ICollection<TbTask> Tasks { get; set; } = new List<TbTask>();
}