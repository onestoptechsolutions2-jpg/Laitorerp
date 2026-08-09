using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Volo.Abp;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers TC-016 (the Zikis Odoo -> Jipos scenario): a live proposal blocks a second one on the
// same Opportunity, superseding it preserves the original record and its reason, and frees the
// Opportunity for a replacement proposal that links back to what it replaces.
public class ProposalSupersedeAppServiceTests : ErpTestBase
{
    private async Task<Guid> CreateOpportunityAsync()
    {
        var customerAppService = GetRequiredService<CustomerAppService>();
        var opportunityAppService = GetRequiredService<OpportunityAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Zikis" });
        var opportunity = await opportunityAppService.CreateAsync(new CreateUpdateOpportunityDto
        {
            CustomerId = customer.Id,
            Name = "Zikis Software Implementation"
        });

        return opportunity.Id;
    }

    [Fact]
    public async Task Second_Live_Proposal_Is_Blocked_Until_First_Is_Superseded()
    {
        await EnsureDatabaseCreatedAsync();

        var proposalAppService = GetRequiredService<ProposalAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var odoo = await proposalAppService.CreateAsync(new CreateUpdateProposalDto
        {
            OpportunityId = opportunityId,
            Title = "Odoo Implementation",
            ProposedSolution = "Odoo"
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => proposalAppService.CreateAsync(new CreateUpdateProposalDto
        {
            OpportunityId = opportunityId,
            Title = "Jipos Implementation",
            ProposedSolution = "Jipos"
        }));

        await proposalAppService.SupersedeAsync(odoo.Id, "Dependency made Odoo unsuitable");

        var jipos = await proposalAppService.CreateAsync(new CreateUpdateProposalDto
        {
            OpportunityId = opportunityId,
            Title = "Jipos Implementation",
            ProposedSolution = "Jipos",
            SupersedesProposalId = odoo.Id
        });

        var supersededOdoo = await proposalAppService.GetAsync(odoo.Id);
        Assert.Equal(ProposalStatus.Superseded, supersededOdoo.Status);
        Assert.Equal("Dependency made Odoo unsuitable", supersededOdoo.SupersededReason);
        Assert.Equal(odoo.Id, jipos.SupersedesProposalId);
    }

    [Fact]
    public async Task SupersedeAsync_Requires_A_Reason()
    {
        await EnsureDatabaseCreatedAsync();

        var proposalAppService = GetRequiredService<ProposalAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var proposal = await proposalAppService.CreateAsync(new CreateUpdateProposalDto
        {
            OpportunityId = opportunityId,
            Title = "Odoo Implementation"
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() => proposalAppService.SupersedeAsync(proposal.Id, ""));
    }

    [Fact]
    public async Task SupersedeAsync_Throws_When_Already_Terminal()
    {
        await EnsureDatabaseCreatedAsync();

        var proposalAppService = GetRequiredService<ProposalAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var proposal = await proposalAppService.CreateAsync(new CreateUpdateProposalDto
        {
            OpportunityId = opportunityId,
            Title = "Odoo Implementation"
        });
        await proposalAppService.SupersedeAsync(proposal.Id, "First reason");

        await Assert.ThrowsAsync<UserFriendlyException>(() => proposalAppService.SupersedeAsync(proposal.Id, "Second reason"));
    }
}
