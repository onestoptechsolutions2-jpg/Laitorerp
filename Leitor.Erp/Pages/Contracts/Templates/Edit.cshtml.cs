using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Contracts.Templates;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class EditModel : AbpPageModel
{
    private const int ExtraBlankSectionCount = 3;

    private readonly ContractTemplateAppService _contractTemplateAppService;

    public EditModel(ContractTemplateAppService contractTemplateAppService)
    {
        _contractTemplateAppService = contractTemplateAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateContractTemplateDto Template { get; set; } = new();

    public async Task OnGetAsync()
    {
        var template = await _contractTemplateAppService.GetAsync(Id);
        Template = new CreateUpdateContractTemplateDto
        {
            Name = template.Name,
            IsActive = template.IsActive,
            DefaultTermMonths = template.DefaultTermMonths,
            Sections = template.Sections
                .OrderBy(x => x.Order)
                .Select(x => new CreateUpdateContractTemplateSectionDto { Heading = x.Heading, BodyText = x.BodyText })
                .ToList()
        };

        for (var i = 0; i < ExtraBlankSectionCount; i++)
        {
            Template.Sections.Add(new CreateUpdateContractTemplateSectionDto());
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _contractTemplateAppService.UpdateAsync(Id, Template);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Index") });
        }

        return RedirectToPage("./Index");
    }
}
