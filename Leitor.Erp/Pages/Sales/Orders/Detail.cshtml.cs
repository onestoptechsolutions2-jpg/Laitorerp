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

namespace Leitor.Erp.Pages.Sales.Orders;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class DetailModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;
    private readonly OrderLineAppService _orderLineAppService;
    private readonly ProductAppService _productAppService;

    public DetailModel(
        OrderAppService orderAppService,
        OrderLineAppService orderLineAppService,
        ProductAppService productAppService)
    {
        _orderAppService = orderAppService;
        _orderLineAppService = orderLineAppService;
        _productAppService = productAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public OrderDto Order { get; set; } = null!;
    public IReadOnlyList<OrderLineDto> Lines { get; set; } = Array.Empty<OrderLineDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();

    [BindProperty]
    public CreateUpdateOrderLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Order = await _orderAppService.GetAsync(Id);

        var lines = await _orderLineAppService.GetListAsync(new GetOrderLineListInput
        {
            OrderId = Id,
            MaxResultCount = 1000
        });
        Lines = lines.Items;

        var products = await _productAppService.GetListAsync(new GetProductListInput
        {
            IsActive = true,
            MaxResultCount = 1000
        });
        ProductOptions = new List<SelectListItem> { new(L["None"], "") };
        ProductOptions.AddRange(
            products.Items.OrderBy(x => x.Name).Select(x => new SelectListItem($"{x.Name} ({x.UnitPrice:N2})", x.Id.ToString()))
        );
    }

    public async Task<IActionResult> OnPostAddLineAsync()
    {
        NewLine.OrderId = Id;
        await _orderLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _orderLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostConvertToInvoiceAsync()
    {
        var invoice = await _orderAppService.ConvertToInvoiceAsync(Id);
        return RedirectToPage("/Sales/Invoices/Detail", new { id = invoice.Id });
    }
}
