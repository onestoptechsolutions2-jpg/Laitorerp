using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Support;

// Uses CreateTicketMessageDto for both the create and update generic slots - same reasoning as
// CustomerNoteAppService/FieldServiceJobNoteAppService: messages are an append-only conversation
// thread, the UI never calls Update.
public class TicketMessageAppService :
    CrudAppService<TicketMessage, TicketMessageDto, Guid, GetTicketMessageListInput, CreateTicketMessageDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public TicketMessageAppService(
        IRepository<TicketMessage, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;

        GetPolicyName = ErpPermissions.Support.Default;
        GetListPolicyName = ErpPermissions.Support.Default;
        CreatePolicyName = ErpPermissions.Support.Edit;
        UpdatePolicyName = ErpPermissions.Support.Edit;
        DeletePolicyName = ErpPermissions.Support.Edit;
    }

    protected override async Task<IQueryable<TicketMessage>> CreateFilteredQueryAsync(GetTicketMessageListInput input)
    {
        // Oldest-first: a conversation thread reads top-to-bottom, unlike the newest-first
        // Notes/Activity sections elsewhere.
        input.Sorting ??= $"{nameof(TicketMessage.CreationTime)} ASC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.TicketId.HasValue, x => x.TicketId == input.TicketId!.Value);
    }

    public override async Task<PagedResultDto<TicketMessageDto>> GetListAsync(GetTicketMessageListInput input)
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

            foreach (var message in result.Items)
            {
                if (message.CreatorId.HasValue && namesById.TryGetValue(message.CreatorId.Value, out var userName))
                {
                    message.CreatorUserName = userName;
                }
            }
        }

        return result;
    }

    protected override Task<TicketMessage> MapToEntityAsync(CreateTicketMessageDto createInput)
    {
        var entity = new TicketMessage(GuidGenerator.Create(), createInput.TicketId, createInput.IsCustomerMessage, createInput.Text);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateTicketMessageDto updateInput, TicketMessage entity)
    {
        entity.IsCustomerMessage = updateInput.IsCustomerMessage;
        entity.Text = updateInput.Text;
        return Task.CompletedTask;
    }
}
