using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Accounting;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Accounting;

// Not a CrudAppService - same reasoning as JournalEntryAppService: a template's lines are only
// meaningful entered together. No balance check at save time (unlike JournalEntryAppService.
// CreateAsync) since a template is just a reusable shape; the real balance check happens when
// RecurringJournalWorker actually posts a JournalEntry from it, through the normal
// JournalEntryAppService.CreateAsync path.
public class RecurringJournalTemplateAppService : ApplicationService
{
    private readonly IRepository<RecurringJournalTemplate, Guid> _repository;
    private readonly IRepository<RecurringJournalTemplateLine, Guid> _lineRepository;
    private readonly IRepository<Account, Guid> _accountRepository;

    public RecurringJournalTemplateAppService(
        IRepository<RecurringJournalTemplate, Guid> repository,
        IRepository<RecurringJournalTemplateLine, Guid> lineRepository,
        IRepository<Account, Guid> accountRepository)
    {
        _repository = repository;
        _lineRepository = lineRepository;
        _accountRepository = accountRepository;
    }

    public async Task<List<RecurringJournalTemplateDto>> GetListAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var templates = await _repository.GetListAsync();
        var templateIds = templates.Select(x => x.Id).ToList();
        var allLines = templateIds.Count > 0
            ? await _lineRepository.GetListAsync(x => templateIds.Contains(x.RecurringJournalTemplateId))
            : new List<RecurringJournalTemplateLine>();
        var linesByTemplateId = allLines.ToLookup(x => x.RecurringJournalTemplateId);

        var dtos = templates.Select(t => ToDto(t, linesByTemplateId[t.Id].ToList())).ToList();
        await ResolveAccountNamesAsync(dtos.SelectMany(x => x.Lines).ToList());
        return dtos;
    }

    public async Task<RecurringJournalTemplateDto> GetAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Default);

        var template = await _repository.GetAsync(id);
        var lines = await _lineRepository.GetListAsync(x => x.RecurringJournalTemplateId == id);
        var dto = ToDto(template, lines);
        await ResolveAccountNamesAsync(dto.Lines);
        return dto;
    }

    public async Task<RecurringJournalTemplateDto> CreateAsync(CreateUpdateRecurringJournalTemplateDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Create);

        var lines = ValidateLines(input);

        var template = new RecurringJournalTemplate(GuidGenerator.Create(), input.Description, input.NextRunDate)
        {
            Frequency = input.Frequency,
            IsActive = input.IsActive
        };
        await _repository.InsertAsync(template, autoSave: true);

        await InsertLinesAsync(template.Id, lines);

        return await GetAsync(template.Id);
    }

    public async Task<RecurringJournalTemplateDto> UpdateAsync(Guid id, CreateUpdateRecurringJournalTemplateDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Edit);

        var lines = ValidateLines(input);

        var template = await _repository.GetAsync(id);
        template.Description = input.Description;
        template.Frequency = input.Frequency;
        template.NextRunDate = input.NextRunDate;
        template.IsActive = input.IsActive;
        await _repository.UpdateAsync(template, autoSave: true);

        var existingLines = await _lineRepository.GetListAsync(x => x.RecurringJournalTemplateId == id);
        await _lineRepository.DeleteManyAsync(existingLines);
        await InsertLinesAsync(id, lines);

        return await GetAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Accounting.Delete);

        var lines = await _lineRepository.GetListAsync(x => x.RecurringJournalTemplateId == id);
        await _lineRepository.DeleteManyAsync(lines);
        await _repository.DeleteAsync(id);
    }

    private static List<CreateUpdateRecurringJournalTemplateLineDto> ValidateLines(CreateUpdateRecurringJournalTemplateDto input)
    {
        var lines = input.Lines.Where(x => x.Debit > 0 || x.Credit > 0).ToList();
        if (lines.Count < 2)
        {
            throw new UserFriendlyException("A recurring journal template needs at least two lines.");
        }

        if (lines.Any(x => x.Debit > 0 && x.Credit > 0))
        {
            throw new UserFriendlyException("A line can't have both a debit and a credit - use separate lines.");
        }

        if (lines.Any(x => string.IsNullOrWhiteSpace(x.CurrencyCode)))
        {
            throw new UserFriendlyException("Every line needs a currency.");
        }

        return lines;
    }

    private async Task InsertLinesAsync(Guid templateId, List<CreateUpdateRecurringJournalTemplateLineDto> lines)
    {
        foreach (var line in lines)
        {
            await _lineRepository.InsertAsync(
                new RecurringJournalTemplateLine(GuidGenerator.Create(), templateId, line.AccountId)
                {
                    Debit = line.Debit,
                    Credit = line.Credit,
                    CurrencyCode = line.CurrencyCode
                },
                autoSave: true);
        }
    }

    private static RecurringJournalTemplateDto ToDto(RecurringJournalTemplate template, List<RecurringJournalTemplateLine> lines)
    {
        return new RecurringJournalTemplateDto
        {
            Id = template.Id,
            Description = template.Description,
            Frequency = template.Frequency,
            NextRunDate = template.NextRunDate,
            IsActive = template.IsActive,
            Lines = lines.Select(x => new RecurringJournalTemplateLineDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Debit = x.Debit,
                Credit = x.Credit,
                CurrencyCode = x.CurrencyCode
            }).ToList()
        };
    }

    private async Task ResolveAccountNamesAsync(List<RecurringJournalTemplateLineDto> lines)
    {
        var accountIds = lines.Select(x => x.AccountId).Distinct().ToList();
        var accountsById = accountIds.Count > 0
            ? (await _accountRepository.GetListAsync(x => accountIds.Contains(x.Id))).ToDictionary(x => x.Id)
            : new Dictionary<Guid, Account>();

        foreach (var line in lines)
        {
            if (accountsById.TryGetValue(line.AccountId, out var account))
            {
                line.AccountCode = account.Code;
                line.AccountName = account.Name;
            }
        }
    }
}
