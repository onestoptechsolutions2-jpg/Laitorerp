using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.FieldService;

public class GetFieldServiceJobNoteListInput : PagedAndSortedResultRequestDto
{
    public Guid? JobId { get; set; }
}
