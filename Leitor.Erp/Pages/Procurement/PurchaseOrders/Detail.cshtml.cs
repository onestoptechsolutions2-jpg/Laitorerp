using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Procurement;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace Leitor.Erp.Pages.Procurement.PurchaseOrders;

[Authorize(Policy = ErpPermissions.Procurement.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly PurchaseOrderLineAppService _purchaseOrderLineAppService;
    private readonly ProductAppService _productAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyOptions _companyOptions;

    public DetailModel(
        PurchaseOrderAppService purchaseOrderAppService,
        PurchaseOrderLineAppService purchaseOrderLineAppService,
        ProductAppService productAppService,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<Customer, Guid> customerRepository,
        IEmailSender emailSender,
        IOptions<ErpCompanyOptions> companyOptions)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
        _purchaseOrderLineAppService = purchaseOrderLineAppService;
        _productAppService = productAppService;
        _vendorRepository = vendorRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _emailSender = emailSender;
        _companyOptions = companyOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PurchaseOrderDto PurchaseOrder { get; set; } = null!;
    public IReadOnlyList<PurchaseOrderLineDto> Lines { get; set; } = Array.Empty<PurchaseOrderLineDto>();
    public List<SelectListItem> ProductOptions { get; set; } = new();
    public Vendor Vendor { get; set; } = null!;
    public Customer? ShipToCustomer { get; set; }

    [BindProperty]
    public CreateUpdatePurchaseOrderLineDto NewLine { get; set; } = new()
    {
        Quantity = 1
    };

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Procurement.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        PurchaseOrder = await _purchaseOrderAppService.GetAsync(Id);
        Vendor = await _vendorRepository.GetAsync(PurchaseOrder.VendorId);

        ShipToCustomer = null;
        if (PurchaseOrder.ShipToCustomer && PurchaseOrder.SourceOrderId.HasValue)
        {
            var sourceOrder = await _orderRepository.GetAsync(PurchaseOrder.SourceOrderId.Value);
            ShipToCustomer = await _customerRepository.GetAsync(sourceOrder.CustomerId);
        }

        var lines = await _purchaseOrderLineAppService.GetListAsync(new GetPurchaseOrderLineListInput
        {
            PurchaseOrderId = Id,
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
        NewLine.PurchaseOrderId = Id;
        await _purchaseOrderLineAppService.CreateAsync(NewLine);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(Guid lineId)
    {
        await _purchaseOrderLineAppService.DeleteAsync(lineId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadAsync();
        var pdfBytes = PurchaseOrderPdfDocument.Generate(PurchaseOrder, Lines, Vendor, _companyOptions, ShipToCustomer);
        return File(pdfBytes, "application/pdf", $"{PurchaseOrder.PONumber}.pdf");
    }

    public async Task<IActionResult> OnPostEmailAsync()
    {
        await LoadAsync();

        if (!string.IsNullOrWhiteSpace(Vendor.Email))
        {
            var pdfBytes = PurchaseOrderPdfDocument.Generate(PurchaseOrder, Lines, Vendor, _companyOptions, ShipToCustomer);
            await _emailSender.SendAsync(
                Vendor.Email,
                $"Purchase Order {PurchaseOrder.PONumber}",
                $"Dear {Vendor.Name},\n\nPlease find attached purchase order {PurchaseOrder.PONumber}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = $"{PurchaseOrder.PONumber}.pdf", File = pdfBytes }
                    }
                }
            );
        }

        return RedirectToPage(new { id = Id });
    }
}
