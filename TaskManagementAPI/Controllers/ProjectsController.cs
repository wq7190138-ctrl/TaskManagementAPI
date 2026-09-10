// Controllers/ProjectsController.cs
using Microsoft.AspNetCore.Mvc;
using TaskManagementAPI.DTOs.Project;
using TaskManagementAPI.DTOs.Task;
using TaskManagementAPI.Services.Implementations;
using TaskManagementAPI.Services.Interfaces;

namespace TaskManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    // GET: api/projects
    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] string? projectName)
    {
        if (string.IsNullOrEmpty(projectName))
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return Ok(projects);
        }
        else
        {
            var projects = await _projectService.GetProjectByProjectNameAsync(projectName);
            return Ok(projects);
        }
    }

    // GET: api/projects/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await _projectService.GetProjectByIdAsync(id);
        if (project == null)
            return NotFound(new { message = $"项目 ID {id} 不存在" });

        return Ok(project);
    }

    // POST: api/projects
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProjectCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await _projectService.CreateProjectAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    // PUT: api/projects/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProjectUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await _projectService.UpdateProjectAsync(id, dto);
        if (project == null)
            return NotFound(new { message = $"项目 ID {id} 不存在" });

        return Ok(project);
    }

    // PATCH: api/projects/5/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ProjectUpdateStatusDto dto)
    {
        var task = await _projectService.UpdateProjectStatusAsync(id, dto);
        if (task == null)
            return NotFound(new { message = $"任务 ID {id} 不存在" });

        return Ok(task);
    }

    // DELETE: api/projects/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _projectService.DeleteProjectAsync(id);
        if (!result)
            return NotFound(new { message = $"项目 ID {id} 不存在" });

        return NoContent();
    }
}