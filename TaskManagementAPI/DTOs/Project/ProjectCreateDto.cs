// DTOs/Project/ProjectCreateDto.cs
using System.ComponentModel.DataAnnotations;

namespace TaskManagementAPI.DTOs.Project;

public class ProjectCreateDto
{
    [Required(ErrorMessage = "项目名称不能为空")]
    [MaxLength(100, ErrorMessage = "项目名称不能超过100个字符")]
    public string Name { get; set; } = string.Empty;
}