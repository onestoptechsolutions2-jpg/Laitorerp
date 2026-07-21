using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Support.Problems;

[Authorize(Policy = ErpPermissions.Support.Default)]
public class DetailModel : AbpPageModel
{
    private readonly ProblemAppService _problemAppService;
    private readonly TicketAppService _ticketAppService;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        ProblemAppService problemAppService,
        TicketAppService ticketAppService,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _problemAppService = problemAppService;
        _ticketAppService = ticketAppService;
        _ticketRepository = ticketRepository;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ProblemDto Problem { get; set; } = null!;
    public IReadOnlyList<TicketDto> LinkedTickets { get; set; } = Array.Empty<TicketDto>();

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Problem", Id);
        Problem = await _problemAppService.GetAsync(Id);

        var linkedTicketIds = (await _ticketRepository.GetListAsync(x => x.ProblemId == Id)).Select(x => x.Id).ToList();
        var tickets = new List<TicketDto>();
        foreach (var ticketId in linkedTicketIds)
        {
            tickets.Add(await _ticketAppService.GetAsync(ticketId));
        }
        LinkedTickets = tickets;
    }

    public async Task<IActionResult> OnPostSetStatusAsync(ProblemStatus status)
    {
        var problem = await _problemAppService.GetAsync(Id);
        await _problemAppService.UpdateAsync(Id, new CreateUpdateProblemDto
        {
            Title = problem.Title,
            Description = problem.Description,
            Status = status,
            RootCause = problem.RootCause,
            Workaround = problem.Workaround,
            IdentifiedDate = problem.IdentifiedDate
        });

        return RedirectToPage(new { id = Id });
    }
}
