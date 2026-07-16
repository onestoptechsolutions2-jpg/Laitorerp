using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;

namespace Leitor.Erp.Services.Customers;

// Not a CrudAppService: attachments are never updated (only uploaded/deleted), and list views
// need FileSizeBytes/uploader-name resolution that don't fit the generic Dto mapping flow. See
// CustomerAttachment.cs for why file bytes live in Postgres rather than a volume/object storage.
public class CustomerAttachmentAppService : ErpAppService
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly IRepository<CustomerAttachment, Guid> _repository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public CustomerAttachmentAppService(
        IRepository<CustomerAttachment, Guid> repository,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _repository = repository;
        _identityUserRepository = identityUserRepository;
    }

    public async Task<List<CustomerAttachmentDto>> GetListAsync(Guid customerId)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Default);

        var attachments = await _repository.GetListAsync(x => x.CustomerId == customerId);
        var dtos = attachments
            .OrderByDescending(x => x.CreationTime)
            .Select(x =>
            {
                var dto = ObjectMapper.Map<CustomerAttachment, CustomerAttachmentDto>(x);
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

    public async Task<CustomerAttachmentDto> UploadAsync(CreateCustomerAttachmentDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Edit);

        if (input.Content.Length > MaxFileSizeBytes)
        {
            throw new UserFriendlyException($"File is too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB.");
        }

        var entity = new CustomerAttachment(
            GuidGenerator.Create(),
            input.CustomerId,
            input.FileName,
            input.ContentType,
            input.Content
        );

        await _repository.InsertAsync(entity, autoSave: true);

        var dto = ObjectMapper.Map<CustomerAttachment, CustomerAttachmentDto>(entity);
        dto.FileSizeBytes = entity.Content.Length;
        return dto;
    }

    public async Task<CustomerAttachmentContentDto> GetContentAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Default);

        var entity = await _repository.GetAsync(id);
        return new CustomerAttachmentContentDto
        {
            FileName = entity.FileName,
            ContentType = entity.ContentType,
            Content = entity.Content
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Edit);
        await _repository.DeleteAsync(id);
    }
}
