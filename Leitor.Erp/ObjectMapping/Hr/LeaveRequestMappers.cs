using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Services.Dtos.Hr;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Hr;

[Mapper]
public partial class LeaveRequestToLeaveRequestDtoMapper : MapperBase<LeaveRequest, LeaveRequestDto>
{
    [MapperIgnoreSource(nameof(LeaveRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(LeaveRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(LeaveRequestDto.EmployeeName))]
    public override partial LeaveRequestDto Map(LeaveRequest source);

    [MapperIgnoreSource(nameof(LeaveRequest.ExtraProperties))]
    [MapperIgnoreSource(nameof(LeaveRequest.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(LeaveRequestDto.EmployeeName))]
    public override partial void Map(LeaveRequest source, LeaveRequestDto destination);
}
