using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.FieldService;

// Uses CreateFieldServiceJobNoteDto for both the create and update generic slots - same reasoning
// as CustomerNoteAppService: notes are an append-only visit log, the UI never calls Update.
public class FieldServiceJobNoteAppService :
    CrudAppService<FieldServiceJobNote, FieldServiceJobNoteDto, Guid, GetFieldServiceJobNoteListInput, CreateFieldServiceJobNoteDto>
{
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public FieldServiceJobNoteAppService(
        IRepository<FieldServiceJobNote, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository)
        : base(repository)
    {
        _identityUserRepository = identityUserRepository;

        GetPolicyName = ErpPermissions.FieldService.Default;
        GetListPolicyName = ErpPermissions.FieldService.Default;
        CreatePolicyName = ErpPermissions.FieldService.Edit;
        UpdatePolicyName = ErpPermissions.FieldService.Edit;
        DeletePolicyName = ErpPermissions.FieldService.Edit;
    }

    protected override async Task<IQueryable<FieldServiceJobNote>> CreateFilteredQueryAsync(GetFieldServiceJobNoteListInput input)
    {
        input.Sorting ??= $"{nameof(FieldServiceJobNote.CreationTime)} DESC";

        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.JobId.HasValue, x => x.JobId == input.JobId!.Value);
    }

    public override async Task<PagedResultDto<FieldServiceJobNoteDto>> GetListAsync(GetFieldServiceJobNoteListInput input)
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

            foreach (var note in result.Items)
            {
                if (note.CreatorId.HasValue && namesById.TryGetValue(note.CreatorId.Value, out var userName))
                {
                    note.CreatorUserName = userName;
                }
            }
        }

        return result;
    }

    protected override Task<FieldServiceJobNote> MapToEntityAsync(CreateFieldServiceJobNoteDto createInput)
    {
        var entity = new FieldServiceJobNote(GuidGenerator.Create(), createInput.JobId, createInput.Type, createInput.Text);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateFieldServiceJobNoteDto updateInput, FieldServiceJobNote entity)
    {
        entity.Type = updateInput.Type;
        entity.Text = updateInput.Text;
        return Task.CompletedTask;
    }
}
