using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace Leitor.Erp.Entities.Hr;

// Distinct from Entities/Partners/Agent.cs, which is documented there as "a person who refers
// business or does field work WITHOUT being an employee" - an actual staff member is modeled
// separately. UserId is an optional self-service login link, same convention as
// Customer.PortalUserId: a loose Guid?, no navigation property, presence of the link is itself
// the authorization for self-service actions (e.g. submitting a LeaveRequest) - not every
// employee needs or gets a login.
public class Employee : FullAuditedAggregateRoot<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? UserId { get; set; }

    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Kenya statutory identifiers - optional strings, not every employee has all three captured
    // on day one.
    public string? KraPin { get; set; }
    public string? NssfNumber { get; set; }
    public string? ShaNumber { get; set; }

    // Monthly gross basic salary, KES - the input PayeCalculator/PayrollRunAppService compute
    // statutory deductions from (see Services/PayeCalculator.cs, Services/Hr/PayrollRunAppService.cs).
    public decimal BasicSalary { get; set; }

    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }

    protected Employee()
    {
    }

    public Employee(Guid id, string fullName, DateTime hireDate)
        : base(id)
    {
        FullName = fullName;
        HireDate = hireDate;
    }
}
