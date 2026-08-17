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

    // Regression test for the 2026-08-17 "Save throws an error on an untouched proposal" bug:
    // Create.cshtml used to expose Status as a plain editable dropdown, so a submitted non-Draft
    // value used to be persisted as-is - a proposal born non-Draft has no UnlockedByUserId, so the
    // very next Save (even with zero edits) hit the lock check and threw. CreateAsync now always
    // forces Draft regardless of what's submitted.
    [Fact]
    public async Task CreateAsync_Always_Creates_A_Draft_Proposal_Regardless_Of_Submitted_Status()
    {
        await EnsureDatabaseCreatedAsync();

        var proposalAppService = GetRequiredService<ProposalAppService>();
        var opportunityId = await CreateOpportunityAsync();

        var proposal = await proposalAppService.CreateAsync(new CreateUpdateProposalDto
        {
            OpportunityId = opportunityId,
            Title = "Odoo Implementation",
            Status = ProposalStatus.Sent
        });

        Assert.Equal(ProposalStatus.Draft, proposal.Status);

        // The bug's exact symptom: opening it and saving with no edits must not throw.
        var reloaded = await proposalAppService.GetAsync(proposal.Id);
        await proposalAppService.UpdateAsync(proposal.Id, new CreateUpdateProposalDto
        {
            OpportunityId = reloaded.OpportunityId,
            Title = reloaded.Title,
            Status = reloaded.Status,
            Summary = reloaded.Summary,
            ProposedSolution = reloaded.ProposedSolution,
            Scope = reloaded.Scope,
            Timeline = reloaded.Timeline,
            Assumptions = reloaded.Assumptions,
            Exclusions = reloaded.Exclusions,
            WarrantyAndSupport = reloaded.WarrantyAndSupport,
            Terms = reloaded.Terms
        });
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
