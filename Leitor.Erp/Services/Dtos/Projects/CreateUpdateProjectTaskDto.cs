using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Projects;

public class CreateUpdateProjectTaskDto
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsCompleted { get; set; }
}
