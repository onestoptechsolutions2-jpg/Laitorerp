using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Opportunities;

public class ProposalAppService :
    CrudAppService<Proposal, ProposalDto, Guid, GetProposalListInput, CreateUpdateProposalDto>
{
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IDataFilter _dataFilter;

    public ProposalAppService(
        IRepository<Proposal, Guid> repository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Quote, Guid> quoteRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _opportunityRepository = opportunityRepository;
        _quoteRepository = quoteRepository;
        _dataFilter = dataFilter;

        // Child reuses the parent Opportunity's permissions, same convention as
        // NeedsAssessmentAppService - Proposals are always managed from the Opportunity Detail
        // page, never their own top-level list.
        GetPolicyName = ErpPermissions.Opportunities.Default;
        GetListPolicyName = ErpPermissions.Opportunities.Default;
        CreatePolicyName = ErpPermissions.Opportunities.Edit;
        UpdatePolicyName = ErpPermissions.Opportunities.Edit;
        DeletePolicyName = ErpPermissions.Opportunities.Edit;
    }

    protected override async Task<IQueryable<Proposal>> CreateFilteredQueryAsync(GetProposalListInput input)
    {
        input.Sorting ??= $"{nameof(Proposal.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.OpportunityId.HasValue, x => x.OpportunityId == input.OpportunityId!.Value);
    }

    protected override async Task<Proposal> MapToEntityAsync(CreateUpdateProposalDto createInput)
    {
        var proposalNumber = await DocumentNumbering.NextAsync(Repository, _dataFilter, "PROP-");

        var entity = new Proposal(GuidGenerator.Create(), createInput.OpportunityId, proposalNumber, createInput.Title);
        CopyToEntity(createInput, entity);
        return entity;
    }

    protected override Task MapToEntityAsync(CreateUpdateProposalDto updateInput, Proposal entity)
    {
        // A resend after edits is a new revision of the same document - bump Version whenever the
        // content changes via update, not on every save (e.g. a pure Status flip Draft->Sent isn't
        // a new revision).
        var contentChanged =
            entity.Summary != updateInput.Summary ||
            entity.ProposedSolution != updateInput.ProposedSolution ||
            entity.Scope != updateInput.Scope ||
            entity.Timeline != updateInput.Timeline ||
            entity.Assumptions != updateInput.Assumptions ||
            entity.Exclusions != updateInput.Exclusions ||
            entity.WarrantyAndSupport != updateInput.WarrantyAndSupport ||
            entity.Terms != updateInput.Terms;

        CopyToEntity(updateInput, entity);

        if (contentChanged)
        {
            entity.Version++;
        }

        return Task.CompletedTask;
    }

    private static void CopyToEntity(CreateUpdateProposalDto input, Proposal entity)
    {
        entity.OpportunityId = input.OpportunityId;
        entity.Title = input.Title;
        entity.Status = input.Status;
        entity.Summary = input.Summary;
        entity.ProposedSolution = input.ProposedSolution;
        entity.Scope = input.Scope;
        entity.Timeline = input.Timeline;
        entity.Assumptions = input.Assumptions;
        entity.Exclusions = input.Exclusions;
        entity.WarrantyAndSupport = input.WarrantyAndSupport;
        entity.Terms = input.Terms;
    }

    // The concrete mechanism behind "proposal becomes a quotation" - same shape as
    // QuoteAppService.ConvertToOrderAsync, except there are no line items to carry forward: the
    // Quote's own lines ARE the proposal's de facto Bill of Materials (see Proposal.cs).
    public async Task<QuoteDto> ConvertToQuoteAsync(Guid proposalId)
    {
        await CheckCreatePolicyAsync();

        var proposal = await Repository.GetAsync(proposalId);
        var opportunity = await _opportunityRepository.GetAsync(proposal.OpportunityId);
        var quoteNumber = await DocumentNumbering.NextAsync(_quoteRepository, _dataFilter, "Q-");

        var quote = new Quote(GuidGenerator.Create(), opportunity.CustomerId, quoteNumber, proposal.Title)
        {
            ProposalId = proposal.Id,
            IssueDate = Clock.Now
        };
        await _quoteRepository.InsertAsync(quote, autoSave: true);

        proposal.Status = ProposalStatus.Accepted;
        await Repository.UpdateAsync(proposal, autoSave: true);

        return ObjectMapper.Map<Quote, QuoteDto>(quote);
    }
}
