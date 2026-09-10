// Services/Implementations/ProjectService.cs
using TaskManagementAPI.DTOs.Project;
using TaskManagementAPI.DTOs.Task;
using TaskManagementAPI.Entities;
using TaskManagementAPI.Repositories.Interfaces;
using TaskManagementAPI.Services.Interfaces;

namespace TaskManagementAPI.Services.Implementations;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;

    public ProjectService(IProjectRepository projectRepository, ITaskRepository taskRepository)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
    }

    public async Task<List<ProjectResponseDto>> GetAllProjectsAsync()
    {
        var projects = await _projectRepository.GetAllAsync();

        return projects.Select(p => new ProjectResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            CreatedAt = p.CreatedAt,
            Status = p.Status,
            StatusUpdateAt = p.StatusUpdateAt,
        }).ToList();
    }

    public async Task<ProjectResponseDto?> GetProjectByIdAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);

        if (project == null)
            return null;

        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            CreatedAt = project.CreatedAt,
            Status = project.Status,
            StatusUpdateAt = project.StatusUpdateAt,
        };
    }

    public async Task<List<ProjectResponseDto>> GetProjectByProjectNameAsync(string projectName)
    {
        var projects = await _projectRepository.GetByProjectNameAsync(projectName);

        return projects.Select(p => new ProjectResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            CreatedAt = p.CreatedAt,
            Status = p.Status,
            StatusUpdateAt = p.StatusUpdateAt,
        }).ToList();
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(ProjectCreateDto dto)
    {
        var project = new TbProject
        {
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _projectRepository.CreateAsync(project);

        return new ProjectResponseDto
        {
            Id = created.Id,
            Name = created.Name,
            CreatedAt = created.CreatedAt,
        };
    }

    public async Task<ProjectResponseDto?> UpdateProjectAsync(int id, ProjectUpdateDto dto)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
            return null;

        project.Name = dto.Name;

        var updated = await _projectRepository.UpdateAsync(project);

        return new ProjectResponseDto
        {
            Id = updated.Id,
            Name = updated.Name,
            CreatedAt = updated.CreatedAt,
        };
    }


    public async Task<ProjectResponseDto?> UpdateProjectStatusAsync(int id, ProjectUpdateStatusDto dto)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null)
            return null;

        project.Status = dto.Status;
        project.StatusUpdateAt = DateTime.UtcNow;

        var updated = await _projectRepository.UpdateAsync(project);

        return new ProjectResponseDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Status = updated.Status,
            CreatedAt = updated.CreatedAt,
            StatusUpdateAt = updated.StatusUpdateAt,
        };
    }


    public async Task<bool> DeleteProjectAsync(int id)
    {
        if (!await _projectRepository.ExistsAsync(id))
            return false;

        await _projectRepository.DeleteAsync(id);
        return true;
    }

    public async Task<int> GetProjectTaskCountAsync(int projectId)
    {
        var tasks = await _taskRepository.GetByProjectIdAsync(projectId);
        return tasks.Count;
    }
}