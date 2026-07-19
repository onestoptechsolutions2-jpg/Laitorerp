using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.FieldService;
using Leitor.Erp.Services.Opportunities;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Governance.GuidedWorkflow;

public enum GuidedStep
{
    NeedsProposal,
    ProposalDraft,
    ProposalSent,
    ProposalRejected,
    QuoteDraft,
    QuoteSent,
    QuoteDeadEnd,
    OrderSubmitted,
    ScheduleOrSkipInstallation,
    InstallationInProgress,
    ReadyForFinalInvoice,
    Done
}

// One page that walks a deal through the whole Lead -> Customer/Opportunity -> Proposal -> Quote
// -> Order -> Deposit Invoice -> Installation -> Final Invoice chain, so staff don't have to
// bounce between six-plus separate module pages to run one deal end to end. No wizard state is
// stored anywhere - CurrentStep is derived fresh from the live chain on every load, the same
// philosophy WorkflowMonitorAppService.DescribeStage already uses, just scoped to one Opportunity
// and returning enough data to render that step's real inline form. Every action here calls the
// same AppService methods the individual module pages already call - this page only orchestrates.
[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class IndexModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly LeadAppService _leadAppService;
    private readonly ProposalAppService _proposalAppService;
    private readonly QuoteAppService _quoteAppService;
    private readonly QuoteLineAppService _quoteLineAppService;
    private readonly OrderAppService _orderAppService;
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderPaymentMilestone, Guid> _milestoneRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<FieldServiceJob, Guid> _fieldServiceJobRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyOptions _companyOptions;

    public IndexModel(
        OpportunityAppService opportunityAppService,
        LeadAppService leadAppService,
        ProposalAppService proposalAppService,
        QuoteAppService quoteAppService,
        QuoteLineAppService quoteLineAppService,
        OrderAppService orderAppService,
        FieldServiceJobAppService fieldServiceJobAppService,
        IRepository<Lead, Guid> leadRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderPaymentMilestone, Guid> milestoneRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<FieldServiceJob, Guid> fieldServiceJobRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Vendor, Guid> vendorRepository,
        IEmailSender emailSender,
        IOptions<ErpCompanyOptions> companyOptions)
    {
        _opportunityAppService = opportunityAppService;
        _leadAppService = leadAppService;
        _proposalAppService = proposalAppService;
        _quoteAppService = quoteAppService;
        _quoteLineAppService = quoteLineAppService;
        _orderAppService = orderAppService;
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _leadRepository = leadRepository;
        _customerRepository = customerRepository;
        _opportunityRepository = opportunityRepository;
        _proposalRepository = proposalRepository;
        _quoteRepository = quoteRepository;
        _orderRepository = orderRepository;
        _milestoneRepository = milestoneRepository;
        _invoiceRepository = invoiceRepository;
        _fieldServiceJobRepository = fieldServiceJobRepository;
        _identityUserRepository = identityUserRepository;
        _vendorRepository = vendorRepository;
        _emailSender = emailSender;
        _companyOptions = companyOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? OpportunityId { get; set; }

    [BindProperty]
    public CreateUpdateLeadDto NewLead { get; set; } = new();

    [BindProperty]
    public CreateUpdateProposalDto NewProposal { get; set; } = new();

    [BindProperty]
    public CreateUpdateFieldServiceJobDto NewJob { get; set; } = new()
    {
        ScheduledDate = DateTime.Today
    };

    public List<SelectListItem> ResumableOpportunities { get; set; } = new();
    public List<SelectListItem> ConvertibleLeads { get; set; } = new();

    public OpportunityDto? Opportunity { get; set; }
    public Customer? Customer { get; set; }
    public ProposalDto? Proposal { get; set; }
    public QuoteDto? Quote { get; set; }
    public OrderDto? Order { get; set; }
    public List<OrderPaymentMilestone> Milestones { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<FieldServiceJobDto> Jobs { get; set; } = new();
    public GuidedStep CurrentStep { get; set; }

    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> VendorOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (OpportunityId == null)
        {
            await LoadPickerOptionsAsync();
            return;
        }

        await LoadChainAsync(OpportunityId.Value);

        if (CurrentStep == GuidedStep.ScheduleOrSkipInstallation)
        {
            await LoadInstallationOptionsAsync();
        }
    }

    public async Task<IActionResult> OnPostCreateLeadAndOpportunityAsync()
    {
        var lead = await _leadAppService.CreateAsync(NewLead);
        var (_, opportunityId) = await _leadAppService.ConvertToCustomerAsync(lead.Id);
        return RedirectToPage(new { opportunityId });
    }

    public IActionResult OnPostResume(Guid opportunityId)
    {
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostConvertExistingLeadAsync(Guid leadId)
    {
        var (_, opportunityId) = await _leadAppService.ConvertToCustomerAsync(leadId);
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostCreateProposalAsync(Guid opportunityId)
    {
        NewProposal.OpportunityId = opportunityId;
        await _proposalAppService.CreateAsync(NewProposal);
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostSendProposalEmailAsync(Guid opportunityId, Guid proposalId)
    {
        var proposal = await _proposalAppService.GetAsync(proposalId);
        var opportunity = await _opportunityRepository.GetAsync(proposal.OpportunityId);
        var customer = await _customerRepository.GetAsync(opportunity.CustomerId);

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            var pdfBytes = ProposalPdfDocument.Generate(proposal, customer, _companyOptions);
            await _emailSender.SendAsync(
                customer.Email,
                $"Proposal {proposal.ProposalNumber}",
                $"Dear {customer.Name},\n\nPlease find attached proposal {proposal.ProposalNumber}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{proposal.ProposalNumber}.pdf", File = pdfBytes }
                    }
                }
            );

            await _proposalAppService.MarkSentAsync(proposalId, "Email");
        }

        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostMarkProposalWhatsAppAsync(Guid opportunityId, Guid proposalId)
    {
        await _proposalAppService.MarkSentAsync(proposalId, "WhatsApp");
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostConvertProposalToQuoteAsync(Guid opportunityId, Guid proposalId)
    {
        await _proposalAppService.ConvertToQuoteAsync(proposalId);
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostSendQuoteEmailAsync(Guid opportunityId, Guid quoteId)
    {
        var quote = await _quoteAppService.GetAsync(quoteId);
        var customer = await _customerRepository.GetAsync(quote.CustomerId);
        var lines = await _quoteLineAppService.GetListAsync(new GetQuoteLineListInput
        {
            QuoteId = quoteId,
            MaxResultCount = 1000
        });

        if (!string.IsNullOrWhiteSpace(customer.Email))
        {
            var pdfBytes = QuotePdfDocument.Generate(quote, lines.Items, customer, _companyOptions);
            await _emailSender.SendAsync(
                customer.Email,
                $"Quote {quote.QuoteNumber}",
                $"Dear {customer.Name},\n\nPlease find attached quote {quote.QuoteNumber}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{quote.QuoteNumber}.pdf", File = pdfBytes }
                    }
                }
            );
        }

        await _quoteAppService.UpdateAsync(quoteId, new CreateUpdateQuoteDto
        {
            CustomerId = quote.CustomerId,
            Title = quote.Title,
            Status = QuoteStatus.Sent,
            IssueDate = quote.IssueDate,
            ExpiryDate = quote.ExpiryDate,
            Notes = quote.Notes,
            ProposalId = quote.ProposalId
        });

        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostConvertQuoteToOrderAsync(Guid opportunityId, Guid quoteId)
    {
        await _quoteAppService.ConvertToOrderAsync(quoteId);
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostConfirmOrderAsync(Guid opportunityId, Guid orderId)
    {
        var order = await _orderAppService.GetAsync(orderId);
        await _orderAppService.UpdateAsync(orderId, new CreateUpdateOrderDto
        {
            CustomerId = order.CustomerId,
            QuoteId = order.QuoteId,
            Status = OrderStatus.Confirmed,
            OrderDate = order.OrderDate,
            Notes = order.Notes,
            PaymentTerms = order.PaymentTerms
        });
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostScheduleInstallationAsync(Guid opportunityId, Guid orderId)
    {
        var order = await _orderAppService.GetAsync(orderId);
        NewJob.CustomerId = order.CustomerId;
        NewJob.OrderId = orderId;
        await _fieldServiceJobAppService.CreateAsync(NewJob);
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostCompleteInstallationAsync(Guid opportunityId, Guid jobId)
    {
        var job = await _fieldServiceJobAppService.GetAsync(jobId);
        await _fieldServiceJobAppService.UpdateAsync(jobId, new CreateUpdateFieldServiceJobDto
        {
            CustomerId = job.CustomerId,
            OrderId = job.OrderId,
            ContractId = job.ContractId,
            Type = job.Type,
            Status = FieldServiceJobStatus.Completed,
            ScheduledDate = job.ScheduledDate,
            AssignedToUserId = job.AssignedToUserId,
            VendorId = job.VendorId,
            SiteAddress = job.SiteAddress,
            Description = job.Description
        });
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostIssueFinalInvoiceAsync(Guid opportunityId, Guid orderId)
    {
        await _orderAppService.IssueFinalInvoiceAsync(orderId);
        return RedirectToPage(new { opportunityId });
    }

    public async Task<IActionResult> OnPostCloseWonAsync(Guid opportunityId)
    {
        var opportunity = await _opportunityAppService.GetAsync(opportunityId);
        await _opportunityAppService.UpdateAsync(opportunityId, new CreateUpdateOpportunityDto
        {
            CustomerId = opportunity.CustomerId,
            Name = opportunity.Name,
            Status = OpportunityStatus.Won,
            EstimatedValue = opportunity.EstimatedValue,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            AssignedToUserId = opportunity.AssignedToUserId,
            LostReason = opportunity.LostReason,
            Notes = opportunity.Notes
        });
        return RedirectToPage(new { opportunityId });
    }

    private async Task LoadPickerOptionsAsync()
    {
        var opportunities = await _opportunityRepository.GetListAsync(x => x.Status == OpportunityStatus.Open);
        var customerIds = opportunities.Select(x => x.CustomerId).Distinct().ToList();
        var customerNames = (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => x.Name);

        ResumableOpportunities = opportunities
            .OrderByDescending(x => x.CreationTime)
            .Take(25)
            .Select(x => new SelectListItem(
                $"{x.Name} ({(customerNames.TryGetValue(x.CustomerId, out var n) ? n : "")})",
                x.Id.ToString()))
            .ToList();

        var leads = await _leadRepository.GetListAsync(x => x.Status != LeadStatus.Converted);
        ConvertibleLeads = leads
            .OrderByDescending(x => x.CreationTime)
            .Take(25)
            .Select(x => new SelectListItem(
                string.IsNullOrWhiteSpace(x.CompanyName) ? x.Name : $"{x.Name} ({x.CompanyName})",
                x.Id.ToString()))
            .ToList();
    }

    private async Task LoadChainAsync(Guid opportunityId)
    {
        var opportunityEntity = await _opportunityRepository.GetAsync(opportunityId);
        Opportunity = await _opportunityAppService.GetAsync(opportunityId);
        Customer = await _customerRepository.GetAsync(opportunityEntity.CustomerId);

        var proposalEntity = (await _proposalRepository.GetListAsync(x => x.OpportunityId == opportunityId))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (proposalEntity == null)
        {
            CurrentStep = GuidedStep.NeedsProposal;
            return;
        }

        Proposal = await _proposalAppService.GetAsync(proposalEntity.Id);

        if (Proposal.Status == ProposalStatus.Rejected)
        {
            CurrentStep = GuidedStep.ProposalRejected;
            return;
        }

        var quoteEntity = (await _quoteRepository.GetListAsync(x => x.ProposalId == proposalEntity.Id))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (quoteEntity == null)
        {
            CurrentStep = Proposal.Status == ProposalStatus.Draft ? GuidedStep.ProposalDraft : GuidedStep.ProposalSent;
            return;
        }

        Quote = await _quoteAppService.GetAsync(quoteEntity.Id);

        if (Quote.Status is QuoteStatus.Rejected or QuoteStatus.Expired)
        {
            CurrentStep = GuidedStep.QuoteDeadEnd;
            return;
        }

        var orderEntity = (await _orderRepository.GetListAsync(x => x.QuoteId == quoteEntity.Id))
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (orderEntity == null)
        {
            CurrentStep = Quote.Status == QuoteStatus.Draft ? GuidedStep.QuoteDraft : GuidedStep.QuoteSent;
            return;
        }

        Order = await _orderAppService.GetAsync(orderEntity.Id);

        if (Order.Status == OrderStatus.Submitted)
        {
            CurrentStep = GuidedStep.OrderSubmitted;
            return;
        }

        Milestones = (await _milestoneRepository.GetListAsync(x => x.OrderId == orderEntity.Id)).ToList();
        Invoices = (await _invoiceRepository.GetListAsync(x => x.OrderId == orderEntity.Id)).ToList();
        var jobEntities = (await _fieldServiceJobRepository.GetListAsync(x => x.OrderId == orderEntity.Id))
            .OrderBy(x => x.CreationTime)
            .ToList();
        Jobs = new List<FieldServiceJobDto>();
        foreach (var jobEntity in jobEntities)
        {
            // Sequential, not Task.WhenAll - these share one scoped DbContext, which EF Core
            // doesn't allow concurrent operations against.
            Jobs.Add(await _fieldServiceJobAppService.GetAsync(jobEntity.Id));
        }

        var hasFinalInvoice = Milestones.Any(x => x.Kind == OrderPaymentMilestoneKind.Final && x.IsInvoiced)
            || (Order.PaymentTerms != PaymentTerms.Milestone && Invoices.Any());

        if (hasFinalInvoice)
        {
            CurrentStep = GuidedStep.Done;
        }
        else if (Jobs.Count == 0)
        {
            CurrentStep = GuidedStep.ScheduleOrSkipInstallation;
        }
        else if (!Jobs.All(x => x.Status == FieldServiceJobStatus.Completed))
        {
            CurrentStep = GuidedStep.InstallationInProgress;
        }
        else
        {
            CurrentStep = GuidedStep.ReadyForFinalInvoice;
        }
    }

    private async Task LoadInstallationOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString())));

        var vendors = await _vendorRepository.GetListAsync();
        VendorOptions = new List<SelectListItem> { new(L["None"], "") };
        VendorOptions.AddRange(vendors.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())));
    }
}
