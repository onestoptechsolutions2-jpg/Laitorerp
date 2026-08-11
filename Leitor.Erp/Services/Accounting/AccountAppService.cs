using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

public class AccountAppService :
    CrudAppService<Account, AccountDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateAccountDto>
{
    private readonly IRepository<Budget, Guid> _budgetRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<RecurringJournalTemplateLine, Guid> _recurringJournalTemplateLineRepository;
    private readonly IRepository<FixedAsset, Guid> _fixedAssetRepository;
    private readonly IRepository<BankAccount, Guid> _bankAccountRepository;

    public AccountAppService(
        IRepository<Account, Guid> repository,
        IRepository<Budget, Guid> budgetRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<RecurringJournalTemplateLine, Guid> recurringJournalTemplateLineRepository,
        IRepository<FixedAsset, Guid> fixedAssetRepository,
        IRepository<BankAccount, Guid> bankAccountRepository)
        : base(repository)
    {
        _budgetRepository = budgetRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _recurringJournalTemplateLineRepository = recurringJournalTemplateLineRepository;
        _fixedAssetRepository = fixedAssetRepository;
        _bankAccountRepository = bankAccountRepository;

        GetPolicyName = ErpPermissions.Accounting.Default;
        GetListPolicyName = ErpPermissions.Accounting.Default;
        CreatePolicyName = ErpPermissions.Accounting.Edit;
        UpdatePolicyName = ErpPermissions.Accounting.Edit;
        DeletePolicyName = ErpPermissions.Accounting.Edit;
    }

    // Every place a GL Account can be referenced - blocked if any exist (system-wide "block
    // deletion if dependents exist" policy, see DependencyGuard). Financial integrity matters
    // more here than almost anywhere else in the app.
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _budgetRepository.GetListAsync(x => x.AccountId == id)).Count, "Budget"),
            (async () => (await _journalEntryLineRepository.GetListAsync(x => x.AccountId == id)).Count, "Journal Entry Line"),
            (async () => (await _recurringJournalTemplateLineRepository.GetListAsync(x => x.AccountId == id)).Count, "Recurring Journal Template Line"),
            (async () => (await _fixedAssetRepository.GetListAsync(x =>
                x.AssetAccountId == id || x.DepreciationExpenseAccountId == id || x.AccumulatedDepreciationAccountId == id)).Count, "Fixed Asset"),
            (async () => (await _bankAccountRepository.GetListAsync(x => x.LinkedGlAccountId == id)).Count, "Bank Account")
        );

        await Repository.DeleteAsync(id);
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
