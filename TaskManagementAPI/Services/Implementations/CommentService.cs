// Services/Implementations/CommentService.cs
using TaskManagementAPI.DTOs.Comment;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Interfaces;

namespace TaskManagementAPI.Services.Implementations;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly ITaskRepository _taskRepository;

    public CommentService(ICommentRepository commentRepository, ITaskRepository taskRepository)
    {
        _commentRepository = commentRepository;
        _taskRepository = taskRepository;
    }

    public async Task<List<CommentResponseDto>> GetCommentsByTaskIdAsync(int taskId)
    {
        var comments = await _commentRepository.GetByTaskIdAsync(taskId);

        return comments.Select(c => new CommentResponseDto
        {
            Id = c.Id,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            CreatedBy = c.CreatedBy,
            TaskId = c.TaskId
        }).ToList();
    }

    public async Task<CommentResponseDto> AddCommentAsync(int taskId, CommentCreateDto dto)
    {
        // 验证任务是否存在
        if (!await _taskRepository.ExistsAsync(taskId))
            throw new Exception($"任务 ID {taskId} 不存在");

        var comment = new TbComment
        {
            Content = dto.Content,
            CreatedBy = dto.CreatedBy,
            TaskId = taskId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.CreateAsync(comment);

        return new CommentResponseDto
        {
            Id = created.Id,
            Content = created.Content,
            CreatedAt = created.CreatedAt,
            CreatedBy = created.CreatedBy,
            TaskId = created.TaskId
        };
    }

    public async Task<bool> DeleteCommentAsync(int id)
    {
        // 检查评论是否存在
        var comments = await _commentRepository.GetByTaskIdAsync(0); // 这里需要优化
                                                                     // 简单起见，直接用 try-catch 或者改 Repository 加 Exists 方法

        try
        {
            await _commentRepository.DeleteAsync(id);
            return true;
        }
        catch
        {
            return false;
        }
    }
}