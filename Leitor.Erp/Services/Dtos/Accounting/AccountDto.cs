using System;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class AccountDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public SystemAccountRole SystemRole { get; set; }
    public bool IsActive { get; set; } = true;
}
