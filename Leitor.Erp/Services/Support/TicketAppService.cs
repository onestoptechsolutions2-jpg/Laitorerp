using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.Support;

public class TicketAppService :
    CrudAppService<Ticket, TicketDto, Guid, GetTicketListInput, CreateUpdateTicketDto>
{
    private readonly IRepository<TicketMessage, Guid> _messageRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IClock _clock;
    private readonly IDataFilter _dataFilter;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public TicketAppService(
        IRepository<Ticket, Guid> repository,
        IRepository<TicketMessage, Guid> messageRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IClock clock,
        IDataFilter dataFilter,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
        : base(repository)
    {
        _messageRepository = messageRepository;
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _clock = clock;
        _dataFilter = dataFilter;
        _deletionRequestRepository = deletionRequestRepository;

        GetPolicyName = ErpPermissions.Support.Default;
        GetListPolicyName = ErpPermissions.Support.Default;
        CreatePolicyName = ErpPermissions.Support.Create;
        UpdatePolicyName = ErpPermissions.Support.Edit;
        DeletePolicyName = ErpPermissions.Support.Delete;
    }

    // Messages are an independent aggregate root (see TicketMessage.cs) with no FK relationship
    // configured, so deleting a ticket doesn't cascade automatically - same pattern as
    // CustomerAppService.DeleteAsync.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "Ticket", id);

        var messages = await _messageRepository.GetListAsync(x => x.TicketId == id);
        await _messageRepository.DeleteManyAsync(messages);

        await Repository.DeleteAsync(id);
    }

    protected override async Task<IQueryable<Ticket>> CreateFilteredQueryAsync(GetTicketListInput input)
    {
        input.Sorting ??= $"{nameof(Ticket.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value)
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(input.Priority.HasValue, x => x.Priority == input.Priority!.Value)
            .WhereIf(input.AssignedToUserId.HasValue, x => x.AssignedToUserId == input.AssignedToUserId!.Value)
            .WhereIf(
                !string.IsNullOrWhiteSpace(input.Filter),
                x => x.Subject.Contains(input.Filter!) || x.TicketNumber.Contains(input.Filter!)
            );
    }

    public override async Task<TicketDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<TicketDto>> GetListAsync(GetTicketListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<TicketDto> tickets)
    {
        var customerIds = tickets.Select(x => x.CustomerId).Distinct().ToList();
        var customers = await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id));
        var customerNamesById = customers.ToDictionary(x => x.Id, x => x.Name);

        var userIds = tickets
            .Where(x => x.AssignedToUserId.HasValue)
            .Select(x => x.AssignedToUserId!.Value)
            .Distinct()
            .ToList();
        var usersById = userIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        foreach (var ticket in tickets)
        {
            if (customerNamesById.TryGetValue(ticket.CustomerId, out var customerName))
            {
                ticket.CustomerName = customerName;
            }

            if (ticket.AssignedToUserId.HasValue && usersById.TryGetValue(ticket.AssignedToUserId.Value, out var userName))
            {
                ticket.AssignedToUserName = userName;
            }
        }
    }

    // CreateUpdateTicketDto -> Ticket is mapped manually rather than via Mapperly - same reason as
    // every other entity in this app (protected Id setter + constructor args Mapperly can't
    // resolve from the DTO).
    protected override async Task<Ticket> MapToEntityAsync(CreateUpdateTicketDto createInput)
    {
        var ticketNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "TKT-");

        var entity = new Ticket(GuidGenerator.Create(), createInput.CustomerId, ticketNumber, createInput.Subject);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateTicketDto updateInput, Ticket entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private void CopyToEntity(CreateUpdateTicketDto input, Ticket entity)
    {
        entity.CustomerId = input.CustomerId;
        entity.OrderId = input.OrderId;
        entity.JobId = input.JobId;
        entity.ContractId = input.ContractId;
        entity.Subject = input.Subject;
        entity.Type = input.Type;
        entity.Priority = input.Priority;
        entity.AssignedToUserId = input.AssignedToUserId;
        entity.CustomerSatisfactionRating = input.CustomerSatisfactionRating;

        // Resolved and Closed are both terminal "no longer active work" outcomes (see
        // TicketStatus.cs) - ResolvedDate tracks the transition into either, cleared if reopened,
        // same auto-tracking pattern as FieldServiceJob.CompletedDate/CustomerTask.CompletedAt.
        var wasTerminal = entity.Status is TicketStatus.Resolved or TicketStatus.Closed;
        var isTerminal = input.Status is TicketStatus.Resolved or TicketStatus.Closed;

        if (isTerminal && !wasTerminal)
        {
            entity.ResolvedDate = _clock.Now;
        }
        else if (!isTerminal)
        {
            entity.ResolvedDate = null;
        }

        entity.Status = input.Status;
    }
}
