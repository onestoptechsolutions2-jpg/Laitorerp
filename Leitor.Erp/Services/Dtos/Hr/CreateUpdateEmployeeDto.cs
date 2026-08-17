using System;
using System.ComponentModel.DataAnnotations;

namespace Leitor.Erp.Services.Dtos.Hr;

public class CreateUpdateEmployeeDto
{
    [Required]
    [StringLength(256)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(64)]
    public string? Phone { get; set; }

    public Guid? UserId { get; set; }

    [StringLength(128)]
    public string? JobTitle { get; set; }

    [StringLength(128)]
    public string? Department { get; set; }

    [Required]
    public DateTime HireDate { get; set; }

    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; } = true;

    [StringLength(32)]
    public string? KraPin { get; set; }

    [StringLength(32)]
    public string? NssfNumber { get; set; }

    [StringLength(32)]
    public string? ShaNumber { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BasicSalary { get; set; }

    [StringLength(128)]
    public string? BankName { get; set; }

    [StringLength(64)]
    public string? BankAccountNumber { get; set; }
}
