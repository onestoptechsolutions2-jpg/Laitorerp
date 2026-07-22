using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

public class BankAccountAppService :
    CrudAppService<BankAccount, BankAccountDto, Guid, GetBankAccountListInput, CreateUpdateBankAccountDto>
{
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<BankStatementLine, Guid> _bankStatementLineRepository;

    public BankAccountAppService(
        IRepository<BankAccount, Guid> repository,
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<BankStatementLine, Guid> bankStatementLineRepository)
        : base(repository)
    {
        _accountRepository = accountRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _bankStatementLineRepository = bankStatementLineRepository;

        GetPolicyName = ErpPermissions.Banking.Default;
        GetListPolicyName = ErpPermissions.Banking.Default;
        CreatePolicyName = ErpPermissions.Banking.Create;
        UpdatePolicyName = ErpPermissions.Banking.Edit;
        DeletePolicyName = ErpPermissions.Banking.Delete;
    }

    public override async Task<BankAccountDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        await ResolveExtrasAsync(new[] { dto });
        return dto;
    }

    public override async Task<PagedResultDto<BankAccountDto>> GetListAsync(GetBankAccountListInput input)
    {
        var result = await base.GetListAsync(input);
        await ResolveExtrasAsync(result.Items);
        return result;
    }

    protected override async Task<IQueryable<BankAccount>> CreateFilteredQueryAsync(GetBankAccountListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(!string.IsNullOrWhiteSpace(input.Filter), x => x.Name.Contains(input.Filter!));
    }

    private async Task ResolveExtrasAsync(IReadOnlyCollection<BankAccountDto> bankAccounts)
    {
        var glAccountIds = bankAccounts.Select(x => x.LinkedGlAccountId).Distinct().ToList();
        var accountsById = glAccountIds.Count > 0
            ? (await _accountRepository.GetListAsync(x => glAccountIds.Contains(x.Id))).ToDictionary(x => x.Id)
            : new Dictionary<Guid, Account>();

        var glLines = glAccountIds.Count > 0
            ? (await _journalEntryLineRepository.GetListAsync(x => glAccountIds.Contains(x.AccountId))).ToLookup(x => x.AccountId)
            : Enumerable.Empty<JournalEntryLine>().ToLookup(x => x.AccountId);

        var bankAccountIds = bankAccounts.Select(x => x.Id).ToList();
        var unreconciledCounts = bankAccountIds.Count > 0
            ? (await _bankStatementLineRepository.GetListAsync(x => bankAccountIds.Contains(x.BankAccountId) && !x.IsReconciled))
                .GroupBy(x => x.BankAccountId)
                .ToDictionary(g => g.Key, g => g.Count())
            : new Dictionary<Guid, int>();

        foreach (var bankAccount in bankAccounts)
        {
            if (accountsById.TryGetValue(bankAccount.LinkedGlAccountId, out var glAccount))
            {
                bankAccount.LinkedGlAccountName = $"{glAccount.Code} - {glAccount.Name}";
            }

            bankAccount.GlBalance = glLines[bankAccount.LinkedGlAccountId].Sum(x => (x.Debit - x.Credit) * x.ExchangeRateToBase);
            bankAccount.UnreconciledStatementLineCount = unreconciledCounts.GetValueOrDefault(bankAccount.Id);
        }
    }

    protected override Task<BankAccount> MapToEntityAsync(CreateUpdateBankAccountDto createInput)
    {
        var entity = new BankAccount(GuidGenerator.Create(), createInput.Name, createInput.LinkedGlAccountId);
        CopyToEntity(createInput, entity);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdateBankAccountDto updateInput, BankAccount entity)
    {
        CopyToEntity(updateInput, entity);
        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateBankAccountDto input, BankAccount entity)
    {
        entity.Name = input.Name;
        entity.AccountNumber = input.AccountNumber;
        entity.BankName = input.BankName;
        entity.CurrencyCode = input.CurrencyCode;
        entity.LinkedGlAccountId = input.LinkedGlAccountId;
        entity.OpeningBalance = input.OpeningBalance;
        entity.OpeningBalanceDate = input.OpeningBalanceDate;
    }
}
