using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CreateUpdateBankAccountDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(64)]
    public string? AccountNumber { get; set; }

    [StringLength(128)]
    public string? BankName { get; set; }

    [Required]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required]
    public Guid LinkedGlAccountId { get; set; }

    public decimal OpeningBalance { get; set; }
    public DateTime OpeningBalanceDate { get; set; } = DateTime.Today;
}
