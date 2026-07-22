using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class DepreciationEntryDto : FullAuditedEntityDto<Guid>
{
    public Guid FixedAssetId { get; set; }
    public DateTime PeriodDate { get; set; }
    public decimal Amount { get; set; }
    public Guid JournalEntryId { get; set; }
}
