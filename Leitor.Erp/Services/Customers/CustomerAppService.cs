using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Cybersecurity;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Pos;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Customers;

public class CustomerAppService :
    CrudAppService<Customer, CustomerDto, Guid, GetCustomerListInput, CreateUpdateCustomerDto>
{
    private readonly IRepository<CustomerContact, Guid> _contactRepository;
    private readonly IRepository<CustomerContract, Guid> _contractRepository;
    private readonly IRepository<CustomerNote, Guid> _noteRepository;
    private readonly IRepository<CustomerTask, Guid> _taskRepository;
    private readonly IRepository<CustomerAttachment, Guid> _attachmentRepository;
    private readonly IRepository<CustomerPriceList, Guid> _customerPriceListRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<WarrantyClaim, Guid> _warrantyClaimRepository;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<SecurityAssessment, Guid> _securityAssessmentRepository;
    private readonly IRepository<ServiceRequest, Guid> _serviceRequestRepository;
    private readonly IRepository<PosSale, Guid> _posSaleRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IRepository<WorkflowStageEvent, Guid> _stageEventRepository;

    public CustomerAppService(
        IRepository<Customer, Guid> repository,
        IRepository<CustomerContact, Guid> contactRepository,
        IRepository<CustomerContract, Guid> contractRepository,
        IRepository<CustomerNote, Guid> noteRepository,
        IRepository<CustomerTask, Guid> taskRepository,
        IRepository<CustomerAttachment, Guid> attachmentRepository,
        IRepository<CustomerPriceList, Guid> customerPriceListRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<WarrantyClaim, Guid> warrantyClaimRepository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Project, Guid> projectRepository,
        IRepository<SecurityAssessment, Guid> securityAssessmentRepository,
        IRepository<ServiceRequest, Guid> serviceRequestRepository,
        IRepository<PosSale, Guid> posSaleRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository)
        : base(repository)
    {
        _contactRepository = contactRepository;
        _contractRepository = contractRepository;
        _noteRepository = noteRepository;
        _taskRepository = taskRepository;
        _attachmentRepository = attachmentRepository;
        _customerPriceListRepository = customerPriceListRepository;
        _identityUserRepository = identityUserRepository;
        _quoteRepository = quoteRepository;
        _orderRepository = orderRepository;
        _invoiceRepository = invoiceRepository;
        _jobRepository = jobRepository;
        _ticketRepository = ticketRepository;
        _warrantyClaimRepository = warrantyClaimRepository;
        _opportunityRepository = opportunityRepository;
        _projectRepository = projectRepository;
        _securityAssessmentRepository = securityAssessmentRepository;
        _serviceRequestRepository = serviceRequestRepository;
        _posSaleRepository = posSaleRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _stageEventRepository = stageEventRepository;

        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Create;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Delete;
    }

    // Contacts/contracts/notes/tasks/attachments/price-list-overrides have no independent identity
    // of their own (no separate Detail page, never referenced from anywhere else) - true owned
    // children, cascaded here same as always. Everything else that references this customer
    // (Quotes, Orders, Invoices, FieldServiceJobs, Tickets, WarrantyClaims, Opportunities,
    // Projects, SecurityAssessments, ServiceRequests, POS Sales) is its own independent top-level
    // aggregate with its own Delete action - deleting the Customer used to cascade-delete those
    // too, but that's a real business-document loss an ERP shouldn't do silently. Blocked instead
    // (see DependencyGuard) - the operator has to clear or reassign that history first.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Customer", id);

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _quoteRepository.GetListAsync(x => x.CustomerId == id)).Count, "Quote"),
            (async () => (await _orderRepository.GetListAsync(x => x.CustomerId == id)).Count, "Order"),
            (async () => (await _invoiceRepository.GetListAsync(x => x.CustomerId == id)).Count, "Invoice"),
            (async () => (await _jobRepository.GetListAsync(x => x.CustomerId == id)).Count, "Field Service Job"),
            (async () => (await _ticketRepository.GetListAsync(x => x.CustomerId == id)).Count, "Ticket"),
            (async () => (await _warrantyClaimRepository.GetListAsync(x => x.CustomerId == id)).Count, "Warranty Claim"),
            (async () => (await _opportunityRepository.GetListAsync(x => x.CustomerId == id)).Count, "Opportunity"),
            (async () => (await _projectRepository.GetListAsync(x => x.CustomerId == id)).Count, "Project"),
            (async () => (await _securityAssessmentRepository.GetListAsync(x => x.CustomerId == id)).Count, "Security Assessment"),
            (async () => (await _serviceRequestRepository.GetListAsync(x => x.CustomerId == id)).Count, "Service Request"),
            (async () => (await _posSaleRepository.GetListAsync(x => x.CustomerId == id)).Count, "POS Sale")
        );

        var contacts = await _contactRepository.GetListAsync(x => x.CustomerId == id);
        await _contactRepository.DeleteManyAsync(contacts);

        var contracts = await _contractRepository.GetListAsync(x => x.CustomerId == id);
        await _contractRepository.DeleteManyAsync(contracts);

        var notes = await _noteRepository.GetListAsync(x => x.CustomerId == id);
        await _noteRepository.DeleteManyAsync(notes);

        var tasks = await _taskRepository.GetListAsync(x => x.CustomerId == id);
        await _taskRepository.DeleteManyAsync(tasks);

        var attachments = await _attachmentRepository.GetListAsync(x => x.CustomerId == id);
        await _attachmentRepository.DeleteManyAsync(attachments);

        var priceListOverrides = await _customerPriceListRepository.GetListAsync(x => x.CustomerId == id);
        await _customerPriceListRepository.DeleteManyAsync(priceListOverrides);

        await Repository.DeleteAsync(id);
    }

    // Anonymizes PII in place rather than deleting the row - Orders/Invoices/Payments etc. keep a
    // valid CustomerId to reference, so historical financial reporting stays intact. Separate from
    // Delete (see ErpPermissions.Customers.Erase): this is the manual data-subject-erasure action
    // the 2026-07-21 audit's data retention gap called for, distinct from routine record removal.
    // Portal login access is cut by clearing PortalUserId, since a customer whose data has been
    // erased shouldn't retain a working self-service login.
    public async Task EraseDataAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Erase);

        var entity = await Repository.GetAsync(id);
        entity.Name = $"Erased Customer {entity.Id.ToString()[..8]}";
        entity.Email = null;
        entity.PhoneNumber = null;
        entity.AddressLine = null;
        entity.City = null;
        entity.State = null;
        entity.PostalCode = null;
        entity.Country = null;
        entity.Notes = null;
        entity.PortalUserId = null;
        await Repository.UpdateAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(_stageEventRepository, GuidGenerator, CurrentUser, Clock, "Customer", entity.Id, WorkflowStage.DataErased);
    }

    public override async Task<CustomerDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveAccountOwnerNamesAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<CustomerDto>> GetListAsync(GetCustomerListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveAccountOwnerNamesAsync(result.Items);
        return result;
    }

    private async Task ResolveAccountOwnerNamesAsync(IReadOnlyCollection<CustomerDto> customers)
    {
        var userIds = customers
            .Where(x => x.AccountOwnerUserId.HasValue)
            .Select(x => x.AccountOwnerUserId!.Value)
            .Concat(customers.Where(x => x.PortalUserId.HasValue).Select(x => x.PortalUserId!.Value))
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return;
        }

        var users = await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id));
        var namesById = users.ToDictionary(x => x.Id, x => x.UserName);

        foreach (var customer in customers)
        {
            if (customer.AccountOwnerUserId.HasValue &&
                namesById.TryGetValue(customer.AccountOwnerUserId.Value, out var ownerName))
            {
                customer.AccountOwnerUserName = ownerName;
            }

            if (customer.PortalUserId.HasValue &&
                namesById.TryGetValue(customer.PortalUserId.Value, out var portalUserName))
            {
                customer.PortalUserName = portalUserName;
            }
        }
    }

    protected override async Task<IQueryable<Customer>> CreateFilteredQueryAsync(GetCustomerListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(
            !string.IsNullOrWhiteSpace(input.Filter),
            x => x.Name.Contains(input.Filter!) ||
                 (x.Email != null && x.Email.Contains(input.Filter!))
        );
    }

    // CreateUpdateCustomerDto -> Customer is mapped manually rather than via Mapperly: Customer's
    // Id has a protected setter and its constructor requires a generated Guid, which Mapperly
    // cannot resolve from the DTO. See ObjectMapping/Customers/CustomerMappers.cs for the (safe)
    // Entity -> Dto direction, which Mapperly does handle.
    protected override Task<Customer> MapToEntityAsync(CreateUpdateCustomerDto createInput)
    {
        var entity = new Customer(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateCustomerDto updateInput, Customer entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateCustomerDto input, Customer entity)
    {
        entity.Name = input.Name;
        entity.Email = input.Email;
        entity.PhoneNumber = input.PhoneNumber;
        entity.AddressLine = input.AddressLine;
        entity.City = input.City;
        entity.State = input.State;
        entity.PostalCode = input.PostalCode;
        entity.Country = input.Country;
        entity.Status = input.Status;
        entity.Notes = input.Notes;
        entity.AccountOwnerUserId = input.AccountOwnerUserId;
        entity.PortalUserId = input.PortalUserId;
        entity.DefaultPaymentTerms = input.DefaultPaymentTerms;
        entity.DefaultPriceListId = input.DefaultPriceListId;
        entity.CreditLimit = input.CreditLimit;
        entity.DefaultCurrencyCode = input.DefaultCurrencyCode;
        entity.DiscountPercent = input.DiscountPercent;
    }
}
