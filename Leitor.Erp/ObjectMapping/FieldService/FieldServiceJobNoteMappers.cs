using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Services.Dtos.FieldService;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.FieldService;

[Mapper]
public partial class FieldServiceJobNoteToFieldServiceJobNoteDtoMapper : MapperBase<FieldServiceJobNote, FieldServiceJobNoteDto>
{
    [MapperIgnoreSource(nameof(FieldServiceJobNote.ExtraProperties))]
    [MapperIgnoreSource(nameof(FieldServiceJobNote.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FieldServiceJobNoteDto.CreatorUserName))]
    public override partial FieldServiceJobNoteDto Map(FieldServiceJobNote source);

    [MapperIgnoreSource(nameof(FieldServiceJobNote.ExtraProperties))]
    [MapperIgnoreSource(nameof(FieldServiceJobNote.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FieldServiceJobNoteDto.CreatorUserName))]
    public override partial void Map(FieldServiceJobNote source, FieldServiceJobNoteDto destination);
}
