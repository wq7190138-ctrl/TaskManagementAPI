// DTOs/Comment/CommentCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.DTOs.Comment;

public class CommentCreateDto
{
    [Required(ErrorMessage = "评论内容不能为空")]
    [MaxLength(500, ErrorMessage = "评论内容不能超过500个字符")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "评论人不能为空")]
    [MaxLength(50, ErrorMessage = "评论人名称不能超过50个字符")]
    public string CreatedBy { get; set; } = string.Empty;
}