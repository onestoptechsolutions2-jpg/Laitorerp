using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.FieldService;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class CreateFieldServiceJobNoteDto
{
    [Required]
    public Guid JobId { get; set; }

    public FieldServiceJobNoteType Type { get; set; } = FieldServiceJobNoteType.General;

    [Required]
    [StringLength(4000)]
    public string Text { get; set; } = string.Empty;
}
