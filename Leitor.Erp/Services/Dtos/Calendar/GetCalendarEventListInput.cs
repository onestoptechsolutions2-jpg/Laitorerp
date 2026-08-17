using System;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Calendar;

public class GetCalendarEventListInput : PagedAndSortedResultRequestDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
