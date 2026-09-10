// Controllers/CommentsController.cs
using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.DTOs.Comment;
using TaskManagementAPI.Services.Interfaces;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ITaskService _taskService;

    public CommentsController(ICommentService commentService, ITaskService taskService)
    {
        _commentService = commentService;
        _taskService = taskService;
    }

    // GET: api/tasks/5/comments
    [HttpGet("~/api/tasks/{taskId:int}/comments")]
    public async Task<IActionResult> GetByTask(int taskId)
    {
        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null)
            return NotFound(new { message = $"任务 ID {taskId} 不存在" });

        var comments = await _commentService.GetCommentsByTaskIdAsync(taskId);
        return Ok(comments);
    }

    // POST: api/tasks/5/comments
    [HttpPost("~/api/tasks/{taskId:int}/comments")]
    public async Task<IActionResult> Create(int taskId, [FromBody] CommentCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var comment = await _commentService.AddCommentAsync(taskId, dto);
            return CreatedAtAction(nameof(GetByTask), new { taskId = taskId }, comment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/comments/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _commentService.DeleteCommentAsync(id);
        if (!result)
            return NotFound(new { message = $"评论 ID {id} 不存在" });

        return NoContent();
    }
}