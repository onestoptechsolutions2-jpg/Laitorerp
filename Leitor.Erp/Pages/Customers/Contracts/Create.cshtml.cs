using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Customers.Contracts;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class CreateModel : AbpPageModel
{
    private readonly CustomerContractAppService _customerContractAppService;
    private readonly ContractTemplateAppService _contractTemplateAppService;
    private readonly IRepository<Project, Guid> _projectRepository;

    public CreateModel(
        CustomerContractAppService customerContractAppService,
        ContractTemplateAppService contractTemplateAppService,
        IRepository<Project, Guid> projectRepository)
    {
        _customerContractAppService = customerContractAppService;
        _contractTemplateAppService = contractTemplateAppService;
        _projectRepository = projectRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

    // Set when arriving via the Project Detail page's "Convert to Contract" link - prefills the
    // title/start date from the Project and, on save, links the Project back to the new contract
    // (Project.ConvertedToContractId) so the "projects feed the recurring contract" flow is
    // recorded, not just navigated. No new wizard page - this extends the existing Create page.
    [BindProperty(SupportsGet = true)]
    public Guid? FromProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PrefillTitle { get; set; }

    [BindProperty]
    public CreateUpdateCustomerContractDto Contract { get; set; } = new()
    {
        StartDate = DateTime.Today
    };

    [BindProperty]
    public List<int> SelectedServiceFlags { get; set; } = new();

    public List<SelectListItem> ContractTemplateOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        Contract.CustomerId = CustomerId;

        if (!string.IsNullOrWhiteSpace(PrefillTitle))
        {
            Contract.Title = PrefillTitle;
        }

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Contract.CustomerId = CustomerId;
        Contract.ServicesIncluded = (ContractServiceScope)SelectedServiceFlags.Aggregate(0, (acc, flag) => acc | flag);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var contract = await _customerContractAppService.CreateAsync(Contract);

        if (FromProjectId.HasValue)
        {
            var project = await _projectRepository.FindAsync(FromProjectId.Value);
            if (project != null)
            {
                project.ConvertedToContractId = contract.Id;
                await _projectRepository.UpdateAsync(project);
            }
        }

        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }

    private async Task LoadOptionsAsync()
    {
        var templates = await _contractTemplateAppService.GetListAsync();
        ContractTemplateOptions = new List<SelectListItem> { new(L["None"], "") };
        ContractTemplateOptions.AddRange(
            templates.Where(x => x.IsActive).Select(x => new SelectListItem(x.Name, x.Id.ToString()))
        );
    }
}
