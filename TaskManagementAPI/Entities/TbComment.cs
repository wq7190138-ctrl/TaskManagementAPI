// Entities/TbComment.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementAPI.Entities;

[Table("tb_Comments")]
public class TbComment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    // 外键
    public int TaskId { get; set; }

    // 导航属性：评论属于一个任务
    [ForeignKey(nameof(TaskId))]
    public TbTask? Task { get; set; }
}