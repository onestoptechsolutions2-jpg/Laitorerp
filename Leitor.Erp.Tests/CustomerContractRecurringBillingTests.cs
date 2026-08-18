using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-18 consolidation audit's "Contract -> recurring Invoice" fix:
// CustomerContractAppService's NextBillingDate defaulting logic. The actual invoice-issuing
// worker (BackgroundWorkers/ContractRecurringBillingWorker.cs) isn't unit-tested here, matching
// this codebase's existing convention - RecurringJournalWorker (the pattern this worker mirrors)
// has no test file either; only the AppService-level logic that feeds it is covered.
public class CustomerContractRecurringBillingTests : ErpTestBase
{
    private async Task<Guid> CreateCustomerAsync()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Billing Test Co" });
        return customer.Id;
    }

    [Fact]
    public async Task CreateAsync_With_BillingFrequency_And_No_Explicit_Date_Defaults_NextBillingDate_To_StartDate()
    {
        var customerId = await CreateCustomerAsync();
        var contractAppService = GetRequiredService<CustomerContractAppService>();

        var startDate = new DateTime(2026, 9, 1);
        var contract = await contractAppService.CreateAsync(new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = "CT-0001",
            Title = "Managed Services Retainer",
            StartDate = startDate,
            BillingFrequency = RecurringBillingFrequency.Monthly
        });

        Assert.Equal(startDate, contract.NextBillingDate);
    }

    [Fact]
    public async Task CreateAsync_With_Explicit_NextBillingDate_Preserves_It()
    {
        var customerId = await CreateCustomerAsync();
        var contractAppService = GetRequiredService<CustomerContractAppService>();

        var explicitDate = new DateTime(2026, 10, 15);
        var contract = await contractAppService.CreateAsync(new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = "CT-0002",
            Title = "Managed Services Retainer",
            StartDate = new DateTime(2026, 9, 1),
            BillingFrequency = RecurringBillingFrequency.Quarterly,
            NextBillingDate = explicitDate
        });

        Assert.Equal(explicitDate, contract.NextBillingDate);
    }

    [Fact]
    public async Task UpdateAsync_Turning_Billing_On_Defaults_NextBillingDate_Only_The_First_Time()
    {
        var customerId = await CreateCustomerAsync();
        var contractAppService = GetRequiredService<CustomerContractAppService>();

        var startDate = new DateTime(2026, 9, 1);
        var contract = await contractAppService.CreateAsync(new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = "CT-0003",
            Title = "Managed Services Retainer",
            StartDate = startDate
        });
        Assert.Null(contract.NextBillingDate);

        var turnedOn = await contractAppService.UpdateAsync(contract.Id, new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            StartDate = startDate,
            BillingFrequency = RecurringBillingFrequency.Monthly
        });
        Assert.Equal(startDate, turnedOn.NextBillingDate);

        // A later save with billing already on and no explicit date doesn't re-default/reset it -
        // only the first on-transition sets it.
        var afterAnotherSave = await contractAppService.UpdateAsync(contract.Id, new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = contract.ContractNumber,
            Title = "Renamed Retainer",
            StartDate = startDate,
            BillingFrequency = RecurringBillingFrequency.Monthly
        });
        Assert.Equal(startDate, afterAnotherSave.NextBillingDate);
    }

    [Fact]
    public async Task UpdateAsync_Turning_Billing_Off_Clears_NextBillingDate()
    {
        var customerId = await CreateCustomerAsync();
        var contractAppService = GetRequiredService<CustomerContractAppService>();

        var startDate = new DateTime(2026, 9, 1);
        var contract = await contractAppService.CreateAsync(new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = "CT-0004",
            Title = "Managed Services Retainer",
            StartDate = startDate,
            BillingFrequency = RecurringBillingFrequency.Monthly
        });
        Assert.NotNull(contract.NextBillingDate);

        var turnedOff = await contractAppService.UpdateAsync(contract.Id, new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            StartDate = startDate,
            BillingFrequency = RecurringBillingFrequency.None
        });

        Assert.Null(turnedOff.NextBillingDate);
    }
}
