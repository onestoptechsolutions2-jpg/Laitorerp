using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Sales.Invoices;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class IndexModel : AbpPageModel
{
    private readonly InvoiceAppService _invoiceAppService;

    public IndexModel(InvoiceAppService invoiceAppService)
    {
        _invoiceAppService = invoiceAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public IReadOnlyList<InvoiceDto> Invoices { get; set; } = Array.Empty<InvoiceDto>();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Delete);

        var result = await _invoiceAppService.GetListAsync(new GetInvoiceListInput
        {
            Filter = Filter,
            MaxResultCount = 1000
        });

        Invoices = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _invoiceAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter });
    }
}
