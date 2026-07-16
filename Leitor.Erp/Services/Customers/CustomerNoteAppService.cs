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

// Uses CreateCustomerNoteDto for both the create and update generic slots - there's no separate
// update DTO/flow since notes are an append-only activity log (see CustomerNote.cs); the UI never
// calls the Update endpoint this gives us for free, which is an acceptable minor unused API surface
// in exchange for reusing the same CrudAppService base as the rest of the module.
public class CustomerNoteAppService :
    CrudAppService<CustomerNote, CustomerNoteDto, Guid, GetCustomerNoteListInput, CreateCustomerNoteDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public CustomerNoteAppService(
        IRepository<CustomerNote, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;

        GetPolicyName = ErpPermissions.Customers.Default;
        GetListPolicyName = ErpPermissions.Customers.Default;
        CreatePolicyName = ErpPermissions.Customers.Edit;
        UpdatePolicyName = ErpPermissions.Customers.Edit;
        DeletePolicyName = ErpPermissions.Customers.Edit;
    }

    protected override async Task<IQueryable<CustomerNote>> CreateFilteredQueryAsync(GetCustomerNoteListInput input)
    {
        input.Sorting ??= $"{nameof(CustomerNote.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.CustomerId.HasValue, x => x.CustomerId == input.CustomerId!.Value);
    }

    public override async Task<PagedResultDto<CustomerNoteDto>> GetListAsync(GetCustomerNoteListInput input)
    {
        var result = await base.GetListAsync(input);

        var creatorIds = result.Items
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        if (creatorIds.Count > 0)
        {
            var creators = await _identityUserRepository.GetListAsync(x => creatorIds.Contains(x.Id));
            var namesById = creators.ToDictionary(x => x.Id, x => x.UserName);

            foreach (var note in result.Items)
            {
                if (note.CreatorId.HasValue && namesById.TryGetValue(note.CreatorId.Value, out var userName))
                {
                    note.CreatorUserName = userName;
                }
            }
        }

        return result;
    }

    protected override Task<CustomerNote> MapToEntityAsync(CreateCustomerNoteDto createInput)
    {
        var entity = new CustomerNote(GuidGenerator.Create(), createInput.CustomerId, createInput.Type, createInput.Text);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateCustomerNoteDto updateInput, CustomerNote entity)
    {
        entity.Type = updateInput.Type;
        entity.Text = updateInput.Text;
        return Task.CompletedTask;
    }
}
