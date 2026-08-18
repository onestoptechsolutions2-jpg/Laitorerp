using System;
using System.Linq;
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
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the "quick wins" automation added after the 2026-08-18 consolidation review: a Commission
// is now recorded automatically from the Partner/Agent's standing rate/basis/trigger the moment a
// Proposal converts to a Quote (ProposalAppService.ConvertToQuoteAsync), instead of waiting for
// someone to open "New Commission" and retype numbers that were already sitting on the Opportunity
// - see CommissionAutoCreateService. An OnClientPayment-triggered commission created this way has
// no Invoice yet, so CommissionAutoPayableService was extended to resolve it later by tracing the
// eventual paid Invoice back to its Opportunity (Invoice -> Order -> Quote -> Proposal).
public class CommissionAutoCreateAppServiceTests : ErpTestBase
{
    private async Task EnablePartnerCommissionFeatureAsync()
    {
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.PartnerCommission, "true", "T", null);
    }

    private async Task<Guid> CreateCustomerAsync(string name = "Zikis")
    {
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = name });
        return customer.Id;
    }

    private async Task<ProposalDto> CreateAcceptableProposalAsync(Guid customerId, Guid? partnerId, Guid? agentId, decimal? estimatedValue)
    {
        var opportunityAppService = GetRequiredService<OpportunityAppService>();
        var proposalAppService = GetRequiredService<ProposalAppService>();

        var opportunity = await opportunityAppService.CreateAsync(new CreateUpdateOpportunityDto
        {
            CustomerId = customerId,
            Name = "Auto-Commission Deal",
            EstimatedValue = estimatedValue,
            PartnerId = partnerId,
            AgentId = agentId
        });

        return await proposalAppService.CreateAsync(new CreateUpdateProposalDto
        {
            OpportunityId = opportunity.Id,
            Title = "Auto-Commission Proposal"
        });
    }

    [Fact]
    public async Task ConvertToQuote_Auto_Creates_Payable_Commission_For_OnProposalAccepted_Agent()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var proposalAppService = GetRequiredService<ProposalAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();

        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto
        {
            Name = "Riffat",
            CommissionBasis = CommissionBasis.Percentage,
            CommissionRate = 20m,
            CommissionTrigger = CommissionTrigger.OnProposalAccepted
        });

        var customerId = await CreateCustomerAsync();
        var proposal = await CreateAcceptableProposalAsync(customerId, partnerId: null, agentId: agent.Id, estimatedValue: 100000m);

        await proposalAppService.ConvertToQuoteAsync(proposal.Id);

        var commissions = await commissionAppService.GetListAsync(new GetCommissionListInput { OpportunityId = proposal.OpportunityId });
        var commission = Assert.Single(commissions.Items);

        Assert.Equal(agent.Id, commission.AgentId);
        Assert.Equal(20000m, commission.Amount);
        Assert.Equal(CommissionStatus.Payable, commission.Status);
    }

    [Fact]
    public async Task ConvertToQuote_Auto_Creates_Pending_Commission_For_OnClientPayment_Partner()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var proposalAppService = GetRequiredService<ProposalAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto
        {
            Name = "Jipos",
            CommissionBasis = CommissionBasis.Percentage,
            CommissionRate = 15m,
            CommissionTrigger = CommissionTrigger.OnClientPayment
        });

        var customerId = await CreateCustomerAsync();
        var proposal = await CreateAcceptableProposalAsync(customerId, partnerId: partner.Id, agentId: null, estimatedValue: 85000m);

        await proposalAppService.ConvertToQuoteAsync(proposal.Id);

        var commissions = await commissionAppService.GetListAsync(new GetCommissionListInput { OpportunityId = proposal.OpportunityId });
        var commission = Assert.Single(commissions.Items);

        Assert.Equal(partner.Id, commission.PartnerId);
        Assert.Equal(12750m, commission.Amount);
        Assert.Equal(CommissionStatus.Pending, commission.Status);
        Assert.Null(commission.SourceInvoiceId);
    }

    [Fact]
    public async Task ConvertToQuote_Creates_Two_Commissions_When_Both_Partner_And_Agent_Are_Set()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var agentAppService = GetRequiredService<AgentAppService>();
        var proposalAppService = GetRequiredService<ProposalAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto { Name = "Jipos", CommissionRate = 10m });
        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat", CommissionRate = 5m });

        var customerId = await CreateCustomerAsync();
        var proposal = await CreateAcceptableProposalAsync(customerId, partnerId: partner.Id, agentId: agent.Id, estimatedValue: 50000m);

        await proposalAppService.ConvertToQuoteAsync(proposal.Id);

        var commissions = await commissionAppService.GetListAsync(new GetCommissionListInput { OpportunityId = proposal.OpportunityId });
        Assert.Equal(2, commissions.Items.Count);
        Assert.Contains(commissions.Items, x => x.PartnerId == partner.Id);
        Assert.Contains(commissions.Items, x => x.AgentId == agent.Id);
    }

    [Fact]
    public async Task ConvertToQuote_Does_Not_Auto_Create_Commission_Without_An_EstimatedValue()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var proposalAppService = GetRequiredService<ProposalAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();

        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto
        {
            Name = "Riffat",
            CommissionBasis = CommissionBasis.Percentage,
            CommissionRate = 20m
        });

        var customerId = await CreateCustomerAsync();
        var proposal = await CreateAcceptableProposalAsync(customerId, partnerId: null, agentId: agent.Id, estimatedValue: null);

        await proposalAppService.ConvertToQuoteAsync(proposal.Id);

        var commissions = await commissionAppService.GetListAsync(new GetCommissionListInput { OpportunityId = proposal.OpportunityId });
        Assert.Empty(commissions.Items);
    }

    [Fact]
    public async Task ConvertToQuote_Does_Not_Auto_Create_Commission_When_Feature_Disabled()
    {
        await EnsureDatabaseCreatedAsync();

        var agentAppService = GetRequiredService<AgentAppService>();
        var proposalAppService = GetRequiredService<ProposalAppService>();

        // Agent directory itself is core (not feature-gated - see AgentAppService), so it can be
        // populated even with PartnerCommission left off.
        var agent = await agentAppService.CreateAsync(new CreateUpdateAgentDto { Name = "Riffat", CommissionRate = 20m });

        var customerId = await CreateCustomerAsync();
        var proposal = await CreateAcceptableProposalAsync(customerId, partnerId: null, agentId: agent.Id, estimatedValue: 100000m);

        // Should not throw even though PartnerCommission (and therefore CommissionAppService) is off.
        await proposalAppService.ConvertToQuoteAsync(proposal.Id);
    }

    [Fact]
    public async Task Full_Payment_Resolves_Auto_Created_OnClientPayment_Commission_Via_Opportunity_Chain()
    {
        await EnsureDatabaseCreatedAsync();
        await EnablePartnerCommissionFeatureAsync();

        var partnerAppService = GetRequiredService<PartnerAppService>();
        var proposalAppService = GetRequiredService<ProposalAppService>();
        var commissionAppService = GetRequiredService<CommissionAppService>();
        var quoteAppService = GetRequiredService<QuoteAppService>();
        var quoteLineAppService = GetRequiredService<QuoteLineAppService>();
        var orderAppService = GetRequiredService<OrderAppService>();
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var invoiceLineAppService = GetRequiredService<InvoiceLineAppService>();
        var paymentAppService = GetRequiredService<PaymentAppService>();

        var partner = await partnerAppService.CreateAsync(new CreateUpdatePartnerDto
        {
            Name = "Jipos",
            CommissionBasis = CommissionBasis.Percentage,
            CommissionRate = 15m,
            CommissionTrigger = CommissionTrigger.OnClientPayment
        });

        var customerId = await CreateCustomerAsync("Zikis Client");
        var proposal = await CreateAcceptableProposalAsync(customerId, partnerId: partner.Id, agentId: null, estimatedValue: 85000m);

        var quote = await proposalAppService.ConvertToQuoteAsync(proposal.Id);

        var commissionsBeforePayment = await commissionAppService.GetListAsync(new GetCommissionListInput { OpportunityId = proposal.OpportunityId });
        var autoCommission = Assert.Single(commissionsBeforePayment.Items);
        Assert.Equal(CommissionStatus.Pending, autoCommission.Status);
        Assert.Null(autoCommission.SourceInvoiceId);

        // Build the real Quote -> Order -> Invoice chain the way a salesperson actually would.
        await quoteLineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            Description = "Jipos package",
            UnitPrice = 85000m,
            Quantity = 1
        });

        var order = await quoteAppService.ConvertToOrderAsync(quote.Id);
        var invoice = await invoiceAppService.CreateAsync(new CreateUpdateInvoiceDto
        {
            CustomerId = customerId,
            OrderId = order.Id,
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

        var billedInvoice = await invoiceAppService.GetAsync(invoice.Id);
        await paymentAppService.CreateAsync(new CreateUpdatePaymentDto
        {
            InvoiceId = invoice.Id,
            Amount = billedInvoice.Total,
            PaymentDate = DateTime.UtcNow
        });

        var updatedCommission = await commissionAppService.GetAsync(autoCommission.Id);
        Assert.Equal(CommissionStatus.Payable, updatedCommission.Status);
        Assert.Equal(invoice.Id, updatedCommission.SourceInvoiceId);
    }
}
