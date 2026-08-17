using System;
using Leitor.Erp.Entities.Hr;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Hr;

public class GetLeaveRequestListInput : PagedAndSortedResultRequestDto
{
    public Guid? EmployeeId { get; set; }
    public LeaveRequestStatus? Status { get; set; }
}
