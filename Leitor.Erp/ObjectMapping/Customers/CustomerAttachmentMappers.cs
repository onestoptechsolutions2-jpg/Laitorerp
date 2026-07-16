using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

// FileSizeBytes (computed from Content.Length) and UploadedByUserName (resolved from
// IIdentityUserRepository) are filled in manually by CustomerAttachmentAppService after this
// mapping runs - Content itself is never mapped onto the Dto (list views shouldn't load blobs).
[Mapper]
public partial class CustomerAttachmentToCustomerAttachmentDtoMapper : MapperBase<CustomerAttachment, CustomerAttachmentDto>
{
    [MapperIgnoreSource(nameof(CustomerAttachment.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerAttachment.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(CustomerAttachment.Content))]
    [MapperIgnoreTarget(nameof(CustomerAttachmentDto.FileSizeBytes))]
    [MapperIgnoreTarget(nameof(CustomerAttachmentDto.UploadedByUserName))]
    public override partial CustomerAttachmentDto Map(CustomerAttachment source);

    [MapperIgnoreSource(nameof(CustomerAttachment.ExtraProperties))]
    [MapperIgnoreSource(nameof(CustomerAttachment.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(CustomerAttachment.Content))]
    [MapperIgnoreTarget(nameof(CustomerAttachmentDto.FileSizeBytes))]
    [MapperIgnoreTarget(nameof(CustomerAttachmentDto.UploadedByUserName))]
    public override partial void Map(CustomerAttachment source, CustomerAttachmentDto destination);
}
