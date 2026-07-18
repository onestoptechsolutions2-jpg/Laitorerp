using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Timing;

namespace Leitor.Erp.Services.Customers;

// Uses CreateLeadTouchDto for both the create and update generic slots - same reasoning as
// CustomerNoteAppService/TicketMessageAppService: touches are an append-only contact log, the UI
// never calls Update.
public class LeadTouchAppService :
    CrudAppService<LeadTouch, LeadTouchDto, Guid, GetLeadTouchListInput, CreateLeadTouchDto>
{
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IClock _clock;

    public LeadTouchAppService(
        IRepository<LeadTouch, Guid> repository,
        IRepository<Lead, Guid> leadRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IClock clock)
        : base(repository)
    {
        _leadRepository = leadRepository;
        _identityUserRepository = identityUserRepository;
        _clock = clock;

        GetPolicyName = ErpPermissions.Leads.Default;
        GetListPolicyName = ErpPermissions.Leads.Default;
        CreatePolicyName = ErpPermissions.Leads.Edit;
        UpdatePolicyName = ErpPermissions.Leads.Edit;
        DeletePolicyName = ErpPermissions.Leads.Edit;
    }

    protected override async Task<IQueryable<LeadTouch>> CreateFilteredQueryAsync(GetLeadTouchListInput input)
    {
        // Newest-first: this is an activity log, not a Ticket-style conversation thread that
        // needs top-to-bottom reading order (see TicketMessageAppService for that exception).
        input.Sorting ??= $"{nameof(LeadTouch.TouchedAt)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.LeadId.HasValue, x => x.LeadId == input.LeadId!.Value);
    }

    public override async Task<PagedResultDto<LeadTouchDto>> GetListAsync(GetLeadTouchListInput input)
    {
        var result = await base.GetListAsync(input);

        var creatorIds = result.Items
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        if (creatorIds.Count > 0)
        {
            var creators = await _identityUserRepository.GetListAsync(x => creatorIds.Contains(x.Id));
            var namesById = creators.ToDictionary(x => x.Id, x => x.UserName);

            foreach (var touch in result.Items)
            {
                if (touch.CreatorId.HasValue && namesById.TryGetValue(touch.CreatorId.Value, out var userName))
                {
                    touch.CreatorUserName = userName;
                }
            }
        }

        return result;
    }

    // The system can't stop someone messaging on personal WhatsApp, but it refuses to record or
    // encourage further outreach against a lead flagged Do Not Contact.
    protected override async Task<LeadTouch> MapToEntityAsync(CreateLeadTouchDto createInput)
    {
        var lead = await _leadRepository.GetAsync(createInput.LeadId);
        if (lead.DoNotContact)
        {
            throw new UserFriendlyException("This lead is flagged Do Not Contact - no further outreach can be logged against it.");
        }

        return new LeadTouch(
            GuidGenerator.Create(),
            createInput.LeadId,
            createInput.Channel,
            createInput.Direction,
            createInput.TouchedAt ?? _clock.Now)
        {
            Notes = createInput.Notes
        };
    }

    protected override Task MapToEntityAsync(CreateLeadTouchDto updateInput, LeadTouch entity)
    {
        entity.Channel = updateInput.Channel;
        entity.Direction = updateInput.Direction;
        entity.Notes = updateInput.Notes;
        if (updateInput.TouchedAt.HasValue)
        {
            entity.TouchedAt = updateInput.TouchedAt.Value;
        }

        return Task.CompletedTask;
    }
}
