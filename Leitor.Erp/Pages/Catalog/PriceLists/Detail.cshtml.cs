using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Catalog.PriceLists;

[Authorize(Policy = ErpPermissions.Catalog.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PriceListAppService _priceListAppService;
    private readonly PriceListItemAppService _priceListItemAppService;
    private readonly ProductAppService _productAppService;

    public DetailModel(
        PriceListAppService priceListAppService,
        PriceListItemAppService priceListItemAppService,
        ProductAppService productAppService)
    {
        _priceListAppService = priceListAppService;
        _priceListItemAppService = priceListItemAppService;
        _productAppService = productAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PriceListDto PriceList { get; set; } = null!;
    public IReadOnlyList<PriceListItemDto> Items { get; set; } = Array.Empty<PriceListItemDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();

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
        var pricedProductIds = Items.Select(x => x.ProductId).ToHashSet();
        ProductOptions = products.Items
            .Where(x => !pricedProductIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Name} ({x.UnitPrice:N2})", x.Id.ToString()))
            .ToList();
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
