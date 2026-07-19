using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

namespace Leitor.Erp.Services.Opportunities;

// Not a CrudAppService: attachments are never updated (only uploaded/deleted) - same shape as
// CustomerAttachmentAppService, see NeedsAssessmentAttachment.cs for why file bytes live in
// Postgres rather than a volume/object storage.
public class NeedsAssessmentAttachmentAppService : ErpAppService
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IRepository<NeedsAssessmentAttachment, Guid> _repository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public NeedsAssessmentAttachmentAppService(
        IRepository<NeedsAssessmentAttachment, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _repository = repository;
        _identityUserRepository = identityUserRepository;
    }

    public async Task<List<NeedsAssessmentAttachmentDto>> GetListAsync(Guid needsAssessmentId)
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Default);

        var attachments = await _repository.GetListAsync(x => x.NeedsAssessmentId == needsAssessmentId);
        var dtos = attachments
            .OrderByDescending(x => x.CreationTime)
            .Select(x =>
            {
                var dto = ObjectMapper.Map<NeedsAssessmentAttachment, NeedsAssessmentAttachmentDto>(x);
                dto.FileSizeBytes = x.Content.Length;
                return dto;
            })
            .ToList();

        var uploaderIds = dtos
            .Where(x => x.CreatorId.HasValue)
            .Select(x => x.CreatorId!.Value)
            .Distinct()
            .ToList();

        if (uploaderIds.Count > 0)
        {
            var uploaders = await _identityUserRepository.GetListAsync(x => uploaderIds.Contains(x.Id));
            var namesById = uploaders.ToDictionary(x => x.Id, x => x.UserName);

            foreach (var dto in dtos)
            {
                if (dto.CreatorId.HasValue && namesById.TryGetValue(dto.CreatorId.Value, out var userName))
                {
                    dto.UploadedByUserName = userName;
                }
            }
        }

        return dtos;
    }

    public async Task<NeedsAssessmentAttachmentDto> UploadAsync(CreateNeedsAssessmentAttachmentDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Edit);

        if (input.Content.Length > MaxFileSizeBytes)
        {
            throw new UserFriendlyException($"File is too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var entity = new NeedsAssessmentAttachment(
            GuidGenerator.Create(),
            input.NeedsAssessmentId,
            input.FileName,
            input.ContentType,
            input.Content
        );

        await _repository.InsertAsync(entity, autoSave: true);

        var dto = ObjectMapper.Map<NeedsAssessmentAttachment, NeedsAssessmentAttachmentDto>(entity);
        dto.FileSizeBytes = entity.Content.Length;
        return dto;
    }

    public async Task<NeedsAssessmentAttachmentContentDto> GetContentAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Default);

        var entity = await _repository.GetAsync(id);
        return new NeedsAssessmentAttachmentContentDto
        {
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            Content = entity.Content
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Opportunities.Edit);
        await _repository.DeleteAsync(id);
    }
}
