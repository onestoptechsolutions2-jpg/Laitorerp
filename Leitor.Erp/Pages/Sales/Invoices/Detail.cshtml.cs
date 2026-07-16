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

namespace Leitor.Erp.Pages.Sales.Invoices;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class DetailModel : AbpPageModel
{
    private readonly InvoiceAppService _invoiceAppService;
    private readonly InvoiceLineAppService _invoiceLineAppService;
    private readonly PaymentAppService _paymentAppService;
    private readonly ProductAppService _productAppService;

    public DetailModel(
        InvoiceAppService invoiceAppService,
        InvoiceLineAppService invoiceLineAppService,
        PaymentAppService paymentAppService,
        ProductAppService productAppService)
    {
        _invoiceAppService = invoiceAppService;
        _invoiceLineAppService = invoiceLineAppService;
        _paymentAppService = paymentAppService;
        _productAppService = productAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public InvoiceDto Invoice { get; set; } = null!;
    public IReadOnlyList<InvoiceLineDto> Lines { get; set; } = Array.Empty<InvoiceLineDto>();
    public IReadOnlyList<PaymentDto> Payments { get; set; } = Array.Empty<PaymentDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();

    [BindProperty]
    public CreateUpdateInvoiceLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    [BindProperty]
    public CreateUpdatePaymentDto NewPayment { get; set; } = new()
    {
        PaymentDate = DateTime.Today
    };

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Invoice = await _invoiceAppService.GetAsync(Id);

        var lines = await _invoiceLineAppService.GetListAsync(new GetInvoiceLineListInput
        {
            InvoiceId = Id,
            MaxResultCount = 1000
        });
        Lines = lines.Items;

        var payments = await _paymentAppService.GetListAsync(new GetPaymentListInput
        {
            InvoiceId = Id,
            MaxResultCount = 1000
        });
        Payments = payments.Items;

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
        NewLine.InvoiceId = Id;
        await _invoiceLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _invoiceLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddPaymentAsync()
    {
        NewPayment.InvoiceId = Id;
        await _paymentAppService.CreateAsync(NewPayment);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeletePaymentAsync(Guid paymentId)
    {
        await _paymentAppService.DeleteAsync(paymentId);
        return RedirectToPage(new { id = Id });
    }
}
