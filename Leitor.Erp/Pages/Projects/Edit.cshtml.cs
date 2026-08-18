using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Features;
using Leitor.Erp.Pages.Shared;
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

[Authorize(Policy = ErpPermissions.Projects.Edit)]
public class EditModel : AbpPageModel
{
    private readonly ProjectAppService _projectAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(
        ProjectAppService projectAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Project, Guid> projectRepository,
        IFeatureChecker featureChecker)
    {
        _projectAppService = projectAppService;
        _customerRepository = customerRepository;
        _projectRepository = projectRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateProjectDto Project { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> DependsOnProjectOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement))
        {
            return NotFound();
        }

        var project = await _projectAppService.GetAsync(Id);
        Project = new CreateUpdateProjectDto
        {
            CustomerId = project.CustomerId,
            Title = project.Title,
            Description = project.Description,
            Status = project.Status,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Budget = project.Budget,
            DependsOnProjectId = project.DependsOnProjectId
        };

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

        await _projectAppService.UpdateAsync(Id, Project);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = Id }) });
        }

        return RedirectToPage("./Detail", new { id = Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        // Excludes this project itself - can't depend on itself.
        var projects = await _projectRepository.GetListAsync(x => x.Id != Id);
        DependsOnProjectOptions = new List<SelectListItem> { new(L["None"], "") };
        DependsOnProjectOptions.AddRange(
            projects.OrderBy(x => x.Title).Select(x => new SelectListItem($"{x.ProjectNumber} - {x.Title}", x.Id.ToString()))
        );
    }
}
