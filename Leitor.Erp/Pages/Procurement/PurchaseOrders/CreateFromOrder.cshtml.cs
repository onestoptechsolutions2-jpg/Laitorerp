using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Procurement.PurchaseOrders;

// Two-step, no-JS flow (matching this app's GET-based filter-reload convention rather than
// AJAX/modals): Step 1 (no VendorId yet) picks a vendor; Step 2 (VendorId present, via query
// string) shows the Order's lines pre-selected/pre-costed from ProductVendor for that vendor.
// A Purchase Order can only go to one vendor at a time, so an Order needing several vendors just
// repeats this flow once per vendor - same "everything is an explicit manual action" style as the
// rest of this app.
[Authorize(Policy = ErpPermissions.Procurement.Create)]
public class CreateFromOrderModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;
    private readonly OrderLineAppService _orderLineAppService;
    private readonly PurchaseOrderAppService _purchaseOrderAppService;
    private readonly PurchaseOrderLineAppService _purchaseOrderLineAppService;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly IRepository<ProductVendor, Guid> _productVendorRepository;

    public CreateFromOrderModel(
        OrderAppService orderAppService,
        OrderLineAppService orderLineAppService,
        PurchaseOrderAppService purchaseOrderAppService,
        PurchaseOrderLineAppService purchaseOrderLineAppService,
        IRepository<Vendor, Guid> vendorRepository,
        IRepository<ProductVendor, Guid> productVendorRepository)
    {
        _orderAppService = orderAppService;
        _orderLineAppService = orderLineAppService;
        _purchaseOrderAppService = purchaseOrderAppService;
        _purchaseOrderLineAppService = purchaseOrderLineAppService;
        _vendorRepository = vendorRepository;
        _productVendorRepository = productVendorRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid OrderId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? VendorId { get; set; }

    public OrderDto Order { get; set; } = null!;
    public List<SelectListItem> VendorOptions { get; set; } = new();

    [BindProperty]
    public List<LineSelectionModel> Lines { get; set; } = new();

    [BindProperty]
    public bool ShipToCustomer { get; set; } = true;

    [BindProperty]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    public async Task OnGetAsync()
    {
        Order = await _orderAppService.GetAsync(OrderId);

        var orderLines = await _orderLineAppService.GetListAsync(new GetOrderLineListInput
        {
            OrderId = OrderId,
            MaxResultCount = 1000
        });

        var productIds = orderLines.Items
            .Where(x => x.ProductId.HasValue)
            .Select(x => x.ProductId!.Value)
            .Distinct()
            .ToList();

        if (VendorId.HasValue)
        {
            var productVendors = productIds.Count > 0
                ? await _productVendorRepository.GetListAsync(x => x.VendorId == VendorId.Value && productIds.Contains(x.ProductId))
                : new List<ProductVendor>();
            var costByProductId = productVendors.ToDictionary(x => x.ProductId, x => x.Cost);

            Lines = orderLines.Items.Select(line => new LineSelectionModel
            {
                OrderLineId = line.Id,
                Include = line.ProductId.HasValue && costByProductId.ContainsKey(line.ProductId.Value),
                ProductId = line.ProductId,
                Description = line.Description,
                UnitPrice = line.ProductId.HasValue && costByProductId.TryGetValue(line.ProductId.Value, out var cost)
                    ? cost
                    : line.UnitPrice,
                Quantity = line.Quantity,
                DiscountPercent = 0
            }).ToList();
        }
        else
        {
            // Filter the vendor dropdown to vendors that actually supply something on this order -
            // falls back to every vendor if none of the order's lines have a catalog product/
            // sourcing link yet, so the flow is never a dead end.
            var relevantVendorIds = productIds.Count > 0
                ? (await _productVendorRepository.GetListAsync(x => productIds.Contains(x.ProductId))).Select(x => x.VendorId).Distinct().ToList()
                : new List<Guid>();

            var vendors = relevantVendorIds.Count > 0
                ? await _vendorRepository.GetListAsync(x => relevantVendorIds.Contains(x.Id))
                : await _vendorRepository.GetListAsync();

            VendorOptions = vendors.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!VendorId.HasValue)
        {
            return RedirectToPage(new { orderId = OrderId });
        }

        var sourceOrder = await _orderAppService.GetAsync(OrderId);

        var purchaseOrder = await _purchaseOrderAppService.CreateAsync(new CreateUpdatePurchaseOrderDto
        {
            VendorId = VendorId.Value,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateTime.Today,
            ExpectedDeliveryDate = ExpectedDeliveryDate,
            Notes = Notes,
            SourceOrderId = OrderId,
            ShipToCustomer = ShipToCustomer,
            // A dropship PO is priced in whatever currency the sourcing vendor bills in, but this
            // form has no vendor-currency concept yet - defaulting to the source Order's currency
            // is the closest sensible v1 behavior; editable afterward on the PO itself.
            CurrencyCode = sourceOrder.CurrencyCode
        });

        foreach (var line in Lines.Where(x => x.Include))
        {
            await _purchaseOrderLineAppService.CreateAsync(new CreateUpdatePurchaseOrderLineDto
            {
                PurchaseOrderId = purchaseOrder.Id,
                ProductId = line.ProductId,
                Description = line.Description,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                DiscountPercent = line.DiscountPercent
            });
        }

        return RedirectToPage("/Procurement/PurchaseOrders/Detail", new { id = purchaseOrder.Id });
    }

    public class LineSelectionModel
    {
        public Guid OrderLineId { get; set; }
        public bool Include { get; set; }
        public Guid? ProductId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}
