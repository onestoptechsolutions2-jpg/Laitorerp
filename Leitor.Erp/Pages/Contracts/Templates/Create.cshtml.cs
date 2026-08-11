using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Contracts.Templates;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class CreateModel : AbpPageModel
{
    private const int BlankSectionCount = 15;

    private readonly ContractTemplateAppService _contractTemplateAppService;

    public CreateModel(ContractTemplateAppService contractTemplateAppService)
    {
        _contractTemplateAppService = contractTemplateAppService;
    }

    [BindProperty]
    public CreateUpdateContractTemplateDto Template { get; set; } = new();

    public void OnGet()
    {
        for (var i = 0; i < BlankSectionCount; i++)
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

        await _contractTemplateAppService.CreateAsync(Template);
        return RedirectToPage("./Index");
    }
}
