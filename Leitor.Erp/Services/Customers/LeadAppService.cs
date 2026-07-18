using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Customers;

public class LeadAppService :
    CrudAppService<Lead, LeadDto, Guid, GetLeadListInput, CreateUpdateLeadDto>
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public LeadAppService(
        IRepository<Lead, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository)
        : base(repository)
    {
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;

        GetPolicyName = ErpPermissions.Leads.Default;
        GetListPolicyName = ErpPermissions.Leads.Default;
        CreatePolicyName = ErpPermissions.Leads.Create;
        UpdatePolicyName = ErpPermissions.Leads.Edit;
        DeletePolicyName = ErpPermissions.Leads.Delete;
    }

    protected override async Task<IQueryable<Lead>> CreateFilteredQueryAsync(GetLeadListInput input)
    {
        input.Sorting ??= $"{nameof(Lead.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query
            .WhereIf(input.Status.HasValue, x => x.Status == input.Status!.Value)
            .WhereIf(
                !string.IsNullOrWhiteSpace(input.Filter),
                x => x.Name.Contains(input.Filter!) ||
                     (x.CompanyName != null && x.CompanyName.Contains(input.Filter!)) ||
                     (x.Email != null && x.Email.Contains(input.Filter!))
            );
    }

    public override async Task<LeadDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<LeadDto>> GetListAsync(GetLeadListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<LeadDto> leads)
    {
        var userIds = leads
            .Where(x => x.AssignedToUserId.HasValue)
            .Select(x => x.AssignedToUserId!.Value)
            .Distinct()
            .ToList();
        var usersById = userIds.Count > 0
            ? (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.UserName)
            : new Dictionary<Guid, string>();

        foreach (var lead in leads)
        {
            if (lead.AssignedToUserId.HasValue && usersById.TryGetValue(lead.AssignedToUserId.Value, out var userName))
            {
                lead.AssignedToUserName = userName;
            }
        }
    }

    // The concrete mechanism behind "lead becomes a customer" - same shape as
    // QuoteAppService.ConvertToOrderAsync: creates the new entity, carries the obvious fields
    // forward, then marks the source record converted.
    public async Task<Guid> ConvertToCustomerAsync(Guid leadId)
    {
        await CheckCreatePolicyAsync();

        var lead = await Repository.GetAsync(leadId);

        var customer = new Customer(GuidGenerator.Create(), lead.Name)
        {
            Email = lead.Email,
            PhoneNumber = lead.Phone,
            Notes = lead.Notes
        };
        await _customerRepository.InsertAsync(customer, autoSave: true);

        lead.Status = LeadStatus.Converted;
        lead.ConvertedCustomerId = customer.Id;
        await Repository.UpdateAsync(lead, autoSave: true);

        return customer.Id;
    }

    // CreateUpdateLeadDto -> Lead is mapped manually rather than via Mapperly - same reason as
    // every other entity in this app (protected Id setter + constructor args Mapperly can't
    // resolve from the DTO).
    protected override Task<Lead> MapToEntityAsync(CreateUpdateLeadDto createInput)
    {
        var entity = new Lead(GuidGenerator.Create(), createInput.Name);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateLeadDto updateInput, Lead entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateLeadDto input, Lead entity)
    {
        entity.Name = input.Name;
        entity.CompanyName = input.CompanyName;
        entity.Email = input.Email;
        entity.Phone = input.Phone;
        entity.Source = input.Source;
        entity.Status = input.Status;
        entity.AssignedToUserId = input.AssignedToUserId;
        entity.Notes = input.Notes;
    }
}
