using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.FieldService;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Opportunities;
using Leitor.Erp.Services.Sales;
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
    private readonly OpportunityAppService _opportunityAppService;
    private readonly QuoteAppService _quoteAppService;
    private readonly OrderAppService _orderAppService;
    private readonly InvoiceAppService _invoiceAppService;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
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
        OpportunityAppService opportunityAppService,
        QuoteAppService quoteAppService,
        OrderAppService orderAppService,
        InvoiceAppService invoiceAppService,
        IRepository<Lead, Guid> leadRepository,
        IRepository<Proposal, Guid> proposalRepository,
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
        _opportunityAppService = opportunityAppService;
        _quoteAppService = quoteAppService;
        _orderAppService = orderAppService;
        _invoiceAppService = invoiceAppService;
        _leadRepository = leadRepository;
        _proposalRepository = proposalRepository;
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

    // 360 pipeline/finance view - the Quote/Order/Invoice repositories already existed in
    // CustomerAppService for cascade-delete; this surfaces the same data for display instead.
    public Lead? OriginatingLead { get; set; }
    public IReadOnlyList<OpportunityDto> Opportunities { get; set; } = Array.Empty<OpportunityDto>();
    public IReadOnlyList<Proposal> Proposals { get; set; } = Array.Empty<Proposal>();
    public IReadOnlyList<QuoteDto> Quotes { get; set; } = Array.Empty<QuoteDto>();
    public IReadOnlyList<OrderDto> Orders { get; set; } = Array.Empty<OrderDto>();
    public IReadOnlyList<InvoiceDto> Invoices { get; set; } = Array.Empty<InvoiceDto>();

    public decimal LifetimeRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int TotalOrders { get; set; }
    public int OpenOpportunities { get; set; }
    public decimal? WinRate { get; set; }
    public decimal? AverageDealSize { get; set; }

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

        OriginatingLead = (await _leadRepository.GetListAsync(x => x.ConvertedCustomerId == Id)).FirstOrDefault();

        // FieldService.Default/Support.Default/Opportunities.Default/Sales.Default aren't granted
        // to every role that holds Customers.Default (e.g. a Procurement/Dispatcher role can view
        // Customers but not Field Service or Support) - gate each section the same way
        // DashboardAppService already gates its own sections, rather than letting GetListAsync
        // throw for those roles.
        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Default))
        {
            var fieldServiceJobs = await _fieldServiceJobAppService.GetListAsync(new GetFieldServiceJobListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            FieldServiceJobs = fieldServiceJobs.Items;
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Default))
        {
            var tickets = await _ticketAppService.GetListAsync(new GetTicketListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            Tickets = tickets.Items;
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Default))
        {
            var opportunities = await _opportunityAppService.GetListAsync(new GetOpportunityListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            Opportunities = opportunities.Items;

            var opportunityIds = Opportunities.Select(x => x.Id).ToList();
            Proposals = opportunityIds.Count > 0
                ? (await _proposalRepository.GetListAsync(x => opportunityIds.Contains(x.OpportunityId)))
                    .OrderByDescending(x => x.CreationTime)
                    .ToList()
                : Array.Empty<Proposal>();
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Default))
        {
            var quotes = await _quoteAppService.GetListAsync(new GetQuoteListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            Quotes = quotes.Items;

            var orders = await _orderAppService.GetListAsync(new GetOrderListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            Orders = orders.Items;

            var invoices = await _invoiceAppService.GetListAsync(new GetInvoiceListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            Invoices = invoices.Items;
        }

        LifetimeRevenue = Invoices.Sum(x => x.Total);
        OutstandingBalance = Invoices.Sum(x => Math.Max(0, x.Total - x.AmountPaid));
        TotalOrders = Orders.Count;
        OpenOpportunities = Opportunities.Count(x => x.Status == OpportunityStatus.Open);

        var won = Opportunities.Count(x => x.Status == OpportunityStatus.Won);
        var lost = Opportunities.Count(x => x.Status == OpportunityStatus.Lost);
        WinRate = won + lost > 0 ? (decimal)won / (won + lost) : null;
        AverageDealSize = Orders.Count > 0 ? Orders.Average(x => x.Total) : null;
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
