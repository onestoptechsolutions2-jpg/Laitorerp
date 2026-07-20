using System.ComponentModel.DataAnnotations;
using Leitor.Erp.Entities.Accounting;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CreateUpdateAccountDto
{
    [Required]
    [StringLength(16)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    public SystemAccountRole SystemRole { get; set; } = SystemAccountRole.None;

    public bool IsActive { get; set; } = true;
}
