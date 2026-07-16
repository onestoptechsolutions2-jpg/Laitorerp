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

namespace Leitor.Erp.Pages.Sales.Quotes;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class DetailModel : AbpPageModel
{
    private readonly QuoteAppService _quoteAppService;
    private readonly QuoteLineAppService _quoteLineAppService;
    private readonly ProductAppService _productAppService;

    public DetailModel(
        QuoteAppService quoteAppService,
        QuoteLineAppService quoteLineAppService,
        ProductAppService productAppService)
    {
        _quoteAppService = quoteAppService;
        _quoteLineAppService = quoteLineAppService;
        _productAppService = productAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public QuoteDto Quote { get; set; } = null!;
    public IReadOnlyList<QuoteLineDto> Lines { get; set; } = Array.Empty<QuoteLineDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();

    [BindProperty]
    public CreateUpdateQuoteLineDto NewLine { get; set; } = new()
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
        Quote = await _quoteAppService.GetAsync(Id);

        var lines = await _quoteLineAppService.GetListAsync(new GetQuoteLineListInput
        {
            QuoteId = Id,
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
        NewLine.QuoteId = Id;
        await _quoteLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _quoteLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostConvertToOrderAsync()
    {
        var order = await _quoteAppService.ConvertToOrderAsync(Id);
        return RedirectToPage("/Sales/Orders/Detail", new { id = order.Id });
    }
}
