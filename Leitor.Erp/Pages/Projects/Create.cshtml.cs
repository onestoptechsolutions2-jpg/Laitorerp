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

[Authorize(Policy = ErpPermissions.Projects.Create)]
public class CreateModel : AbpPageModel
{
    private readonly ProjectAppService _projectAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
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

    [BindProperty]
    public CreateUpdateProjectDto Project { get; set; } = new()
    {
        StartDate = DateTime.Today
    };

    // Set when arriving via a Completed project's "Create Follow-up Project" link - same
    // prefilled-Create-page mechanism as Contracts/Create's FromProjectId/PrefillTitle (no new
    // wizard page). Unlike the Project->Contract link, this needs no write-back step: the new
    // Project's own DependsOnProjectId field carries the relationship directly.
    [BindProperty(SupportsGet = true)]
    public Guid? DependsOnProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? PrefillCustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? PrefillTitle { get; set; }

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> DependsOnProjectOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement))
        {
            return NotFound();
        }

        if (DependsOnProjectId.HasValue)
        {
            Project.DependsOnProjectId = DependsOnProjectId;
        }
        if (PrefillCustomerId.HasValue)
        {
            Project.CustomerId = PrefillCustomerId.Value;
        }
        if (!string.IsNullOrWhiteSpace(PrefillTitle))
        {
            Project.Title = PrefillTitle;
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

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = project.Id }) });
        }

        return RedirectToPage("./Detail", new { id = project.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var projects = await _projectRepository.GetListAsync();
        DependsOnProjectOptions = new List<SelectListItem> { new(L["None"], "") };
        DependsOnProjectOptions.AddRange(
            projects.OrderBy(x => x.Title).Select(x => new SelectListItem($"{x.ProjectNumber} - {x.Title}", x.Id.ToString()))
        );
    }
}
