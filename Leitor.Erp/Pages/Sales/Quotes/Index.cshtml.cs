using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Sales.Quotes;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class IndexModel : AbpPageModel
{
    private readonly QuoteAppService _quoteAppService;

    public IndexModel(QuoteAppService quoteAppService)
    {
        _quoteAppService = quoteAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public IReadOnlyList<QuoteDto> Quotes { get; set; } = Array.Empty<QuoteDto>();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Delete);

        var result = await _quoteAppService.GetListAsync(new GetQuoteListInput
        {
            Filter = Filter,
            MaxResultCount = 1000
        });

        Quotes = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _quoteAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter });
    }
}
