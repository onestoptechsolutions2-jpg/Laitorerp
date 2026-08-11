using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CreateUpdateContractTemplateDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [Range(1, 120)]
    public int? DefaultTermMonths { get; set; }

    public List<CreateUpdateContractTemplateSectionDto> Sections { get; set; } = new();
}

public class CreateUpdateContractTemplateSectionDto
{
    [StringLength(256)]
    public string? Heading { get; set; }

    [StringLength(8000)]
    public string BodyText { get; set; } = string.Empty;
}
