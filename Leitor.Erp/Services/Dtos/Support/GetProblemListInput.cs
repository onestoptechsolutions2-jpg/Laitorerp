using Leitor.Erp.Entities.Support;
using Volo.Abp.Application.Dtos;

namespace Leitor.Erp.Services.Dtos.Support;

public class GetProblemListInput : PagedAndSortedResultRequestDto
{
    public ProblemStatus? Status { get; set; }
    public string? Filter { get; set; }
}
