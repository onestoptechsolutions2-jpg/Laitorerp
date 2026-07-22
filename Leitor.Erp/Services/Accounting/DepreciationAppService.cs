using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Manual trigger, not a background worker - auto-posting GL entries with no explicit user action
// is riskier here than the existing workers (which only send emails or sync exchange rates), so
// a staff member runs this deliberately from the FixedAsset Detail page, one asset at a time.
public class DepreciationAppService : ApplicationService
{
    private readonly IRepository<FixedAsset, Guid> _fixedAssetRepository;
    private readonly IRepository<DepreciationEntry, Guid> _depreciationEntryRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IDataFilter _dataFilter;

    public DepreciationAppService(
        IRepository<FixedAsset, Guid> fixedAssetRepository,
        IRepository<DepreciationEntry, Guid> depreciationEntryRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IRepository<Currency, Guid> currencyRepository,
        IDataFilter dataFilter)
    {
        _fixedAssetRepository = fixedAssetRepository;
        _depreciationEntryRepository = depreciationEntryRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _currencyRepository = currencyRepository;
        _dataFilter = dataFilter;
    }

    public async Task<DepreciationEntryDto?> RunDepreciationAsync(Guid fixedAssetId, DateTime periodMonth)
    {
        await CheckPolicyAsync(ErpPermissions.FixedAssets.Edit);

        var asset = await _fixedAssetRepository.GetAsync(fixedAssetId);
        if (asset.Status != FixedAssetStatus.InUse)
        {
            throw new UserFriendlyException("Only an in-use asset can be depreciated.");
        }

        var periodDate = new DateTime(periodMonth.Year, periodMonth.Month, 1);

        var existingEntries = await _depreciationEntryRepository.GetListAsync(x => x.FixedAssetId == fixedAssetId);
        if (existingEntries.Any(x => x.PeriodDate == periodDate))
        {
            throw new UserFriendlyException($"{asset.AssetNumber} has already been depreciated for {periodDate:MMMM yyyy}.");
        }

        var depreciableBase = asset.PurchaseCost - asset.SalvageValue;
        var alreadyDepreciated = existingEntries.Sum(x => x.Amount);
        var remaining = depreciableBase - alreadyDepreciated;
        if (remaining <= 0)
        {
            throw new UserFriendlyException($"{asset.AssetNumber} is already fully depreciated.");
        }

        var monthlyAmount = Math.Min(remaining, depreciableBase / asset.UsefulLifeMonths);
        var baseCurrencyCode = (await _currencyRepository.GetListAsync(x => x.IsBaseCurrency)).FirstOrDefault()?.Code
            ?? throw new UserFriendlyException("No base currency is configured yet - set one on the Currencies page first.");

        await JournalPostingService.PostByAccountIdAsync(
            _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
            periodDate, "FixedAsset", asset.Id,
            $"Depreciation - {asset.AssetNumber} ({periodDate:MMMM yyyy})",
            asset.DepreciationExpenseAccountId, asset.AccumulatedDepreciationAccountId,
            monthlyAmount, baseCurrencyCode, 1m);

        var journalEntry = (await _journalEntryRepository.GetListAsync(
            x => x.SourceDocumentType == "FixedAsset" && x.SourceDocumentId == asset.Id && x.EntryDate == periodDate))
            .OrderByDescending(x => x.CreationTime)
            .First();

        var entry = new DepreciationEntry(GuidGenerator.Create(), asset.Id, periodDate, monthlyAmount, journalEntry.Id);
        await _depreciationEntryRepository.InsertAsync(entry, autoSave: true);

        return ObjectMapper.Map<DepreciationEntry, DepreciationEntryDto>(entry);
    }
}
