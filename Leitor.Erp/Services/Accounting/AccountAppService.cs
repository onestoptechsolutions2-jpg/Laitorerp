using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

public class AccountAppService :
    CrudAppService<Account, AccountDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateAccountDto>
{
    public AccountAppService(IRepository<Account, Guid> repository)
        : base(repository)
    {
        GetPolicyName = ErpPermissions.Accounting.Default;
        GetListPolicyName = ErpPermissions.Accounting.Default;
        CreatePolicyName = ErpPermissions.Accounting.Edit;
        UpdatePolicyName = ErpPermissions.Accounting.Edit;
        DeletePolicyName = ErpPermissions.Accounting.Edit;
    }

    protected override async Task<Account> MapToEntityAsync(CreateUpdateAccountDto createInput)
    {
        if (createInput.SystemRole != SystemAccountRole.None)
        {
            await EnsureRoleNotAlreadyAssignedAsync(createInput.SystemRole, currentId: null);
        }

        var entity = new Account(GuidGenerator.Create(), createInput.Code, createInput.Name, createInput.Type);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateAccountDto updateInput, Account entity)
    {
        if (updateInput.SystemRole != SystemAccountRole.None)
        {
            await EnsureRoleNotAlreadyAssignedAsync(updateInput.SystemRole, currentId: entity.Id);
        }

        CopyToEntity(updateInput, entity);
    }

    // Unlike TaxRate.IsDefault/Currency.IsBaseCurrency (silently reassigned), a system role
    // conflict throws instead - JournalPostingService depends on these roles resolving to exactly
    // the account someone deliberately configured, so silently stealing a role from another
    // account would be a surprising, hard-to-notice way to break auto-posting.
    private async Task EnsureRoleNotAlreadyAssignedAsync(SystemAccountRole role, Guid? currentId)
    {
        var conflict = (await Repository.GetListAsync(x => x.SystemRole == role && x.Id != (currentId ?? Guid.Empty))).Any();
        if (conflict)
        {
            throw new UserFriendlyException($"Another account already has the \"{role}\" role. Remove it there first.");
        }
    }

    private static void CopyToEntity(CreateUpdateAccountDto input, Account entity)
    {
        entity.Code = input.Code;
        entity.Name = input.Name;
        entity.Type = input.Type;
        entity.SystemRole = input.SystemRole;
        entity.IsActive = input.IsActive;
    }
}
