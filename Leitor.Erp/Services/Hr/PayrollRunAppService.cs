using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Settings;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;
using Volo.Abp.Settings;

namespace Leitor.Erp.Services.Hr;

// A payroll run is computed, not user-entered field-by-field - RunAsync loads active Employees,
// computes every statutory component via PayeCalculator + the seeded NSSF tiers + the flat
// SHA/Housing-Levy settings, and creates one PayrollRunLine per employee. This is deliberately not
// a CrudAppService: there's no "create with these fields" input shape, the whole point is the
// server computes it from Employee.BasicSalary + the current rate tables.
[RequiresFeature(ErpFeatures.HumanResources)]
public class PayrollRunAppService : ApplicationService
{
    // Kenya-only module - payroll is always run in KES, same domain assumption Employee.BasicSalary
    // already makes (see that field's own comment).
    private const string PayrollCurrencyCode = "KES";
    private const decimal PayrollExchangeRateToBase = 1m;

    private readonly IRepository<PayrollRun, Guid> _payrollRunRepository;
    private readonly IRepository<PayrollRunLine, Guid> _payrollRunLineRepository;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<PayeTaxBand, Guid> _payeTaxBandRepository;
    private readonly IRepository<NssfTier, Guid> _nssfTierRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IDataFilter _dataFilter;
    private readonly ISettingProvider _settingProvider;

    public PayrollRunAppService(
        IRepository<PayrollRun, Guid> payrollRunRepository,
        IRepository<PayrollRunLine, Guid> payrollRunLineRepository,
        IRepository<Employee, Guid> employeeRepository,
        IRepository<PayeTaxBand, Guid> payeTaxBandRepository,
        IRepository<NssfTier, Guid> nssfTierRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IDataFilter dataFilter,
        ISettingProvider settingProvider)
    {
        _payrollRunRepository = payrollRunRepository;
        _payrollRunLineRepository = payrollRunLineRepository;
        _employeeRepository = employeeRepository;
        _payeTaxBandRepository = payeTaxBandRepository;
        _nssfTierRepository = nssfTierRepository;
        _identityUserRepository = identityUserRepository;
        _accountRepository = accountRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _dataFilter = dataFilter;
        _settingProvider = settingProvider;
    }

    public async Task<PagedResultDto<PayrollRunDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Payroll.Default);

        var queryable = await _payrollRunRepository.GetQueryableAsync();
        var totalCount = queryable.Count();
        var runs = queryable.OrderByDescending(x => x.PeriodStart).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        var dtos = new List<PayrollRunDto>();
        foreach (var run in runs)
        {
            dtos.Add(await ToDtoWithTotalsAsync(run, includeLines: false));
        }

        return new PagedResultDto<PayrollRunDto>(totalCount, dtos);
    }

    public async Task<PayrollRunDto> GetAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Payroll.Default);

        var run = await _payrollRunRepository.GetAsync(id);
        return await ToDtoWithTotalsAsync(run, includeLines: true);
    }

    // Loads active Employees, computes each statutory component, creates one PayrollRunLine per
    // employee, saves as Draft. Never posts to the ledger itself - PostAsync is a deliberate
    // separate step (review the Draft numbers before they hit the books).
    public async Task<PayrollRunDto> RunAsync(DateTime periodStart, DateTime periodEnd)
    {
        await CheckPolicyAsync(ErpPermissions.Payroll.Run);

        if (periodEnd < periodStart)
        {
            throw new UserFriendlyException("The period end date must be on or after the period start date.");
        }

        // Rate tables can accumulate more than one EffectiveFrom "version" over time (a rate
        // change is added as new rows rather than overwriting history, so past payroll runs stay
        // reproducible) - only the most recent version as of the period end is live for this run,
        // not every row ever inserted, or bands/tiers from different rate-table generations would
        // silently stack and double-count.
        var allBands = await _payeTaxBandRepository.GetListAsync(x => x.EffectiveFrom <= periodEnd);
        if (allBands.Count == 0)
        {
            throw new UserFriendlyException("No PAYE tax bands are configured - set them up on the Payroll > Tax Bands page first.");
        }
        var currentBandsEffectiveFrom = allBands.Max(x => x.EffectiveFrom);
        var bands = allBands.Where(x => x.EffectiveFrom == currentBandsEffectiveFrom).ToList();

        var allNssfTiers = await _nssfTierRepository.GetListAsync(x => x.EffectiveFrom <= periodEnd);
        var nssfTiers = allNssfTiers.Count == 0
            ? new List<NssfTier>()
            : allNssfTiers
                .Where(x => x.EffectiveFrom == allNssfTiers.Max(t => t.EffectiveFrom))
                .OrderBy(x => x.LowerBound)
                .ToList();

        var personalRelief = await GetSettingDecimalAsync(ErpSettings.PayePersonalReliefMonthly, 2400m);
        var shaRatePercent = await GetSettingDecimalAsync(ErpSettings.ShaContributionRatePercent, 2.75m);
        var shaMinimum = await GetSettingDecimalAsync(ErpSettings.ShaContributionMinimum, 300m);
        var housingLevyEmployeeRatePercent = await GetSettingDecimalAsync(ErpSettings.HousingLevyEmployeeRatePercent, 1.5m);
        var housingLevyEmployerRatePercent = await GetSettingDecimalAsync(ErpSettings.HousingLevyEmployerRatePercent, 1.5m);

        var employees = await _employeeRepository.GetListAsync(x => x.IsActive);

        var run = new PayrollRun(GuidGenerator.Create(), periodStart, periodEnd);
        await _payrollRunRepository.InsertAsync(run, autoSave: true);

        foreach (var employee in employees)
        {
            var grossPay = employee.BasicSalary;

            decimal nssfEmployee = 0m;
            decimal nssfEmployer = 0m;
            foreach (var tier in nssfTiers)
            {
                var pensionableInTier = Math.Min(grossPay, tier.UpperBound) - tier.LowerBound;
                if (pensionableInTier <= 0)
                {
                    continue;
                }

                nssfEmployee += pensionableInTier * (tier.EmployeeRate / 100m);
                nssfEmployer += pensionableInTier * (tier.EmployerRate / 100m);
            }

            // Taxable income is gross pay less the employee's own NSSF contribution (pension
            // contributions are deductible before PAYE under Kenyan tax law).
            var taxableIncome = grossPay - nssfEmployee;
            var paye = Services.PayeCalculator.ComputeNetPaye(taxableIncome, bands, personalRelief);
            var grossPaye = Services.PayeCalculator.ComputeGrossPaye(taxableIncome, bands);

            var shaContribution = Math.Max(grossPay * (shaRatePercent / 100m), shaMinimum);
            var housingLevyEmployee = grossPay * (housingLevyEmployeeRatePercent / 100m);
            var housingLevyEmployer = grossPay * (housingLevyEmployerRatePercent / 100m);

            var netPay = grossPay - paye - nssfEmployee - shaContribution - housingLevyEmployee;

            var line = new PayrollRunLine(GuidGenerator.Create(), run.Id, employee.Id)
            {
                GrossPay = grossPay,
                TaxableIncome = taxableIncome,
                Paye = paye,
                PersonalRelief = Math.Min(personalRelief, grossPaye),
                NssfEmployee = nssfEmployee,
                NssfEmployer = nssfEmployer,
                ShaContribution = shaContribution,
                HousingLevyEmployee = housingLevyEmployee,
                HousingLevyEmployer = housingLevyEmployer,
                OtherDeductions = 0m,
                NetPay = netPay
            };
            await _payrollRunLineRepository.InsertAsync(line, autoSave: true);
        }

        return await ToDtoWithTotalsAsync(run, includeLines: true);
    }

    // Posts one balanced multi-line journal entry for the whole run: Salary Expense debited for
    // total gross, credited across Salary Payable (net pay) and Statutory Deductions Payable (PAYE
    // + both NSSF sides + SHA + both Housing Levy sides) - the employer-side NSSF/Housing Levy
    // amounts are an additional expense+liability, not deducted from the employee's pay, so gross
    // pay alone isn't the whole debit side; see the line construction below for the exact shape.
    public async Task<PayrollRunDto> PostAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Payroll.Run);

        var alreadyPosted = await JournalPostingService.IsAlreadyPostedAsync(_journalEntryRepository, JournalPostingService.SourceDocumentTypes.PayrollRun, id);
        if (alreadyPosted)
        {
            throw new UserFriendlyException("This payroll run has already been posted to the ledger.");
        }

        var run = await _payrollRunRepository.GetAsync(id);
        if (run.Status != PayrollRunStatus.Draft)
        {
            throw new UserFriendlyException("Only a Draft payroll run can be posted.");
        }

        var lines = await _payrollRunLineRepository.GetListAsync(x => x.PayrollRunId == id);
        if (lines.Count == 0)
        {
            throw new UserFriendlyException("This payroll run has no lines to post.");
        }

        var salaryExpenseAccount = await ResolveAccountAsync(SystemAccountRole.SalaryExpense);
        var salaryPayableAccount = await ResolveAccountAsync(SystemAccountRole.SalaryPayable);
        var statutoryPayableAccount = await ResolveAccountAsync(SystemAccountRole.StatutoryDeductionsPayable);

        var totalGross = lines.Sum(x => x.GrossPay);
        var totalEmployerNssf = lines.Sum(x => x.NssfEmployer);
        var totalEmployerHousingLevy = lines.Sum(x => x.HousingLevyEmployer);
        var totalNetPay = lines.Sum(x => x.NetPay);
        var totalStatutory = lines.Sum(x => x.Paye + x.NssfEmployee + x.NssfEmployer + x.ShaContribution + x.HousingLevyEmployee + x.HousingLevyEmployer);

        // Debit side: gross salary expense, plus the employer's own NSSF/Housing Levy shares
        // (these are an employer cost on top of gross pay, not deducted from it).
        var totalDebit = totalGross + totalEmployerNssf + totalEmployerHousingLevy;

        var postingLines = new List<JournalPostingService.MultiLineEntry>
        {
            new(salaryExpenseAccount.Id, totalDebit, 0m, PayrollCurrencyCode, PayrollExchangeRateToBase),
            new(salaryPayableAccount.Id, 0m, totalNetPay, PayrollCurrencyCode, PayrollExchangeRateToBase),
            new(statutoryPayableAccount.Id, 0m, totalStatutory, PayrollCurrencyCode, PayrollExchangeRateToBase)
        };

        await JournalPostingService.PostMultiLineAsync(
            _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
            run.PeriodEnd, JournalPostingService.SourceDocumentTypes.PayrollRun, run.Id,
            $"Payroll run {run.PeriodStart:yyyy-MM-dd} to {run.PeriodEnd:yyyy-MM-dd}",
            postingLines);

        run.Status = PayrollRunStatus.Posted;
        run.RunAt = Clock.Now;
        run.RunByUserId = CurrentUser.Id;
        await _payrollRunRepository.UpdateAsync(run);

        return await ToDtoWithTotalsAsync(run, includeLines: true);
    }

    private async Task<Account> ResolveAccountAsync(SystemAccountRole role)
    {
        var account = (await _accountRepository.GetListAsync(x => x.SystemRole == role)).FirstOrDefault();
        if (account == null)
        {
            throw new UserFriendlyException(
                $"No account is configured with the \"{role}\" role yet - set one on the Chart of Accounts page first.");
        }

        return account;
    }

    private async Task<decimal> GetSettingDecimalAsync(string settingName, decimal fallback)
    {
        var raw = await _settingProvider.GetOrNullAsync(settingName);
        return decimal.TryParse(raw, out var value) ? value : fallback;
    }

    private async Task<PayrollRunDto> ToDtoWithTotalsAsync(PayrollRun run, bool includeLines)
    {
        var dto = ObjectMapper.Map<PayrollRun, PayrollRunDto>(run);

        var lines = await _payrollRunLineRepository.GetListAsync(x => x.PayrollRunId == run.Id);
        dto.EmployeeCount = lines.Count;
        dto.TotalNetPay = lines.Sum(x => x.NetPay);

        if (run.RunByUserId.HasValue)
        {
            var user = await _identityUserRepository.FindAsync(run.RunByUserId.Value);
            dto.RunByUserName = user?.UserName;
        }

        if (includeLines && lines.Count > 0)
        {
            var employeeIds = lines.Select(x => x.EmployeeId).Distinct().ToList();
            var namesById = (await _employeeRepository.GetListAsync(x => employeeIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.FullName);

            dto.Lines = lines.Select(x =>
            {
                var lineDto = ObjectMapper.Map<PayrollRunLine, PayrollRunLineDto>(x);
                lineDto.EmployeeName = namesById.GetValueOrDefault(x.EmployeeId, string.Empty);
                return lineDto;
            }).OrderBy(x => x.EmployeeName).ToList();
        }

        return dto;
    }

    private async Task CheckPolicyAsync(string policyName)
    {
        if (!await AuthorizationService.IsGrantedAsync(policyName))
        {
            throw new Volo.Abp.Authorization.AbpAuthorizationException();
        }
    }
}
