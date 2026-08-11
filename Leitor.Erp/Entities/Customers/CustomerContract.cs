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

    // Per-contract SLA response targets, in hours, one per Ticket priority tier. Null means "use
    // the fixed default table" (see TicketAppService.ResolveSlaWindowAsync) - a contract only needs
    // to set the tiers it actually wants to override (e.g. a Platinum support contract sets all
    // four tighter; most contracts set none and just inherit the default).
    public int? SlaUrgentHours { get; set; }
    public int? SlaHighHours { get; set; }
    public int? SlaMediumHours { get; set; }
    public int? SlaLowHours { get; set; }

    // Which of the retainer's 13 possible services this specific contract actually covers - see
    // ContractServiceScope. Defaults to None (nothing recorded) rather than everything, since not
    // every contract is the full managed-IT-and-cybersecurity bundle.
    public ContractServiceScope ServicesIncluded { get; set; } = ContractServiceScope.None;

    // Set by ContractExpiryAlertWorker once it emails the account owner about this EndDate: stops
    // the daily worker run from re-sending the same alert. Reset to null whenever EndDate changes
    // (see CustomerContractAppService.CopyToEntity) so a renewal gets its own 30-day alert.
    public DateTime? LastExpiryAlertSentDate { get; set; }

    // When set, "Generate PDF" is available on Customer Detail (see
    // CustomersDetailModel.OnGetContractPdfAsync) - null for contracts created before this feature
    // or that don't need a generated legal document. ClientSignatoryName is captured per contract
    // rather than pulled from Customer, since the signing representative isn't otherwise tracked
    // anywhere on the Customer record.
    public Guid? ContractTemplateId { get; set; }
    public string? ClientSignatoryName { get; set; }

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
