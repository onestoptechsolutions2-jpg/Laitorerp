using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Partners;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace Leitor.Erp.Services.Partners;

// Called from ProposalAppService.ConvertToQuoteAsync - the "proposal accepted" moment - so a
// Commission is recorded automatically from the Partner/Agent's own standing rate/basis/trigger
// instead of waiting for someone to open "New Commission" and re-type numbers that were already
// sitting on the Opportunity and its Partner/Agent record (2026-08-18 consolidation follow-up:
// CommissionAppService.MapToEntityAsync already pre-fills the create form from these same fields -
// this is that same computation, just not gated behind a human remembering to open the form).
// Same static-method-with-injected-deps shape as CommissionAutoPayableService/DeletionGate, so
// ProposalAppService (core Opportunities, always-on) doesn't need a hard DI dependency on the
// togglable Partner/Commission module beyond the repositories themselves.
public static class CommissionAutoCreateService
{
    public static async Task CreateForAcceptedProposalAsync(
        IRepository<Commission, Guid> commissionRepository,
        IRepository<Partner, Guid> partnerRepository,
        IRepository<Agent, Guid> agentRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IFeatureChecker featureChecker,
        IGuidGenerator guidGenerator,
        ICurrentUser currentUser,
        IClock clock,
        Opportunity opportunity)
    {
        if (!opportunity.PartnerId.HasValue && !opportunity.AgentId.HasValue)
        {
            return;
        }

        if (!await featureChecker.IsEnabledAsync(ErpFeatures.PartnerCommission))
        {
            return;
        }

        // An Opportunity can involve a Partner AND an Agent at once (e.g. an Agent-referred lead
        // fulfilled through a delivery Partner - see Opportunity.cs) - each gets its own Commission,
        // matching Commission's own "exactly one of PartnerId/AgentId" invariant.
        var existingParties = (await commissionRepository.GetListAsync(x => x.OpportunityId == opportunity.Id))
            .Select(x => x.PartnerId ?? x.AgentId)
            .ToHashSet();

        if (opportunity.PartnerId.HasValue && !existingParties.Contains(opportunity.PartnerId))
        {
            var partner = await partnerRepository.FindAsync(opportunity.PartnerId.Value);
            if (partner != null)
            {
                await CreateAsync(
                    commissionRepository, stageEventRepository, guidGenerator, currentUser, clock,
                    opportunity.Id, partnerId: partner.Id, agentId: null,
                    partner.CommissionBasis, partner.CommissionRate, partner.CommissionTrigger, opportunity.EstimatedValue);
            }
        }

        if (opportunity.AgentId.HasValue && !existingParties.Contains(opportunity.AgentId))
        {
            var agent = await agentRepository.FindAsync(opportunity.AgentId.Value);
            if (agent != null)
            {
                await CreateAsync(
                    commissionRepository, stageEventRepository, guidGenerator, currentUser, clock,
                    opportunity.Id, partnerId: null, agentId: agent.Id,
                    agent.CommissionBasis, agent.CommissionRate, agent.CommissionTrigger, opportunity.EstimatedValue);
            }
        }
    }

    private static async Task CreateAsync(
        IRepository<Commission, Guid> commissionRepository,
        IRepository<WorkflowStageEvent, Guid> stageEventRepository,
        IGuidGenerator guidGenerator,
        ICurrentUser currentUser,
        IClock clock,
        Guid opportunityId,
        Guid? partnerId,
        Guid? agentId,
        CommissionBasis basis,
        decimal rate,
        CommissionTrigger trigger,
        decimal? estimatedValue)
    {
        // Fixed is a flat payout (Rate read directly, see CommissionAppService.CopyToEntity) - every
        // other basis needs a real deal value to apply the percentage against. Without one yet, skip
        // auto-creation rather than record a zero-amount commission; a manual entry once the value is
        // known still works exactly as it does today.
        var baseAmount = estimatedValue ?? 0m;
        if (basis != CommissionBasis.Fixed && baseAmount <= 0m)
        {
            return;
        }

        var entity = new Commission(guidGenerator.Create(), opportunityId)
        {
            PartnerId = partnerId,
            AgentId = agentId,
            Basis = basis,
            Rate = rate,
            BaseAmount = baseAmount,
            Trigger = trigger,
            Amount = basis == CommissionBasis.Fixed ? rate : Math.Round(baseAmount * rate / 100m, 2)
        };

        // OnProposalAccepted commissions are payable the instant they're recorded - there's no later
        // trigger event to wait for (same rule CommissionAppService.MapToEntityAsync already applies
        // to a manually-entered one). OnClientPayment ones start Pending; CommissionAutoPayableService
        // resolves the Opportunity from the eventual paid Invoice and flips them (see its own comment).
        if (trigger == CommissionTrigger.OnProposalAccepted)
        {
            entity.Status = CommissionStatus.Payable;
        }

        await commissionRepository.InsertAsync(entity, autoSave: true);

        await WorkflowStageLog.RecordAsync(
            stageEventRepository, guidGenerator, currentUser, clock,
            "Commission", entity.Id, WorkflowStage.CommissionAccrued,
            notes: trigger == CommissionTrigger.OnProposalAccepted
                ? "Auto-created and payable on proposal acceptance"
                : "Auto-created on proposal acceptance, pending client payment");
    }
}
