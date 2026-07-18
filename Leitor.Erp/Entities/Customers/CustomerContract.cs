using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Customers;

public class CustomerContract : FullAuditedAggregateRoot<Guid>
{
    public Guid CustomerId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public CustomerContractType Type { get; set; } = CustomerContractType.Maintenance;
    public CustomerContractStatus Status { get; set; } = CustomerContractStatus.Draft;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Value { get; set; }
    public string? Notes { get; set; }

    // Set by ContractExpiryAlertWorker once it emails the account owner about this EndDate: stops
    // the daily worker run from re-sending the same alert. Reset to null whenever EndDate changes
    // (see CustomerContractAppService.CopyToEntity) so a renewal gets its own 30-day alert.
    public DateTime? LastExpiryAlertSentDate { get; set; }

    protected CustomerContract()
    {
    }

    public CustomerContract(Guid id, Guid customerId, string contractNumber, string title)
        : base(id)
    {
        CustomerId = customerId;
        ContractNumber = contractNumber;
        Title = title;
    }
}
