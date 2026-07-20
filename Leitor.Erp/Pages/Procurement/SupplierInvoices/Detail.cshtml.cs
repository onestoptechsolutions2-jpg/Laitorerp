using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Procurement;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Procurement.SupplierInvoices;

[Authorize(Policy = ErpPermissions.Procurement.Default)]
public class DetailModel : AbpPageModel
{
    private readonly SupplierInvoiceAppService _supplierInvoiceAppService;
    private readonly SupplierInvoiceLineAppService _supplierInvoiceLineAppService;
    private readonly VendorPaymentAppService _vendorPaymentAppService;
    private readonly ProductAppService _productAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        SupplierInvoiceAppService supplierInvoiceAppService,
        SupplierInvoiceLineAppService supplierInvoiceLineAppService,
        VendorPaymentAppService vendorPaymentAppService,
        ProductAppService productAppService,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _supplierInvoiceAppService = supplierInvoiceAppService;
        _supplierInvoiceLineAppService = supplierInvoiceLineAppService;
        _vendorPaymentAppService = vendorPaymentAppService;
        _productAppService = productAppService;
        _vendorRepository = vendorRepository;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public SupplierInvoiceDto SupplierInvoice { get; set; } = null!;
    public IReadOnlyList<SupplierInvoiceLineDto> Lines { get; set; } = Array.Empty<SupplierInvoiceLineDto>();
    public IReadOnlyList<VendorPaymentDto> Payments { get; set; } = Array.Empty<VendorPaymentDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public Vendor Vendor { get; set; } = null!;

    [BindProperty]
    public CreateUpdateSupplierInvoiceLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    [BindProperty]
    public CreateUpdateVendorPaymentDto NewPayment { get; set; } = new()
    {
        PaymentDate = DateTime.Today
    };

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Procurement.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "SupplierInvoice", Id);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        SupplierInvoice = await _supplierInvoiceAppService.GetAsync(Id);
        Vendor = await _vendorRepository.GetAsync(SupplierInvoice.VendorId);

        var lines = await _supplierInvoiceLineAppService.GetListAsync(new GetSupplierInvoiceLineListInput
        {
            SupplierInvoiceId = Id,
            MaxResultCount = 1000
        });
        Lines = lines.Items;

        var payments = await _vendorPaymentAppService.GetListAsync(new GetVendorPaymentListInput
        {
            SupplierInvoiceId = Id,
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
        NewLine.SupplierInvoiceId = Id;
        await _supplierInvoiceLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _supplierInvoiceLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddPaymentAsync()
    {
        NewPayment.SupplierInvoiceId = Id;
        await _vendorPaymentAppService.CreateAsync(NewPayment);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeletePaymentAsync(Guid paymentId)
    {
        await _vendorPaymentAppService.DeleteAsync(paymentId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostPostToLedgerAsync()
    {
        await _supplierInvoiceAppService.PostToLedgerAsync(Id);
        return RedirectToPage(new { id = Id });
    }
}
