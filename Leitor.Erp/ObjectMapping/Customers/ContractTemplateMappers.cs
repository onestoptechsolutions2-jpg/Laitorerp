using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Riok.Mapperly.Abstractions;
using Volo.Abp.Mapperly;

namespace Leitor.Erp.ObjectMapping.Customers;

[Mapper]
public partial class ContractTemplateToContractTemplateDtoMapper : MapperBase<ContractTemplate, ContractTemplateDto>
{
    [MapperIgnoreSource(nameof(ContractTemplate.ExtraProperties))]
    [MapperIgnoreSource(nameof(ContractTemplate.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ContractTemplateDto.Sections))]
    public override partial ContractTemplateDto Map(ContractTemplate source);

    [MapperIgnoreSource(nameof(ContractTemplate.ExtraProperties))]
    [MapperIgnoreSource(nameof(ContractTemplate.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ContractTemplateDto.Sections))]
    public override partial void Map(ContractTemplate source, ContractTemplateDto destination);
}

[Mapper]
public partial class ContractTemplateSectionToContractTemplateSectionDtoMapper : MapperBase<ContractTemplateSection, ContractTemplateSectionDto>
{
    [MapperIgnoreSource(nameof(ContractTemplateSection.ExtraProperties))]
    [MapperIgnoreSource(nameof(ContractTemplateSection.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(ContractTemplateSection.ContractTemplateId))]
    public override partial ContractTemplateSectionDto Map(ContractTemplateSection source);

    [MapperIgnoreSource(nameof(ContractTemplateSection.ExtraProperties))]
    [MapperIgnoreSource(nameof(ContractTemplateSection.ConcurrencyStamp))]
    [MapperIgnoreSource(nameof(ContractTemplateSection.ContractTemplateId))]
    public override partial void Map(ContractTemplateSection source, ContractTemplateSectionDto destination);
}
