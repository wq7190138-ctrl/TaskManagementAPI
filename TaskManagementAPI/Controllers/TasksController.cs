// Controllers/TasksController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.DTOs.Task;
using TaskManagementAPI.Services.Implementations;
using TaskManagementAPI.Services.Interfaces;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly ILogger<TaskService> _logger;

    public TasksController(ITaskService taskService, IProjectService projectService, ILogger<TaskService> logger)
    {
        _taskService = taskService;
        _projectService = projectService;
        _logger = logger;
    }

    // GET: api/tasks/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _taskService.GetTaskByIdAsync(id);
        if (task == null)
            return NotFound(new { message = $"任务 ID {id} 不存在" });

        return Ok(task);
    }

    // GET: api/projects/5/tasks
    [HttpGet("~/api/projects/{projectId:int}/tasks")]
    public async Task<IActionResult> GetByProject(int projectId)
    {
        var project = await _projectService.GetProjectByIdAsync(projectId);
        if (project == null)
            return NotFound(new { message = $"项目 ID {projectId} 不存在" });

        var tasks = await _taskService.GetTasksByProjectIdAsync(projectId);
        return Ok(tasks);
    }

    // POST: api/projects/5/tasks
    [HttpPost("~/api/projects/{projectId:int}/tasks")]
    public async Task<IActionResult> Create(int projectId, [FromBody] TaskCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = await _taskService.CreateTaskAsync(projectId, dto);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);

    }

    // PUT: api/tasks/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TaskUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var task = await _taskService.UpdateTaskAsync(id, dto);
        return Ok(task);
    }

    // PATCH: api/tasks/5/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] TaskUpdateStatusDto dto)
    {
        var result = await _taskService.UpdateTaskStatusAsync(id, dto);
        return Ok(result);
    }

    // DELETE: api/tasks/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _taskService.DeleteTaskAsync(id);
        return NoContent();
    }
}