using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Opportunities;
using Leitor.Erp.Services.Partners;
using Leitor.Erp.Services.Sales;
using Volo.Abp;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the Partner/Agent/Commission module: commission-amount calculation by basis, the
// exactly-one-of-Partner/Agent invariant, the two ways a Commission becomes Payable
// (immediately for OnProposalAccepted, via a Payment for OnClientPayment - see
// CommissionAutoPayableService), and MarkPaidAsync's own guard.
public class PartnerCommissionAppServiceTests : ErpTestBase
{
    private async Task EnablePartnerCommissionFeatureAsync()
    {
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.PartnerCommission, "true", "T", null);
    }

    private async Task<Guid> CreateOpportunityAsync()
    {
        var customerAppService = GetRequiredService<CustomerAppService>();
        var opportunityAppService = GetRequiredService<OpportunityAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Zikis" });
        var opportunity = await opportunityAppService.CreateAsync(new CreateUpdateOpportunityDto
        {
            CustomerId = customer.Id,
            Name = "Zikis POS Implementation"
        });

        return opportunity.Id;
    }

    [Fact]
    public async Task CreateAsync_Percentage_Basis_Computes_Amount_From_BaseAmount()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });

        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            PartnerId = partner.Id,
            Basis = CommissionBasis.Percentage,
            Rate = 15m,
            BaseAmount = 85000m,
            Trigger = CommissionTrigger.OnClientPayment
        });

        Assert.Equal(12750m, commission.Amount);
    }

    [Fact]
    public async Task CreateAsync_Fixed_Basis_Uses_Rate_Directly_As_Amount()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });

        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            AgentId = agent.Id,
            Basis = CommissionBasis.Fixed,
            Rate = 5000m,
            BaseAmount = 0m,
            Trigger = CommissionTrigger.OnProposalAccepted
        });

        Assert.Equal(5000m, commission.Amount);
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Neither_Partner_Nor_Agent_Set()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        await Assert.ThrowsAsync<UserFriendlyException>(() => commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            Basis = CommissionBasis.Percentage,
            Rate = 10m,
            BaseAmount = 1000m
        }));
    }

    [Fact]
    public async Task CreateAsync_Throws_When_Both_Partner_And_Agent_Set()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var agentAppService = GetRequiredService<AgentAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });
        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });

        await Assert.ThrowsAsync<UserFriendlyException>(() => commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            PartnerId = partner.Id,
            AgentId = agent.Id,
            Basis = CommissionBasis.Percentage,
            Rate = 10m,
            BaseAmount = 1000m
        }));
    }

    [Fact]
    public async Task CreateAsync_OnProposalAccepted_Trigger_Is_Payable_Immediately()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });

        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            AgentId = agent.Id,
            Basis = CommissionBasis.Percentage,
            Rate = 20m,
            BaseAmount = 100000m,
            Trigger = CommissionTrigger.OnProposalAccepted
        });

        Assert.Equal(CommissionStatus.Payable, commission.Status);
    }

    [Fact]
    public async Task CreateAsync_OnClientPayment_Trigger_Starts_Pending()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });

        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            PartnerId = partner.Id,
            Basis = CommissionBasis.Percentage,
            Rate = 15m,
            BaseAmount = 85000m,
            Trigger = CommissionTrigger.OnClientPayment
        });

        Assert.Equal(CommissionStatus.Pending, commission.Status);
    }

    [Fact]
    public async Task Payment_Against_SourceInvoice_Flips_OnClientPayment_Commission_To_Payable()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var invoiceLineAppService = GetRequiredService<InvoiceLineAppService>();
        var paymentAppService = GetRequiredService<PaymentAppService>();
        var partnerAppService = GetRequiredService<PartnerAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Zikis Client" });
        var invoice = await invoiceAppService.CreateAsync(new CreateUpdateInvoiceDto
        {
            CustomerId = customer.Id,
            Status = InvoiceStatus.Issued,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "KES"
        });
        await invoiceLineAppService.CreateAsync(new CreateUpdateInvoiceLineDto
        {
            InvoiceId = invoice.Id,
            Description = "Jipos package",
            UnitPrice = 85000m,
            Quantity = 1
        });

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });
        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            PartnerId = partner.Id,
            Basis = CommissionBasis.Percentage,
            Rate = 15m,
            BaseAmount = 85000m,
            Trigger = CommissionTrigger.OnClientPayment,
            SourceInvoiceId = invoice.Id
        });
        Assert.Equal(CommissionStatus.Pending, commission.Status);

        var billedInvoice = await invoiceAppService.GetAsync(invoice.Id);
        await paymentAppService.CreateAsync(new CreateUpdatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = billedInvoice.Total,
            PaymentDate = DateTime.UtcNow
        });

        var updatedCommission = await commissionAppService.GetAsync(commission.Id);
        Assert.Equal(CommissionStatus.Payable, updatedCommission.Status);
    }

    [Fact]
    public async Task Partial_Payment_Does_Not_Flip_OnClientPayment_Commission_To_Payable()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var customerAppService = GetRequiredService<CustomerAppService>();
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var invoiceLineAppService = GetRequiredService<InvoiceLineAppService>();
        var paymentAppService = GetRequiredService<PaymentAppService>();
        var partnerAppService = GetRequiredService<PartnerAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Zikis Client" });
        var invoice = await invoiceAppService.CreateAsync(new CreateUpdateInvoiceDto
        {
            CustomerId = customer.Id,
            Status = InvoiceStatus.Issued,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "KES"
        });
        await invoiceLineAppService.CreateAsync(new CreateUpdateInvoiceLineDto
        {
            InvoiceId = invoice.Id,
            Description = "Jipos package",
            UnitPrice = 85000m,
            Quantity = 1
        });

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });
        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            PartnerId = partner.Id,
            Basis = CommissionBasis.Percentage,
            Rate = 15m,
            BaseAmount = 85000m,
            Trigger = CommissionTrigger.OnClientPayment,
            SourceInvoiceId = invoice.Id
        });

        // A deposit, not the full invoice - the partner must not become payable for the whole
        // commission before the client has actually paid the whole invoice.
        await paymentAppService.CreateAsync(new CreateUpdatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = 30000m,
            PaymentDate = DateTime.UtcNow
        });

        var updatedCommission = await commissionAppService.GetAsync(commission.Id);
        Assert.Equal(CommissionStatus.Pending, updatedCommission.Status);
    }

    [Fact]
    public async Task MarkPaidAsync_Throws_When_Not_Yet_Payable()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos" });
        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            PartnerId = partner.Id,
            Basis = CommissionBasis.Percentage,
            Rate = 15m,
            BaseAmount = 85000m,
            Trigger = CommissionTrigger.OnClientPayment
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => commissionAppService.MarkPaidAsync(commission.Id));
    }

    [Fact]
    public async Task MarkPaidAsync_Sets_Paid_Status_And_PaidDate()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });
        var commission = await commissionAppService.CreateAsync(new CreateUpdateCommissionDto
        {
            OpportunityId = opportunityId,
            AgentId = agent.Id,
            Basis = CommissionBasis.RevenueShare,
            Rate = 20m,
            BaseAmount = 100000m,
            Trigger = CommissionTrigger.OnProposalAccepted
        });
        Assert.Equal(CommissionStatus.Payable, commission.Status);

        var paid = await commissionAppService.MarkPaidAsync(commission.Id);

        Assert.Equal(CommissionStatus.Paid, paid.Status);
        Assert.NotNull(paid.PaidDate);
    }

    // Regression for a gap found after shipping: LeadAppService.ConvertToCustomerAsync auto-opens
    // an Opportunity for the new Customer, but originally never carried Lead.ReferrerAgentId
    // forward onto it - so the referral relationship (TC-008: "Referral information is
    // preserved") silently vanished the moment a referred lead converted, even though it was
    // correctly recorded on the Lead itself.
    [Fact]
    public async Task ConvertToCustomerAsync_Propagates_ReferrerAgent_To_The_New_Opportunity()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var leadAppService = GetRequiredService<LeadAppService>();
        var opportunityAppService = GetRequiredService<OpportunityAppService>();

        var riffat = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat" });
        var lead = await leadAppService.CreateAsync(new CreateUpdateLeadDto
        {
            Name = "Mnyanga",
            Source = Leitor.Erp.Entities.Customers.LeadSource.Referral,
            ReferrerAgentId = riffat.Id
        });

        var (_, opportunityId) = await leadAppService.ConvertToCustomerAsync(lead.Id);

        var opportunity = await opportunityAppService.GetAsync(opportunityId);
        Assert.Equal(riffat.Id, opportunity.AgentId);
    }
}
