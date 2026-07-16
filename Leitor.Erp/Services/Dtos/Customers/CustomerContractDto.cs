using System;
using Leitor.Erp.Entities.Customers;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Customers;

public class CustomerContractDto : FullAuditedEntityDto<Guid>
{
    public Guid CustomerId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public CustomerContractType Type { get; set; }
    public CustomerContractStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Value { get; set; }
    public string? Notes { get; set; }
}
