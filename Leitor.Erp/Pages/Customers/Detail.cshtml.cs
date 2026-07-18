using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.FieldService;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Customers;

[Authorize(Policy = ErpPermissions.Customers.Default)]
public class DetailModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;
    private readonly CustomerContactAppService _customerContactAppService;
    private readonly CustomerContractAppService _customerContractAppService;
    private readonly CustomerNoteAppService _customerNoteAppService;
    private readonly CustomerTaskAppService _customerTaskAppService;
    private readonly CustomerAttachmentAppService _customerAttachmentAppService;
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly TicketAppService _ticketAppService;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        CustomerAppService customerAppService,
        CustomerContactAppService customerContactAppService,
        CustomerContractAppService customerContractAppService,
        CustomerNoteAppService customerNoteAppService,
        CustomerTaskAppService customerTaskAppService,
        CustomerAttachmentAppService customerAttachmentAppService,
        FieldServiceJobAppService fieldServiceJobAppService,
        TicketAppService ticketAppService,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _customerAppService = customerAppService;
        _customerContactAppService = customerContactAppService;
        _customerContractAppService = customerContractAppService;
        _customerNoteAppService = customerNoteAppService;
        _customerTaskAppService = customerTaskAppService;
        _customerAttachmentAppService = customerAttachmentAppService;
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _ticketAppService = ticketAppService;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public CustomerDto Customer { get; set; } = null!;
    public IReadOnlyList<CustomerContactDto> Contacts { get; set; } = Array.Empty<CustomerContactDto>();
    public IReadOnlyList<CustomerContractDto> Contracts { get; set; } = Array.Empty<CustomerContractDto>();
    public IReadOnlyList<CustomerNoteDto> Notes { get; set; } = Array.Empty<CustomerNoteDto>();
    public IReadOnlyList<CustomerTaskDto> TaskItems { get; set; } = Array.Empty<CustomerTaskDto>();
    public IReadOnlyList<CustomerAttachmentDto> Attachments { get; set; } = Array.Empty<CustomerAttachmentDto>();
    public IReadOnlyList<FieldServiceJobDto> FieldServiceJobs { get; set; } = Array.Empty<FieldServiceJobDto>();
    public IReadOnlyList<TicketDto> Tickets { get; set; } = Array.Empty<TicketDto>();

    [BindProperty]
    public CreateCustomerNoteDto NewNote { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Customer", Id);

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Customer = await _customerAppService.GetAsync(Id);

        var contacts = await _customerContactAppService.GetListAsync(new GetCustomerContactListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        Contacts = contacts.Items;

        var contracts = await _customerContractAppService.GetListAsync(new GetCustomerContractListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        Contracts = contracts.Items;

        var notes = await _customerNoteAppService.GetListAsync(new GetCustomerNoteListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        Notes = notes.Items;

        var tasks = await _customerTaskAppService.GetListAsync(new GetCustomerTaskListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        TaskItems = tasks.Items;

        Attachments = await _customerAttachmentAppService.GetListAsync(Id);

        var fieldServiceJobs = await _fieldServiceJobAppService.GetListAsync(new GetFieldServiceJobListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        FieldServiceJobs = fieldServiceJobs.Items;

        var tickets = await _ticketAppService.GetListAsync(new GetTicketListInput
        {
            CustomerId = Id,
            MaxResultCount = 1000
        });
        Tickets = tickets.Items;
    }

    public async Task<IActionResult> OnPostDeleteContactAsync(Guid contactId)
    {
        await _customerContactAppService.DeleteAsync(contactId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteContractAsync(Guid contractId)
    {
        await _customerContractAppService.DeleteAsync(contractId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddNoteAsync()
    {
        NewNote.CustomerId = Id;
        if (!string.IsNullOrWhiteSpace(NewNote.Text))
        {
            await _customerNoteAppService.CreateAsync(NewNote);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteNoteAsync(Guid noteId)
    {
        await _customerNoteAppService.DeleteAsync(noteId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostToggleTaskAsync(Guid taskId)
    {
        var task = await _customerTaskAppService.GetAsync(taskId);
        await _customerTaskAppService.UpdateAsync(taskId, new CreateUpdateCustomerTaskDto
        {
            CustomerId = task.CustomerId,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            AssignedToUserId = task.AssignedToUserId,
            IsCompleted = !task.IsCompleted
        });

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteTaskAsync(Guid taskId)
    {
        await _customerTaskAppService.DeleteAsync(taskId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteAttachmentAsync(Guid attachmentId)
    {
        await _customerAttachmentAppService.DeleteAsync(attachmentId);
        return RedirectToPage(new { id = Id });
    }
}
