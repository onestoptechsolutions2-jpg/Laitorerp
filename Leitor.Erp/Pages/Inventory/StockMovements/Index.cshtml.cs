using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Leitor.Erp.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Inventory.StockMovements;

[Authorize(Policy = ErpPermissions.Inventory.Default)]
public class IndexModel : AbpPageModel
{
    private readonly StockMovementAppService _stockMovementAppService;
    private readonly IRepository<Product, Guid> _productRepository;

    public IndexModel(StockMovementAppService stockMovementAppService, IRepository<Product, Guid> productRepository)
    {
        _stockMovementAppService = stockMovementAppService;
        _productRepository = productRepository;
    }

    public IReadOnlyList<StockMovementDto> Movements { get; set; } = Array.Empty<StockMovementDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();

    [BindProperty]
    public RecordStockAdjustmentDto NewAdjustment { get; set; } = new()
    {
        MovementDate = DateTime.Today
    };

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Inventory.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Movements = await _stockMovementAppService.GetListAsync(new GetStockMovementListInput());

        var products = await _productRepository.GetListAsync(x => x.TrackInventory);
        ProductOptions = products
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        await _stockMovementAppService.RecordAdjustmentAsync(NewAdjustment);
        return RedirectToPage();
    }
}
