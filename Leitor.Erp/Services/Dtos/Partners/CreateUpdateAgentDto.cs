using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Partners;

namespace Leitor.Erp.Services.Dtos.Partners;

public class CreateUpdateAgentDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(64)]
    public string? Phone { get; set; }

    [StringLength(128)]
    public string? Territory { get; set; }

    [StringLength(512)]
    public string? Skills { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public CommissionBasis CommissionBasis { get; set; } = CommissionBasis.Percentage;
    public decimal CommissionRate { get; set; }
    public CommissionTrigger CommissionTrigger { get; set; } = CommissionTrigger.OnClientPayment;
}
