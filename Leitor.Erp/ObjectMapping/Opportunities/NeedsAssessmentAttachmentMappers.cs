using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Services.Dtos.Opportunities;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Opportunities;

// FileSizeBytes (computed from Content.Length) and UploadedByUserName (resolved from
// IIdentityUserRepository) are filled in manually by NeedsAssessmentAttachmentAppService after
// this mapping runs - Content itself is never mapped onto the Dto (list views shouldn't load blobs).
[Mapper]
public partial class NeedsAssessmentAttachmentToNeedsAssessmentAttachmentDtoMapper : MapperBase<NeedsAssessmentAttachment, NeedsAssessmentAttachmentDto>
{
    [MapperIgnoreSource(nameof(NeedsAssessmentAttachment.ExtraProperties))]
    [MapperIgnoreSource(nameof(NeedsAssessmentAttachment.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(NeedsAssessmentAttachment.Content))]
    [MapperIgnoreTarget(nameof(NeedsAssessmentAttachmentDto.FileSizeBytes))]
    [MapperIgnoreTarget(nameof(NeedsAssessmentAttachmentDto.UploadedByUserName))]
    public override partial NeedsAssessmentAttachmentDto Map(NeedsAssessmentAttachment source);

    [MapperIgnoreSource(nameof(NeedsAssessmentAttachment.ExtraProperties))]
    [MapperIgnoreSource(nameof(NeedsAssessmentAttachment.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(NeedsAssessmentAttachment.Content))]
    [MapperIgnoreTarget(nameof(NeedsAssessmentAttachmentDto.FileSizeBytes))]
    [MapperIgnoreTarget(nameof(NeedsAssessmentAttachmentDto.UploadedByUserName))]
    public override partial void Map(NeedsAssessmentAttachment source, NeedsAssessmentAttachmentDto destination);
}
