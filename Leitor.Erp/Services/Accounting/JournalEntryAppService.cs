using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Not a CrudAppService: a journal entry is only meaningful as a single atomic, balanced
// transaction - same reasoning GoodsReceiptAppService uses for covering multiple PurchaseOrderLines
// in one call. CreateAsync is the one place that validates and persists a manual entry's lines
// together; JournalPostingService (auto-posting from Invoices/Payments/etc.) instead inserts
// JournalEntry/JournalEntryLine directly through repositories, the same way OrderAppService builds
// Invoice/InvoiceLine directly rather than calling InvoiceAppService.
public class JournalEntryAppService : ApplicationService
{
    private readonly IRepository<JournalEntry, Guid> _repository;
    private readonly IRepository<JournalEntryLine, Guid> _lineRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IRepository<BankStatementLine, Guid> _bankStatementLineRepository;
    private readonly IDataFilter _dataFilter;

    public JournalEntryAppService(
        IRepository<JournalEntry, Guid> repository,
        IRepository<JournalEntryLine, Guid> lineRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<Project, Guid> projectRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IRepository<BankStatementLine, Guid> bankStatementLineRepository,
        IDataFilter dataFilter)
    {
        _repository = repository;
        _lineRepository = lineRepository;
        _accountRepository = accountRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _projectRepository = projectRepository;
        _deletionRequestRepository = deletionRequestRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _bankStatementLineRepository = bankStatementLineRepository;
        _dataFilter = dataFilter;
    }

    // Filtered/paged without loading every JournalEntry LINE into memory - the original bug (see
    // audit note below) loaded the full JournalEntryLine table on every page view, which is the
    // truly unbounded part (N lines per entry). Header rows (JournalEntry itself, no lines) are
    // still loaded in full here and paged in C# rather than at the SQL level - a true DB-level
    // Skip/Take via GetQueryableAsync() was attempted first, but its lazily-evaluated IQueryable
    // threw ObjectDisposedException against the DbContext once actually enumerated (confirmed via
    // a failing test in this environment's SQLite test harness - AddAlwaysDisableUnitOfWorkTransaction
    // in ErpTestBase appears to not keep the DbContext alive across the gap between
    // GetQueryableAsync() and later executing it). Sticking to the repository's eager GetListAsync,
    // the same pattern proven everywhere else in this test suite, trades true SQL-level entry
    // paging for something that's actually correct and verified - still a large improvement over
    // the original bug, which loaded every entry's full line set unconditionally on every view.
    // This is the one table nearly every transaction (Invoices, Payments, POS sales, recurring
    // journals, FX revaluation, bank rec) auto-posts to via JournalPostingService, so it grows
    // faster than any other table in the schema - flagged in a 2026-08-17 performance audit.
    public async Task<PagedResultDto<JournalEntryDto>> GetListAsync(GetJournalEntryListInput input)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        List<JournalEntry> matchingEntries;
        if (input.AccountId.HasValue)
        {
            var matchingEntryIds = (await _lineRepository.GetListAsync(x => x.AccountId == input.AccountId!.Value))
                .Select(x => x.JournalEntryId)
                .Distinct()
                .ToList();
            matchingEntries = matchingEntryIds.Count > 0
                ? await _repository.GetListAsync(x => matchingEntryIds.Contains(x.Id))
                : new List<JournalEntry>();
        }
        else
        {
            matchingEntries = await _repository.GetListAsync();
        }

        var totalCount = matchingEntries.Count;
        var entries = matchingEntries
            .OrderByDescending(x => x.EntryDate)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var entryIds = entries.Select(x => x.Id).ToList();
        var allLines = entryIds.Count > 0
            ? await _lineRepository.GetListAsync(x => entryIds.Contains(x.JournalEntryId))
            : new List<JournalEntryLine>();
        var linesByEntryId = allLines.ToLookup(x => x.JournalEntryId);

        var dtos = entries.Select(entry => ToDto(entry, linesByEntryId[entry.Id].ToList())).ToList();
        return new PagedResultDto<JournalEntryDto>(totalCount, dtos);
    }

    public async Task<JournalEntryDto> GetAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var entry = await _repository.GetAsync(id);
        var lines = await _lineRepository.GetListAsync(x => x.JournalEntryId == id);
        var dto = ToDto(entry, lines);
        await ResolveAccountNamesAsync(dto.Lines);
        await ResolveProjectNumbersAsync(dto.Lines);

        if (entry.ReversedEntryId.HasValue)
        {
            var reversedEntry = await _repository.FindAsync(entry.ReversedEntryId.Value);
            dto.ReversedEntryNumber = reversedEntry?.EntryNumber;
        }

        dto.IsReversed = (await _repository.GetListAsync(x => x.ReversedEntryId == id)).Any();

        return dto;
    }

    public async Task<JournalEntryDto> CreateAsync(CreateJournalEntryDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Edit);

        var lines = input.Lines.Where(x => x.Debit > 0 || x.Credit > 0).ToList();
        if (lines.Count < 2)
        {
            throw new UserFriendlyException("A journal entry needs at least two lines.");
        }

        if (lines.Any(x => x.Debit > 0 && x.Credit > 0))
        {
            throw new UserFriendlyException("A line can't have both a debit and a credit - use separate lines.");
        }

        var resolvedRates = new Dictionary<CreateJournalEntryLineDto, decimal>();
        decimal totalDebitBase = 0;
        decimal totalCreditBase = 0;

        foreach (var line in lines)
        {
            var rate = await CurrencyRateResolver.ResolveAsync(_currencyRepository, _exchangeRateRepository, line.CurrencyCode, input.EntryDate);
            resolvedRates[line] = rate;
            totalDebitBase += line.Debit * rate;
            totalCreditBase += line.Credit * rate;
        }

        if (Math.Round(totalDebitBase - totalCreditBase, 2) != 0)
        {
            throw new UserFriendlyException(
                $"This journal entry does not balance: debits total {totalDebitBase:N2} but credits total {totalCreditBase:N2} (in base currency).");
        }

        await FiscalPeriodGuard.EnsureNotLockedAsync(_fiscalPeriodRepository, input.EntryDate);

        var entryNumber = await DocumentNumbering.NextAsync(_repository, _dataFilter, "JE-");

        var entry = new JournalEntry(GuidGenerator.Create(), entryNumber, input.EntryDate, input.Description)
        {
            IsSystemGenerated = false
        };
        await _repository.InsertAsync(entry, autoSave: true);

        var lineDtos = new List<JournalEntryLineDto>();
        foreach (var line in lines)
        {
            var entryLine = new JournalEntryLine(GuidGenerator.Create(), entry.Id, line.AccountId)
            {
                Debit = line.Debit,
                Credit = line.Credit,
                CurrencyCode = line.CurrencyCode,
                ExchangeRateToBase = resolvedRates[line],
                ProjectId = line.ProjectId
            };
            await _lineRepository.InsertAsync(entryLine, autoSave: true);

            lineDtos.Add(new JournalEntryLineDto
            {
                Id = entryLine.Id,
                JournalEntryId = entry.Id,
                AccountId = entryLine.AccountId,
                Debit = entryLine.Debit,
                Credit = entryLine.Credit,
                CurrencyCode = entryLine.CurrencyCode,
                ExchangeRateToBase = entryLine.ExchangeRateToBase,
                ProjectId = entryLine.ProjectId
            });
        }

        await ResolveAccountNamesAsync(lineDtos);
        await ResolveProjectNumbersAsync(lineDtos);

        return new JournalEntryDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryDate = entry.EntryDate,
            Description = entry.Description,
            IsSystemGenerated = entry.IsSystemGenerated,
            Lines = lineDtos,
            TotalDebit = totalDebitBase,
            TotalCredit = totalCreditBase
        };
    }

    // The only way to undo a system-generated entry (which can't be deleted directly) - inserts
    // an equal-and-opposite entry referencing the original rather than mutating or deleting it,
    // preserving the audit trail. Works for manual entries too (fixing a mis-keyed one without
    // losing the record of the mistake). Guarded against reversing the same entry twice.
    public async Task<JournalEntryDto> ReverseAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Edit);

        var alreadyReversed = (await _repository.GetListAsync(x => x.ReversedEntryId == id)).Any();
        if (alreadyReversed)
        {
            throw new UserFriendlyException("This entry has already been reversed.");
        }

        var original = await _repository.GetAsync(id);
        var originalLines = await _lineRepository.GetListAsync(x => x.JournalEntryId == id);

        await FiscalPeriodGuard.EnsureNotLockedAsync(_fiscalPeriodRepository, Clock.Now);

        var entryNumber = await DocumentNumbering.NextAsync(_repository, _dataFilter, "JE-");
        var reversal = new JournalEntry(GuidGenerator.Create(), entryNumber, Clock.Now, $"Reversal of {original.EntryNumber}")
        {
            SourceDocumentType = original.SourceDocumentType,
            SourceDocumentId = original.SourceDocumentId,
            IsSystemGenerated = original.IsSystemGenerated,
            ReversedEntryId = original.Id
        };
        await _repository.InsertAsync(reversal, autoSave: true);

        var lineDtos = new List<JournalEntryLineDto>();
        foreach (var line in originalLines)
        {
            var reversedLine = new JournalEntryLine(GuidGenerator.Create(), reversal.Id, line.AccountId)
            {
                Debit = line.Credit,
                Credit = line.Debit,
                CurrencyCode = line.CurrencyCode,
                ExchangeRateToBase = line.ExchangeRateToBase,
                ProjectId = line.ProjectId
            };
            await _lineRepository.InsertAsync(reversedLine, autoSave: true);

            lineDtos.Add(new JournalEntryLineDto
            {
                Id = reversedLine.Id,
                JournalEntryId = reversal.Id,
                AccountId = reversedLine.AccountId,
                Debit = reversedLine.Debit,
                Credit = reversedLine.Credit,
                CurrencyCode = reversedLine.CurrencyCode,
                ExchangeRateToBase = reversedLine.ExchangeRateToBase,
                ProjectId = reversedLine.ProjectId
            });
        }

        await ResolveAccountNamesAsync(lineDtos);
        await ResolveProjectNumbersAsync(lineDtos);

        return new JournalEntryDto
        {
            Id = reversal.Id,
            EntryNumber = reversal.EntryNumber,
            EntryDate = reversal.EntryDate,
            Description = reversal.Description,
            IsSystemGenerated = reversal.IsSystemGenerated,
            ReversedEntryId = reversal.ReversedEntryId,
            ReversedEntryNumber = original.EntryNumber,
            Lines = lineDtos,
            TotalDebit = lineDtos.Sum(x => x.Debit * x.ExchangeRateToBase),
            TotalCredit = lineDtos.Sum(x => x.Credit * x.ExchangeRateToBase)
        };
    }

    // System-generated entries trace back to a live document (Invoice/Payment/etc.) and are never
    // directly deletable - use ReverseAsync instead. Manual entries route through DeletionGate
    // like every other top-level deletable entity.
    public async Task DeleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Edit);

        var entry = await _repository.GetAsync(id);
        if (entry.IsSystemGenerated)
        {
            throw new UserFriendlyException("This entry was generated automatically and can't be deleted directly - post a reversing entry instead.");
        }

        await DeletionGate.EnsureImmediateDeleteAllowedAsync(AuthorizationService, CurrentUser, _deletionRequestRepository, GuidGenerator, Clock, "JournalEntry", id);

        var lines = await _lineRepository.GetListAsync(x => x.JournalEntryId == id);
        var lineIds = lines.Select(x => x.Id).ToList();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _bankStatementLineRepository.GetListAsync(x => x.MatchedJournalEntryLineId.HasValue && lineIds.Contains(x.MatchedJournalEntryLineId.Value))).Count, "reconciled Bank Statement Line")
        );

        await _lineRepository.DeleteManyAsync(lines);

        await _repository.DeleteAsync(id);
    }

    private static JournalEntryDto ToDto(JournalEntry entry, List<JournalEntryLine> lines)
    {
        return new JournalEntryDto
        {
            Id = entry.Id,
            EntryNumber = entry.EntryNumber,
            EntryDate = entry.EntryDate,
            Description = entry.Description,
            SourceDocumentType = entry.SourceDocumentType,
            SourceDocumentId = entry.SourceDocumentId,
            IsSystemGenerated = entry.IsSystemGenerated,
            ReversedEntryId = entry.ReversedEntryId,
            CreationTime = entry.CreationTime,
            CreatorId = entry.CreatorId,
            Lines = lines.Select(line => new JournalEntryLineDto
            {
                Id = line.Id,
                JournalEntryId = line.JournalEntryId,
                AccountId = line.AccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                CurrencyCode = line.CurrencyCode,
                ExchangeRateToBase = line.ExchangeRateToBase,
                ProjectId = line.ProjectId
            }).ToList(),
            TotalDebit = lines.Sum(x => x.Debit * x.ExchangeRateToBase),
            TotalCredit = lines.Sum(x => x.Credit * x.ExchangeRateToBase)
        };
    }

    private async Task ResolveAccountNamesAsync(List<JournalEntryLineDto> lines)
    {
        var accountIds = lines.Select(x => x.AccountId).Distinct().ToList();
        var accounts = accountIds.Count > 0
            ? await _accountRepository.GetListAsync(x => accountIds.Contains(x.Id))
            : new List<Account>();
        var accountsById = accounts.ToDictionary(x => x.Id);

        foreach (var line in lines)
        {
            if (accountsById.TryGetValue(line.AccountId, out var account))
            {
                line.AccountCode = account.Code;
                line.AccountName = account.Name;
            }
        }
    }

    private async Task ResolveProjectNumbersAsync(List<JournalEntryLineDto> lines)
    {
        var projectIds = lines.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId!.Value).Distinct().ToList();
        var projectNumbersById = projectIds.Count > 0
            ? (await _projectRepository.GetListAsync(x => projectIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.ProjectNumber)
            : new Dictionary<Guid, string>();

        foreach (var line in lines.Where(x => x.ProjectId.HasValue))
        {
            if (projectNumbersById.TryGetValue(line.ProjectId!.Value, out var projectNumber))
            {
                line.ProjectNumber = projectNumber;
            }
        }
    }
}
