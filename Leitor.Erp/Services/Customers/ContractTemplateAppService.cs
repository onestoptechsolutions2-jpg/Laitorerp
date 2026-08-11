using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Governance;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Customers;

// Not a CrudAppService - a template's sections are only meaningful entered/edited together, same
// reasoning and same delete-all-and-reinsert-on-update shape as RecurringJournalTemplateAppService.
// Policy names reuse ErpPermissions.Customers, not a dedicated group - same collapse
// CustomerContractAppService itself already does (Create/Update/Delete all gated on Customers.Edit).
public class ContractTemplateAppService : ApplicationService
{
    private readonly IRepository<ContractTemplate, Guid> _repository;
    private readonly IRepository<ContractTemplateSection, Guid> _sectionRepository;
    private readonly IRepository<CustomerContract, Guid> _customerContractRepository;

    public ContractTemplateAppService(
        IRepository<ContractTemplate, Guid> repository,
        IRepository<ContractTemplateSection, Guid> sectionRepository,
        IRepository<CustomerContract, Guid> customerContractRepository)
    {
        _repository = repository;
        _sectionRepository = sectionRepository;
        _customerContractRepository = customerContractRepository;
    }

    public async Task<List<ContractTemplateDto>> GetListAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Default);

        var templates = await _repository.GetListAsync();
        var templateIds = templates.Select(x => x.Id).ToList();
        var allSections = templateIds.Count > 0
            ? await _sectionRepository.GetListAsync(x => templateIds.Contains(x.ContractTemplateId))
            : new List<ContractTemplateSection>();
        var sectionsByTemplateId = allSections.ToLookup(x => x.ContractTemplateId);

        return templates.Select(t => ToDto(t, sectionsByTemplateId[t.Id].ToList())).ToList();
    }

    public async Task<ContractTemplateDto> GetAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Default);

        var template = await _repository.GetAsync(id);
        var sections = await _sectionRepository.GetListAsync(x => x.ContractTemplateId == id);
        return ToDto(template, sections);
    }

    public async Task<ContractTemplateDto> CreateAsync(CreateUpdateContractTemplateDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Edit);

        var sections = ValidateSections(input);

        var template = new ContractTemplate(GuidGenerator.Create(), input.Name)
        {
            IsActive = input.IsActive,
            DefaultTermMonths = input.DefaultTermMonths
        };
        await _repository.InsertAsync(template, autoSave: true);

        await InsertSectionsAsync(template.Id, sections);

        return await GetAsync(template.Id);
    }

    public async Task<ContractTemplateDto> UpdateAsync(Guid id, CreateUpdateContractTemplateDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Edit);

        var sections = ValidateSections(input);

        var template = await _repository.GetAsync(id);
        template.Name = input.Name;
        template.IsActive = input.IsActive;
        template.DefaultTermMonths = input.DefaultTermMonths;
        await _repository.UpdateAsync(template, autoSave: true);

        var existingSections = await _sectionRepository.GetListAsync(x => x.ContractTemplateId == id);
        await _sectionRepository.DeleteManyAsync(existingSections);
        await InsertSectionsAsync(id, sections);

        return await GetAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Customers.Edit);

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _customerContractRepository.GetListAsync(x => x.ContractTemplateId == id)).Count, "Customer Contract")
        );

        var sections = await _sectionRepository.GetListAsync(x => x.ContractTemplateId == id);
        await _sectionRepository.DeleteManyAsync(sections);
        await _repository.DeleteAsync(id);
    }

    private static List<CreateUpdateContractTemplateSectionDto> ValidateSections(CreateUpdateContractTemplateDto input)
    {
        var sections = input.Sections.Where(x => !string.IsNullOrWhiteSpace(x.BodyText)).ToList();
        if (sections.Count == 0)
        {
            throw new UserFriendlyException("A contract template needs at least one section.");
        }

        return sections;
    }

    private async Task InsertSectionsAsync(Guid templateId, List<CreateUpdateContractTemplateSectionDto> sections)
    {
        for (var i = 0; i < sections.Count; i++)
        {
            await _sectionRepository.InsertAsync(
                new ContractTemplateSection(GuidGenerator.Create(), templateId, i, sections[i].BodyText)
                {
                    Heading = sections[i].Heading
                },
                autoSave: true);
        }
    }

    private static ContractTemplateDto ToDto(ContractTemplate template, List<ContractTemplateSection> sections)
    {
        return new ContractTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            IsActive = template.IsActive,
            DefaultTermMonths = template.DefaultTermMonths,
            Sections = sections.OrderBy(x => x.Order).Select(x => new ContractTemplateSectionDto
            {
                Id = x.Id,
                Order = x.Order,
                Heading = x.Heading,
                BodyText = x.BodyText
            }).ToList()
        };
    }
}
