using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Partners;

namespace Leitor.Erp.Services.Dtos.Partners;

public class CreateUpdatePartnerDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    public PartnerCategory Category { get; set; } = PartnerCategory.Other;

    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(64)]
    public string? Phone { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public CommissionBasis CommissionBasis { get; set; } = CommissionBasis.Percentage;
    public decimal CommissionRate { get; set; }
    public CommissionTrigger CommissionTrigger { get; set; } = CommissionTrigger.OnClientPayment;
}
