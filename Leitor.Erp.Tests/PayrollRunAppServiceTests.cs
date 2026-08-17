using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Hr;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Hr;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Guids;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 Statutory Payroll (Phase 4c): RunAsync's per-employee computation and
// PostAsync's ledger posting/idempotency guard. Test-fixture rate tables here are NOT the real
// seeded rates (see Data/ErpPayeTaxBandDataSeeder.cs/ErpNssfTierDataSeeder.cs) - hand-picked small
// numbers so expected values can be verified by hand, same reasoning PayeCalculatorTests uses.
public class PayrollRunAppServiceTests : ErpTestBase
{
    private async Task<Guid> SeedRateTablesAndEmployeeAsync(decimal basicSalary)
    {
        await EnsureDatabaseCreatedAsync();

        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.HumanResources, "true", "T", null);

        var guidGenerator = GetRequiredService<IGuidGenerator>();

        var payeTaxBandRepository = GetRequiredService<IRepository<PayeTaxBand, Guid>>();
        var effectiveFrom = new DateTime(2026, 1, 1);
        await payeTaxBandRepository.InsertAsync(new PayeTaxBand(guidGenerator.Create(), 0, 20_000m, 10m, effectiveFrom), autoSave: true);
        await payeTaxBandRepository.InsertAsync(new PayeTaxBand(guidGenerator.Create(), 20_000m, null, 25m, effectiveFrom), autoSave: true);

        var nssfTierRepository = GetRequiredService<IRepository<NssfTier, Guid>>();
        await nssfTierRepository.InsertAsync(new NssfTier(guidGenerator.Create(), 1, 0, 6_000m, 6m, 6m, effectiveFrom), autoSave: true);
        await nssfTierRepository.InsertAsync(new NssfTier(guidGenerator.Create(), 2, 6_000m, 18_000m, 6m, 6m, effectiveFrom), autoSave: true);

        var employeeAppService = GetRequiredService<EmployeeAppService>();
        var employee = await employeeAppService.CreateAsync(new CreateUpdateEmployeeDto
        {
            FullName = "Payroll Test Employee",
            HireDate = DateTime.UtcNow.AddYears(-1),
            BasicSalary = basicSalary
        });

        return employee.Id;
    }

    [Fact]
    public async Task RunAsync_Produces_One_Line_Per_Active_Employee_Excludes_Inactive()
    {
        var activeEmployeeId = await SeedRateTablesAndEmployeeAsync(30_000m);

        var employeeAppService = GetRequiredService<EmployeeAppService>();
        var inactiveEmployee = await employeeAppService.CreateAsync(new CreateUpdateEmployeeDto
        {
            FullName = "Former Employee",
            HireDate = DateTime.UtcNow.AddYears(-2),
            TerminationDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = false,
            BasicSalary = 25_000m
        });

        var payrollRunAppService = GetRequiredService<PayrollRunAppService>();
        var run = await payrollRunAppService.RunAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

        var reloaded = await payrollRunAppService.GetAsync(run.Id);
        var line = Assert.Single(reloaded.Lines);
        Assert.Equal(activeEmployeeId, line.EmployeeId);
        Assert.DoesNotContain(reloaded.Lines, x => x.EmployeeId == inactiveEmployee.Id);
    }

    [Fact]
    public async Task RunAsync_Computes_Nssf_And_Housing_Levy_Against_Seeded_TestFixture_Rates()
    {
        // Gross 30,000. NSSF: tier1 6,000@6%=360, tier2 (18,000-6,000)=12,000@6%=720 -> 1,080 each side.
        // Housing levy at the seeded 1.5%/1.5% default settings: 450 each side.
        var employeeId = await SeedRateTablesAndEmployeeAsync(30_000m);

        var payrollRunAppService = GetRequiredService<PayrollRunAppService>();
        var run = await payrollRunAppService.RunAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

        var reloaded = await payrollRunAppService.GetAsync(run.Id);
        var line = reloaded.Lines.Single(x => x.EmployeeId == employeeId);

        Assert.Equal(1_080m, line.NssfEmployee);
        Assert.Equal(450m, line.HousingLevyEmployee);
        Assert.Equal(450m, line.HousingLevyEmployer);

        // Taxable income = 30,000 - 1,080 (employee NSSF) = 28,928... 28,920.
        // PAYE (test bands: 10% to 20,000, 25% above): 20,000*10% + 8,920*25% = 2,000 + 2,230 = 4,230
        // minus the seeded default personal relief of 2,400 -> 1,830.
        Assert.Equal(28_920m, line.TaxableIncome);
        Assert.Equal(1_830m, line.Paye);

        var expectedNetPay = line.GrossPay - line.Paye - line.NssfEmployee - line.ShaContribution - line.HousingLevyEmployee;
        Assert.Equal(expectedNetPay, line.NetPay);
    }

    [Fact]
    public async Task PostAsync_Posts_A_Balanced_Journal_Entry_And_Blocks_Double_Posting()
    {
        await SeedRateTablesAndEmployeeAsync(30_000m);

        var payrollRunAppService = GetRequiredService<PayrollRunAppService>();
        var run = await payrollRunAppService.RunAsync(new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));

        var posted = await payrollRunAppService.PostAsync(run.Id);
        Assert.Equal(PayrollRunStatus.Posted, posted.Status);

        var journalEntryRepository = GetRequiredService<IRepository<JournalEntry, Guid>>();
        var journalEntryLineRepository = GetRequiredService<IRepository<JournalEntryLine, Guid>>();

        var entry = Assert.Single(await journalEntryRepository.GetListAsync(
            x => x.SourceDocumentType == JournalPostingService.SourceDocumentTypes.PayrollRun && x.SourceDocumentId == run.Id));

        var lines = await journalEntryLineRepository.GetListAsync(x => x.JournalEntryId == entry.Id);
        var totalDebit = lines.Sum(x => x.Debit);
        var totalCredit = lines.Sum(x => x.Credit);
        Assert.Equal(totalDebit, totalCredit);
        Assert.True(totalDebit > 0);

        // Re-posting an already-posted run is blocked by JournalPostingService's own
        // IsAlreadyPostedAsync guard, same idempotency mechanism every other poster relies on.
        await Assert.ThrowsAsync<UserFriendlyException>(() => payrollRunAppService.PostAsync(run.Id));
    }
}
