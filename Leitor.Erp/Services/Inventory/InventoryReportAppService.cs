using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Inventory;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Inventory;

// Read-only aggregation, plain ApplicationService rather than CrudAppService - same convention as
// GeneralLedgerReportAppService. QuantityOnHand is always summed live from StockMovement rows here,
// never stored - same "compute, never store" discipline as the GL balances.
public class InventoryReportAppService : ApplicationService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;

    public InventoryReportAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<StockMovement, Guid> stockMovementRepository)
    {
        _productRepository = productRepository;
        _stockMovementRepository = stockMovementRepository;
    }

    public async Task<List<StockOnHandLineDto>> GetStockOnHandAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Inventory.Default);
        return await BuildLinesAsync();
    }

    public async Task<List<StockOnHandLineDto>> GetLowStockAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Inventory.Default);
        var lines = await BuildLinesAsync();
        return lines.Where(x => x.ReorderPoint.HasValue && x.QuantityOnHand <= x.ReorderPoint.Value).ToList();
    }

    private async Task<List<StockOnHandLineDto>> BuildLinesAsync()
    {
        var products = await _productRepository.GetListAsync(x => x.TrackInventory);
        if (products.Count == 0)
        {
            return new List<StockOnHandLineDto>();
        }

        var productIds = products.Select(x => x.Id).ToList();
        var onHandByProductId = (await _stockMovementRepository.GetListAsync(x => productIds.Contains(x.ProductId)))
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        return products
            .Select(product =>
            {
                var quantityOnHand = onHandByProductId.GetValueOrDefault(product.Id);
                return new StockOnHandLineDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    QuantityOnHand = quantityOnHand,
                    Cost = product.Cost,
                    Value = quantityOnHand * product.Cost,
                    ReorderPoint = product.ReorderPoint,
                    ReorderQuantity = product.ReorderQuantity
                };
            })
            .OrderBy(x => x.ProductName)
            .ToList();
    }
}
