using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Sales;
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
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<QuoteLine, Guid> _quoteLineRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderLine, Guid> _orderLineRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<FieldServiceJobNote, Guid> _jobNoteRepository;
    private readonly IRepository<FieldServiceJobPart, Guid> _jobPartRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketMessage, Guid> _ticketMessageRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public CustomerAppService(
        IRepository<Customer, Guid> repository,
        IRepository<CustomerContact, Guid> contactRepository,
        IRepository<CustomerContract, Guid> contractRepository,
        IRepository<CustomerNote, Guid> noteRepository,
        IRepository<CustomerTask, Guid> taskRepository,
        IRepository<CustomerAttachment, Guid> attachmentRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<QuoteLine, Guid> quoteLineRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderLine, Guid> orderLineRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<FieldServiceJobNote, Guid> jobNoteRepository,
        IRepository<FieldServiceJobPart, Guid> jobPartRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketMessage, Guid> ticketMessageRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _contactRepository = contactRepository;
        _contractRepository = contractRepository;
        _noteRepository = noteRepository;
        _taskRepository = taskRepository;
        _attachmentRepository = attachmentRepository;
        _identityUserRepository = identityUserRepository;
        _quoteRepository = quoteRepository;
        _quoteLineRepository = quoteLineRepository;
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _paymentRepository = paymentRepository;
        _jobRepository = jobRepository;
        _jobNoteRepository = jobNoteRepository;
        _jobPartRepository = jobPartRepository;
        _ticketRepository = ticketRepository;
        _ticketMessageRepository = ticketMessageRepository;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Create;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Delete;
    }

    // Contacts, contracts, notes, tasks, attachments, and every other module's records that
    // reference this customer (Quotes, Orders, Invoices, FieldServiceJobs, Tickets - all via a
    // non-nullable CustomerId) are independent aggregate roots with no FK relationship configured
    // in ErpDbContext, just an index on CustomerId. Cascade explicitly here so nothing is left
    // with a dangling required CustomerId pointing at a deleted customer.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Customer", id);

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

        var quotes = await _quoteRepository.GetListAsync(x => x.CustomerId == id);
        if (quotes.Count > 0)
        {
            var quoteIds = quotes.Select(x => x.Id).ToList();
            await _quoteLineRepository.DeleteManyAsync(await _quoteLineRepository.GetListAsync(x => quoteIds.Contains(x.QuoteId)));
            await _quoteRepository.DeleteManyAsync(quotes);
        }

        var orders = await _orderRepository.GetListAsync(x => x.CustomerId == id);
        if (orders.Count > 0)
        {
            var orderIds = orders.Select(x => x.Id).ToList();
            await _orderLineRepository.DeleteManyAsync(await _orderLineRepository.GetListAsync(x => orderIds.Contains(x.OrderId)));
            await _orderRepository.DeleteManyAsync(orders);
        }

        var invoices = await _invoiceRepository.GetListAsync(x => x.CustomerId == id);
        if (invoices.Count > 0)
        {
            var invoiceIds = invoices.Select(x => x.Id).ToList();
            await _invoiceLineRepository.DeleteManyAsync(await _invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId)));
            await _paymentRepository.DeleteManyAsync(await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId)));
            await _invoiceRepository.DeleteManyAsync(invoices);
        }

        var jobs = await _jobRepository.GetListAsync(x => x.CustomerId == id);
        if (jobs.Count > 0)
        {
            var jobIds = jobs.Select(x => x.Id).ToList();
            await _jobNoteRepository.DeleteManyAsync(await _jobNoteRepository.GetListAsync(x => jobIds.Contains(x.JobId)));
            await _jobPartRepository.DeleteManyAsync(await _jobPartRepository.GetListAsync(x => jobIds.Contains(x.JobId)));
            await _jobRepository.DeleteManyAsync(jobs);
        }

        var tickets = await _ticketRepository.GetListAsync(x => x.CustomerId == id);
        if (tickets.Count > 0)
        {
            var ticketIds = tickets.Select(x => x.Id).ToList();
            await _ticketMessageRepository.DeleteManyAsync(await _ticketMessageRepository.GetListAsync(x => ticketIds.Contains(x.TicketId)));
            await _ticketRepository.DeleteManyAsync(tickets);
        }

        await Repository.DeleteAsync(id);
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
    }
}
