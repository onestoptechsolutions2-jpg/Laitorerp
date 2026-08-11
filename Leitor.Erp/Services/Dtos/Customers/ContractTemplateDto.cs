using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class ContractTemplateDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? DefaultTermMonths { get; set; }

    public List<ContractTemplateSectionDto> Sections { get; set; } = new();
}

public class ContractTemplateSectionDto : FullAuditedEntityDto<Guid>
{
    public int Order { get; set; }
    public string? Heading { get; set; }
    public string BodyText { get; set; } = string.Empty;
}
