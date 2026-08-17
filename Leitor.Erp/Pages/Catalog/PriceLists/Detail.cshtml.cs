using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Services.ServiceCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Catalog.PriceLists;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PriceListAppService _priceListAppService;
    private readonly PriceListItemAppService _priceListItemAppService;
    private readonly ProductAppService _productAppService;
    private readonly ServiceCatalogItemAppService _serviceCatalogItemAppService;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        PriceListAppService priceListAppService,
        PriceListItemAppService priceListItemAppService,
        ProductAppService productAppService,
        ServiceCatalogItemAppService serviceCatalogItemAppService,
        IFeatureChecker featureChecker)
    {
        _priceListAppService = priceListAppService;
        _priceListItemAppService = priceListItemAppService;
        _productAppService = productAppService;
        _serviceCatalogItemAppService = serviceCatalogItemAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PriceListDto PriceList { get; set; } = null!;
    public IReadOnlyList<PriceListItemDto> Items { get; set; } = Array.Empty<PriceListItemDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public List<SelectListItem> ServiceOptions { get; set; } = new();

    [BindProperty]
    public CreateUpdatePriceListItemDto NewItem { get; set; } = new();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Catalog.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        PriceList = await _priceListAppService.GetAsync(Id);

        var items = await _priceListItemAppService.GetListAsync(new GetPriceListItemListInput
        {
            PriceListId = Id,
            MaxResultCount = 1000
        });
        Items = items.Items;

        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            IsActive = true,
            MaxResultCount = 1000
        });
        var pricedProductIds = Items.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).ToHashSet();
        ProductOptions = products.Items
            .Where(x => !pricedProductIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Name} ({x.UnitPrice:N2})", x.Id.ToString()))
            .ToList();

        // ServiceCatalog is a toggleable module (unlike Product/Catalog, always on) - skip
        // loading rather than let GetListAsync's [RequiresFeature] throw when it's off.
        if (await _featureChecker.IsEnabledAsync(ErpFeatures.ServiceCatalog))
        {
            var pricedServiceIds = Items.Where(x => x.ServiceCatalogItemId.HasValue).Select(x => x.ServiceCatalogItemId!.Value).ToHashSet();
            var services = await _serviceCatalogItemAppService.GetListAsync(new PagedAndSortedResultRequestDto { MaxResultCount = 1000 });
            ServiceOptions = services.Items
                .Where(x => x.IsActive && !pricedServiceIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
                .ToList();
        }
    }

    public async Task<IActionResult> OnPostAddItemAsync()
    {
        NewItem.PriceListId = Id;
        await _priceListItemAppService.CreateAsync(NewItem);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteItemAsync(Guid itemId)
    {
        await _priceListItemAppService.DeleteAsync(itemId);
        return RedirectToPage(new { id = Id });
    }
}
