using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers Phase 4's Purchasing inheritance fixes: PO line cost inheritance from ProductVendor,
// warehouse defaulting, and per-line tax modeling on PurchaseOrderLine/SupplierInvoiceLine.
public class ProcurementInheritanceTests : ErpTestBase
{
    [Fact]
    public async Task PurchaseOrderLine_With_Zero_UnitPrice_Resolves_From_ProductVendor_Cost()
    {
        await EnsureDatabaseCreatedAsync();
        var vendorAppService = GetRequiredService<VendorAppService>();
        var purchaseOrderAppService = GetRequiredService<PurchaseOrderAppService>();
        var purchaseOrderLineAppService = GetRequiredService<PurchaseOrderLineAppService>();
        var productRepository = GetRequiredService<IRepository<Product, Guid>>();
        var productVendorRepository = GetRequiredService<IRepository<ProductVendor, Guid>>();

        var vendor = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Sourcing Vendor" });

        var product = new Product(Guid.NewGuid(), "Router", 200m);
        await productRepository.InsertAsync(product, autoSave: true);

        var productVendor = new ProductVendor(Guid.NewGuid(), product.Id, vendor.Id, 150m);
        await productVendorRepository.InsertAsync(productVendor, autoSave: true);

        var po = await purchaseOrderAppService.CreateAsync(new CreateUpdatePurchaseOrderDto
        {
            VendorId = vendor.Id,
            OrderDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        var line = await purchaseOrderLineAppService.CreateAsync(new CreateUpdatePurchaseOrderLineDto
        {
            PurchaseOrderId = po.Id,
            ProductId = product.Id,
            Description = "Router",
            UnitPrice = 0,
            Quantity = 1
        });

        Assert.Equal(150m, line.UnitPrice);
    }

    [Fact]
    public async Task PurchaseOrder_CreateAsync_Defaults_WarehouseId_From_Default_Warehouse()
    {
        await EnsureDatabaseCreatedAsync();
        var vendorAppService = GetRequiredService<VendorAppService>();
        var purchaseOrderAppService = GetRequiredService<PurchaseOrderAppService>();
        var warehouseRepository = GetRequiredService<IRepository<Warehouse, Guid>>();

        // Data seeding already provisions a default Warehouse (same seeded-defaults pattern as
        // base currency/tax rates) - assert against whichever one that is rather than inserting a
        // second IsDefault row, which would just create an ambiguous tie.
        var defaultWarehouse = (await warehouseRepository.GetListAsync(x => x.IsDefault)).FirstOrDefault();
        Assert.NotNull(defaultWarehouse);

        var vendor = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Vendor Without Explicit Warehouse" });

        var po = await purchaseOrderAppService.CreateAsync(new CreateUpdatePurchaseOrderDto
        {
            VendorId = vendor.Id,
            OrderDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        Assert.Equal(defaultWarehouse!.Id, po.WarehouseId);
    }

    [Fact]
    public async Task SupplierInvoiceLine_Resolves_TaxRate_And_Total_Is_Tax_Inclusive()
    {
        await EnsureDatabaseCreatedAsync();
        var vendorAppService = GetRequiredService<VendorAppService>();
        var purchaseOrderAppService = GetRequiredService<PurchaseOrderAppService>();
        var supplierInvoiceAppService = GetRequiredService<SupplierInvoiceAppService>();
        var supplierInvoiceLineAppService = GetRequiredService<SupplierInvoiceLineAppService>();

        var vendor = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Taxed Vendor" });
        var po = await purchaseOrderAppService.CreateAsync(new CreateUpdatePurchaseOrderDto
        {
            VendorId = vendor.Id,
            OrderDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });

        var invoice = await supplierInvoiceAppService.CreateAsync(new CreateUpdateSupplierInvoiceDto
        {
            PurchaseOrderId = po.Id,
            VendorId = vendor.Id,
            SupplierInvoiceNumber = "SUP-001",
            Status = SupplierInvoiceStatus.Issued,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "KES"
        });

        await supplierInvoiceLineAppService.CreateAsync(new CreateUpdateSupplierInvoiceLineDto
        {
            SupplierInvoiceId = invoice.Id,
            Description = "Consulting",
            UnitPrice = 1000m,
            Quantity = 1
        });

        var fetched = await supplierInvoiceAppService.GetAsync(invoice.Id);

        Assert.Equal(1000m, fetched.Subtotal);
        Assert.True(fetched.TaxAmount > 0);
        Assert.Equal(fetched.Subtotal + fetched.TaxAmount, fetched.Total);
    }
}
