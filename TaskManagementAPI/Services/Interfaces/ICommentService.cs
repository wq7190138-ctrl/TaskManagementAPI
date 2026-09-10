// Services/Interfaces/ICommentService.cs
using TaskManagementAPI.DTOs.Comment;

namespace TaskManagementAPI.Services.Interfaces;

public interface ICommentService
{
    // 获取任务的所有评论
    Task<List<CommentResponseDto>> GetCommentsByTaskIdAsync(int taskId);

    // 添加评论
    Task<CommentResponseDto> AddCommentAsync(int taskId, CommentCreateDto dto);

    // 删除评论
    Task<bool> DeleteCommentAsync(int id);
}