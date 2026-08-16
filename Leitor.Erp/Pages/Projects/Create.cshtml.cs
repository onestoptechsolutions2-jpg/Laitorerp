using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Projects;
using Leitor.Erp.Services.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Projects;

[Authorize(Policy = ErpPermissions.Projects.Create)]
public class CreateModel : AbpPageModel
{
    private readonly ProjectAppService _projectAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(ProjectAppService projectAppService, IRepository<Customer, Guid> customerRepository, IFeatureChecker featureChecker)
    {
        _projectAppService = projectAppService;
        _customerRepository = customerRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateProjectDto Project { get; set; } = new()
    {
        StartDate = DateTime.Today
    };

    public List<SelectListItem> CustomerOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement))
        {
            return NotFound();
        }

        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var project = await _projectAppService.CreateAsync(Project);
        return RedirectToPage("./Detail", new { id = project.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }
}
