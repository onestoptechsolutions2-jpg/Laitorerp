using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class GetFieldServiceJobPartListInput : PagedAndSortedResultRequestDto
{
    public Guid? JobId { get; set; }
}
