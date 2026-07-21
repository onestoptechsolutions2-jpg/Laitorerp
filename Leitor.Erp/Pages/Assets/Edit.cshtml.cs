using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Dtos.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Assets;

[Authorize(Policy = ErpPermissions.Assets.Edit)]
public class EditModel : AbpPageModel
{
    private readonly ConfigurationItemAppService _configurationItemAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IFeatureChecker _featureChecker;

    public EditModel(
        ConfigurationItemAppService configurationItemAppService,
        IRepository<Customer, Guid> customerRepository,
        IFeatureChecker featureChecker)
    {
        _configurationItemAppService = configurationItemAppService;
        _customerRepository = customerRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateConfigurationItemDto Item { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.AssetManagement))
        {
            return NotFound();
        }

        var item = await _configurationItemAppService.GetAsync(Id);
        Item = new CreateUpdateConfigurationItemDto
        {
            Name = item.Name,
            CIType = item.CIType,
            CustomerId = item.CustomerId,
            SerialNumber = item.SerialNumber,
            Status = item.Status,
            PurchaseDate = item.PurchaseDate,
            WarrantyExpiryDate = item.WarrantyExpiryDate,
            Notes = item.Notes
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

        await _configurationItemAppService.UpdateAsync(Id, Item);
        return RedirectToPage("./Detail", new { id = Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = new List<SelectListItem> { new(L["None"], "") };
        CustomerOptions.AddRange(
            customers.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString()))
        );
    }
}
