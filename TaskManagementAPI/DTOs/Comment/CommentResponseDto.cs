// DTOs/Comment/CommentResponseDto.cs
namespace TaskManagementAPI.DTOs.Comment;

public class CommentResponseDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public int TaskId { get; set; }
}