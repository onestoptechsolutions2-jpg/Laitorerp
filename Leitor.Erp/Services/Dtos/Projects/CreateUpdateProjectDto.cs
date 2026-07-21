using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Projects;

namespace Leitor.Erp.Services.Dtos.Projects;

public class CreateUpdateProjectDto
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Budget { get; set; }
}
