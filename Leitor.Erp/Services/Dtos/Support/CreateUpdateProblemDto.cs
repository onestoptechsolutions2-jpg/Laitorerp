using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Support;

namespace Leitor.Erp.Services.Dtos.Support;

public class CreateUpdateProblemDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public ProblemStatus Status { get; set; } = ProblemStatus.Open;

    [StringLength(2000)]
    public string? RootCause { get; set; }

    [StringLength(2000)]
    public string? Workaround { get; set; }

    [Required]
    public DateTime IdentifiedDate { get; set; }
}
