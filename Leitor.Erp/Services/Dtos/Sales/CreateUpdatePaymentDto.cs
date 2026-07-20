using System;
using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Sales;

namespace Leitor.Erp.Services.Dtos.Sales;

public class CreateUpdatePaymentDto
{
    [Required]
    public Guid InvoiceId { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;

    [StringLength(128)]
    public string? Reference { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    // Optional - defaults from the parent Invoice's CurrencyCode when left blank (see
    // PaymentAppService.MapToEntityAsync).
    [StringLength(8)]
    public string? CurrencyCode { get; set; }
}
