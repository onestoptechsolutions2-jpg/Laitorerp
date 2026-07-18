using System;
using System.Collections.Generic;
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

namespace Leitor.Erp.Pages.Support.Tickets;

[Authorize(Policy = ErpPermissions.Support.Default)]
public class DetailModel : AbpPageModel
{
    private readonly TicketAppService _ticketAppService;
    private readonly TicketMessageAppService _ticketMessageAppService;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        TicketAppService ticketAppService,
        TicketMessageAppService ticketMessageAppService,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _ticketAppService = ticketAppService;
        _ticketMessageAppService = ticketMessageAppService;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public TicketDto Ticket { get; set; } = null!;
    public IReadOnlyList<TicketMessageDto> Messages { get; set; } = Array.Empty<TicketMessageDto>();

    [BindProperty]
    public CreateTicketMessageDto NewMessage { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Ticket", Id);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Ticket = await _ticketAppService.GetAsync(Id);

        var messages = await _ticketMessageAppService.GetListAsync(new GetTicketMessageListInput
        {
            TicketId = Id,
            MaxResultCount = 1000
        });
        Messages = messages.Items;
    }

    public async Task<IActionResult> OnPostSetStatusAsync(TicketStatus status)
    {
        var ticket = await _ticketAppService.GetAsync(Id);
        await _ticketAppService.UpdateAsync(Id, new CreateUpdateTicketDto
        {
            CustomerId = ticket.CustomerId,
            OrderId = ticket.OrderId,
            JobId = ticket.JobId,
            ContractId = ticket.ContractId,
            Subject = ticket.Subject,
            Type = ticket.Type,
            Status = status,
            Priority = ticket.Priority,
            AssignedToUserId = ticket.AssignedToUserId,
            CustomerSatisfactionRating = ticket.CustomerSatisfactionRating
        });

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddMessageAsync()
    {
        NewMessage.TicketId = Id;
        if (!string.IsNullOrWhiteSpace(NewMessage.Text))
        {
            await _ticketMessageAppService.CreateAsync(NewMessage);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteMessageAsync(Guid messageId)
    {
        await _ticketMessageAppService.DeleteAsync(messageId);
        return RedirectToPage(new { id = Id });
    }
}
