using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.ServiceCatalog;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceRequests;
using Leitor.Erp.Services.ServiceRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.ServiceRequests;

[Authorize(Policy = ErpPermissions.ServiceRequests.Create)]
public class CreateModel : AbpPageModel
{
    private readonly ServiceRequestAppService _serviceRequestAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<ServiceCatalogItem, Guid> _serviceCatalogItemRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        ServiceRequestAppService serviceRequestAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<ServiceCatalogItem, Guid> serviceCatalogItemRepository,
        IFeatureChecker featureChecker)
    {
        _serviceRequestAppService = serviceRequestAppService;
        _customerRepository = customerRepository;
        _serviceCatalogItemRepository = serviceCatalogItemRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateServiceRequestDto ServiceRequest { get; set; } = new()
    {
        RequestedDate = DateTime.Today
    };

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> ServiceCatalogItemOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ServiceRequestManagement))
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

        var request = await _serviceRequestAppService.CreateAsync(ServiceRequest);
        return RedirectToPage("./Detail", new { id = request.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var catalogItems = await _serviceCatalogItemRepository.GetListAsync(x => x.IsActive);
        ServiceCatalogItemOptions = new List<SelectListItem> { new(L["None"], "") };
        ServiceCatalogItemOptions.AddRange(
            catalogItems.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString()))
        );
    }
}
