using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Dtos.ServiceRequests;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.FieldService;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Opportunities;
using Leitor.Erp.Services.Projects;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Services.ServiceRequests;
using Leitor.Erp.Services.Support;
using Leitor.Erp.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Settings;

namespace Leitor.Erp.Pages.Customers;

[Authorize(Policy = ErpPermissions.Customers.Default)]
public class DetailModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;
    private readonly CustomerContactAppService _customerContactAppService;
    private readonly CustomerContractAppService _customerContractAppService;
    private readonly ContractTemplateAppService _contractTemplateAppService;
    private readonly ErpCompanyProfileProvider _companyProfileProvider;
    private readonly ISettingProvider _settingProvider;
    private readonly CustomerNoteAppService _customerNoteAppService;
    private readonly CustomerTaskAppService _customerTaskAppService;
    private readonly CustomerAttachmentAppService _customerAttachmentAppService;
    private readonly CustomerPriceListAppService _customerPriceListAppService;
    private readonly PriceListAppService _priceListAppService;
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly TicketAppService _ticketAppService;
    private readonly SupportAnalyticsAppService _supportAnalyticsAppService;
    private readonly OpportunityAppService _opportunityAppService;
    private readonly QuoteAppService _quoteAppService;
    private readonly OrderAppService _orderAppService;
    private readonly InvoiceAppService _invoiceAppService;
    private readonly LeadTouchAppService _leadTouchAppService;
    private readonly ServiceRequestAppService _serviceRequestAppService;
    private readonly ProjectAppService _projectAppService;
    private readonly IFeatureChecker _featureChecker;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        CustomerAppService customerAppService,
        CustomerContactAppService customerContactAppService,
        CustomerContractAppService customerContractAppService,
        ContractTemplateAppService contractTemplateAppService,
        ErpCompanyProfileProvider companyProfileProvider,
        ISettingProvider settingProvider,
        CustomerNoteAppService customerNoteAppService,
        CustomerTaskAppService customerTaskAppService,
        CustomerAttachmentAppService customerAttachmentAppService,
        CustomerPriceListAppService customerPriceListAppService,
        PriceListAppService priceListAppService,
        FieldServiceJobAppService fieldServiceJobAppService,
        TicketAppService ticketAppService,
        SupportAnalyticsAppService supportAnalyticsAppService,
        OpportunityAppService opportunityAppService,
        QuoteAppService quoteAppService,
        OrderAppService orderAppService,
        InvoiceAppService invoiceAppService,
        LeadTouchAppService leadTouchAppService,
        ServiceRequestAppService serviceRequestAppService,
        ProjectAppService projectAppService,
        IFeatureChecker featureChecker,
        IRepository<Lead, Guid> leadRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _customerAppService = customerAppService;
        _customerContactAppService = customerContactAppService;
        _customerContractAppService = customerContractAppService;
        _contractTemplateAppService = contractTemplateAppService;
        _companyProfileProvider = companyProfileProvider;
        _settingProvider = settingProvider;
        _customerNoteAppService = customerNoteAppService;
        _customerTaskAppService = customerTaskAppService;
        _customerAttachmentAppService = customerAttachmentAppService;
        _customerPriceListAppService = customerPriceListAppService;
        _priceListAppService = priceListAppService;
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _ticketAppService = ticketAppService;
        _supportAnalyticsAppService = supportAnalyticsAppService;
        _opportunityAppService = opportunityAppService;
        _quoteAppService = quoteAppService;
        _orderAppService = orderAppService;
        _invoiceAppService = invoiceAppService;
        _leadTouchAppService = leadTouchAppService;
        _serviceRequestAppService = serviceRequestAppService;
        _projectAppService = projectAppService;
        _featureChecker = featureChecker;
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
    public CustomerSlaPerformanceDto? SlaPerformance { get; set; }
    public IReadOnlyList<ServiceRequestDto> ServiceRequests { get; set; } = Array.Empty<ServiceRequestDto>();
    public IReadOnlyList<ProjectDto> Projects { get; set; } = Array.Empty<ProjectDto>();

    // 360 pipeline/finance view - the Quote/Order/Invoice repositories already existed in
    // CustomerAppService for cascade-delete; this surfaces the same data for display instead.
    public Lead? OriginatingLead { get; set; }
    public IReadOnlyList<LeadTouchDto> LeadTouches { get; set; } = Array.Empty<LeadTouchDto>();
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

    public List<CustomerPriceListDto> CustomerPriceLists { get; set; } = new();
    public List<PriceListDto> AvailablePriceLists { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool CanErase { get; set; }
    public bool HasPendingDeletionRequest { get; set; }
    public int TaskDueSoonLeadDays { get; set; } = 3;

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Edit);
        CanErase = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Erase);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "Customer", Id);
        TaskDueSoonLeadDays = int.TryParse(await _settingProvider.GetOrNullAsync(ErpSettings.TaskDueSoonLeadDays), out var leadDays) ? leadDays : 3;

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

        // Surfaces the Lead's pre-conversion contact history on the Customer it became - LeadTouch
        // rows stay keyed to LeadId (never migrated to CustomerId), so this reads through
        // OriginatingLead rather than needing a schema change.
        if (OriginatingLead != null && await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Default))
        {
            var touches = await _leadTouchAppService.GetListAsync(new GetLeadTouchListInput
            {
                LeadId = OriginatingLead.Id,
                MaxResultCount = 1000
            });
            LeadTouches = touches.Items;
        }

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
            SlaPerformance = await _supportAnalyticsAppService.GetCustomerSlaPerformanceAsync(Id);
        }

        // ServiceRequestManagement is a toggleable module (unlike Support/FieldService above,
        // which are always-on) - gate on the feature too, or GetListAsync's own
        // [RequiresFeature] throws for a deployment that has it off.
        if (await _featureChecker.IsEnabledAsync(ErpFeatures.ServiceRequestManagement) &&
            await AuthorizationService.IsGrantedAsync(ErpPermissions.ServiceRequests.Default))
        {
            var serviceRequests = await _serviceRequestAppService.GetListAsync(new GetServiceRequestListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            ServiceRequests = serviceRequests.Items;
        }

        if (await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement) &&
            await AuthorizationService.IsGrantedAsync(ErpPermissions.Projects.Default))
        {
            var projects = await _projectAppService.GetListAsync(new GetProjectListInput
            {
                CustomerId = Id,
                MaxResultCount = 1000
            });
            Projects = projects.Items;
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

            // Load customer price lists for price list management section
            try
            {
                CustomerPriceLists = await _customerPriceListAppService.GetListAsync(Id);

                // Get all available price lists (for dropdown)
                var allPriceLists = await _priceListAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
                AvailablePriceLists = allPriceLists.Items.ToList();
            }
            catch
            {
                // If price list loading fails, don't crash the page - just leave empty
                CustomerPriceLists = new List<CustomerPriceListDto>();
                AvailablePriceLists = new List<PriceListDto>();
            }
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

    // Only reachable for a contract that picked a template (see the "Pdf" button gate on
    // Detail.cshtml) - GetAsync would 404 anyway for a mismatched id, but the ContractTemplateId
    // check here gives a clearer NotFound rather than a null-reference deeper in ContractPdfDocument.
    public async Task<IActionResult> OnGetContractPdfAsync(Guid contractId)
    {
        var contract = await _customerContractAppService.GetAsync(contractId);
        if (!contract.ContractTemplateId.HasValue)
        {
            return NotFound();
        }

        var template = await _contractTemplateAppService.GetAsync(contract.ContractTemplateId.Value);
        var customer = await _customerAppService.GetAsync(contract.CustomerId);
        var company = await _companyProfileProvider.GetAsync();
        var companySignatoryName = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyContractSignatoryName);

        var pdfBytes = ContractPdfDocument.Generate(contract, template, customer, company, companySignatoryName);
        return File(pdfBytes, "application/pdf", $"{contract.ContractNumber}.pdf");
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

    public async Task<IActionResult> OnPostEraseDataAsync()
    {
        await _customerAppService.EraseDataAsync(Id);
        return RedirectToPage(new { id = Id });
    }

    [BindProperty]
    public Guid PriceListId { get; set; }

    public async Task<IActionResult> OnPostAddPriceListAsync()
    {
        if (PriceListId == Guid.Empty)
        {
            return RedirectToPage(new { id = Id });
        }

        var input = new CreateUpdateCustomerPriceListDto
        {
            PriceListId = PriceListId,
            IsPrimary = false
        };

        try
        {
            await _customerPriceListAppService.AddAsync(Id, input);
        }
        catch (UserFriendlyException)
        {
            // Price list already assigned, silently ignore redirect
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemovePriceListAsync(Guid priceListId)
    {
        await _customerPriceListAppService.RemoveAsync(priceListId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostSetPrimaryAsync(Guid priceListId)
    {
        await _customerPriceListAppService.SetPrimaryAsync(priceListId);
        return RedirectToPage(new { id = Id });
    }
}
