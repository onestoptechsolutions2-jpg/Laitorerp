using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Hr;

public class EmployeeDto : FullAuditedEntityDto<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? UserId { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; }
    public string? KraPin { get; set; }
    public string? NssfNumber { get; set; }
    public string? ShaNumber { get; set; }
    public decimal BasicSalary { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
}
