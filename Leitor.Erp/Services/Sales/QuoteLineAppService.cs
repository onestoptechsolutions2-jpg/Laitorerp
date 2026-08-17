using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class QuoteLineAppService :
    CrudAppService<QuoteLine, QuoteLineDto, Guid, GetQuoteLineListInput, CreateUpdateQuoteLineDto>
{
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductBundleItem, Guid> _bundleItemRepository;
    private readonly IRepository<PriceListItem, Guid> _priceListItemRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;

    public QuoteLineAppService(
        IRepository<QuoteLine, Guid> repository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductBundleItem, Guid> bundleItemRepository,
        IRepository<PriceListItem, Guid> priceListItemRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<Customer, Guid> customerRepository)
        : base(repository)
    {
        _taxRateRepository = taxRateRepository;
        _productRepository = productRepository;
        _bundleItemRepository = bundleItemRepository;
        _priceListItemRepository = priceListItemRepository;
        _quoteRepository = quoteRepository;
        _customerRepository = customerRepository;

        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    // When the selected Product is a bundle, explode it into one line per component instead of
    // one opaque bundle line - each component keeps its own price/cost/tax, so the actual document
    // itemizes what's being delivered (installation packages: hardware + labor sold together but
    // billed/costed individually). Every other Create call (the overwhelming majority - a regular,
    // non-bundle product or no product at all) falls straight through to the unchanged base
    // behavior. Returns the DTO of the last line inserted, matching the base method's single-DTO
    // return shape - the only caller (Pages/Sales/Quotes/Detail.cshtml.cs's OnPostAddLineAsync)
    // discards the return value and just redirects back to the Quote.
    public override async Task<QuoteLineDto> CreateAsync(CreateUpdateQuoteLineDto input)
    {
        await CheckCreatePolicyAsync();

        if (!input.ProductId.HasValue)
        {
            return await base.CreateAsync(input);
        }

        var product = await _productRepository.FindAsync(input.ProductId.Value);
        if (product is not { IsBundle: true })
        {
            return await base.CreateAsync(input);
        }

        var bundleItems = await _bundleItemRepository.GetListAsync(x => x.BundleProductId == product.Id);
        if (bundleItems.Count == 0)
        {
            throw new UserFriendlyException("This bundle has no components configured.");
        }

        QuoteLine? lastLine = null;
        foreach (var bundleItem in bundleItems)
        {
            var component = await _productRepository.GetAsync(bundleItem.ComponentProductId);
            var (taxRateId, taxRatePercent) = await TaxRateResolver.ResolveAsync(_taxRateRepository, _productRepository, null, component.Id);

            var line = new QuoteLine(GuidGenerator.Create(), input.QuoteId, component.Name, component.UnitPrice)
            {
                ProductId = component.Id,
                Quantity = bundleItem.Quantity,
                Cost = component.Cost,
                TaxRateId = taxRateId,
                TaxRatePercent = taxRatePercent
            };
            await Repository.InsertAsync(line, autoSave: true);
            lastLine = line;
        }

        var dto = await MapToGetOutputDtoAsync(lastLine!);
        ComputeLineTotal(dto);
        return dto;
    }

    protected override async Task<IQueryable<QuoteLine>> CreateFilteredQueryAsync(GetQuoteLineListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.QuoteId.HasValue, x => x.QuoteId == input.QuoteId!.Value);
    }

    public override async Task<PagedResultDto<QuoteLineDto>> GetListAsync(GetQuoteLineListInput input)
    {
        var result = await base.GetListAsync(input);
        foreach (var dto in result.Items)
        {
            ComputeLineTotal(dto);
        }

        return result;
    }

    public override async Task<QuoteLineDto> GetAsync(Guid id)
    {
        var dto = await base.GetAsync(id);
        ComputeLineTotal(dto);
        return dto;
    }

    private static void ComputeLineTotal(QuoteLineDto dto)
    {
        dto.LineTotal = dto.Subtotal();
        dto.MarginPercent = dto.UnitPrice > 0 ? Math.Round(100m * (dto.UnitPrice - dto.Cost) / dto.UnitPrice, 1) : null;
    }

    protected override async Task<QuoteLine> MapToEntityAsync(CreateUpdateQuoteLineDto createInput)
    {
        var entity = new QuoteLine(GuidGenerator.Create(), createInput.QuoteId, createInput.Description, createInput.UnitPrice);
        await CopyToEntityAsync(createInput, entity, applyCustomerDefaults: true);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateQuoteLineDto updateInput, QuoteLine entity)
    {
        await CopyToEntityAsync(updateInput, entity, applyCustomerDefaults: false);
    }

    private async Task CopyToEntityAsync(CreateUpdateQuoteLineDto input, QuoteLine entity, bool applyCustomerDefaults)
    {
        entity.QuoteId = input.QuoteId;
        entity.ProductId = input.ProductId;
        entity.ServiceCatalogItemId = input.ServiceCatalogItemId;
        entity.Description = input.Description;
        entity.UnitPrice = input.UnitPrice;
        entity.Quantity = input.Quantity;
        entity.DiscountPercent = input.DiscountPercent;
        entity.Cost = input.Cost;

        // UnitPrice == 0 means the form's untouched default - resolve a suggested price from the
        // customer's price list rather than leaving the line free. An explicit 0 (a genuine
        // giveaway line) is indistinguishable from this and gets re-resolved too, same tradeoff
        // TaxRateResolver already accepts for a null TaxRateId. DiscountPercent only defaults on
        // Create (applyCustomerDefaults) - 0% is too common a legitimate value on an edit to guess.
        if (input.ProductId.HasValue && (entity.UnitPrice == 0 || (applyCustomerDefaults && entity.DiscountPercent == 0)))
        {
            var quote = await _quoteRepository.FindAsync(input.QuoteId);
            var customer = quote != null ? await _customerRepository.FindAsync(quote.CustomerId) : null;

            if (entity.UnitPrice == 0)
            {
                var priceListId = quote?.PriceListId ?? customer?.DefaultPriceListId;
                entity.UnitPrice = await PriceListResolver.ResolveUnitPriceAsync(
                    _priceListItemRepository, _productRepository, priceListId, input.ProductId.Value);
            }

            if (applyCustomerDefaults && entity.DiscountPercent == 0 && customer != null)
            {
                entity.DiscountPercent = customer.DiscountPercent;
            }
        }
        // Services have no Product.UnitPrice-style fallback (see PriceListResolver.
        // ResolveServiceRateAsync's own comment) - a service line with no rate-card match just
        // stays at whatever was typed, same as a plain one-off line with no catalog reference.
        else if (input.ServiceCatalogItemId.HasValue && entity.UnitPrice == 0)
        {
            var quote = await _quoteRepository.FindAsync(input.QuoteId);
            var customer = quote != null ? await _customerRepository.FindAsync(quote.CustomerId) : null;
            var priceListId = quote?.PriceListId ?? customer?.DefaultPriceListId;

            var resolved = await PriceListResolver.ResolveServiceRateAsync(_priceListItemRepository, priceListId, input.ServiceCatalogItemId.Value);
            if (resolved.HasValue)
            {
                entity.UnitPrice = resolved.Value.UnitPrice;
            }

            if (applyCustomerDefaults && entity.DiscountPercent == 0 && customer != null)
            {
                entity.DiscountPercent = customer.DiscountPercent;
            }
        }

        (entity.TaxRateId, entity.TaxRatePercent) = await TaxRateResolver.ResolveAsync(
            _taxRateRepository, _productRepository, input.TaxRateId, input.ProductId);
    }

    // Fetch product details for auto-population in line item form. If priceListId is provided,
    // use the price list item's price; otherwise use the product's standard price. Always returns
    // product cost and tax rate for the line.
    public async Task<ProductDetailDto> GetProductDetailsAsync(Guid productId, Guid? priceListId = null)
    {
        await CheckGetPolicyAsync();

        var product = await _productRepository.FindAsync(productId);
        if (product == null)
        {
            throw new UserFriendlyException("Product not found.");
        }

        decimal unitPrice = product.UnitPrice;

        // If a price list is specified, look up the price from the price list
        if (priceListId.HasValue)
        {
            var priceListItem = (await _priceListItemRepository.GetListAsync(x =>
                x.PriceListId == priceListId.Value && x.ProductId == productId)).FirstOrDefault();

            if (priceListItem != null)
            {
                unitPrice = priceListItem.UnitPrice;
            }
        }

        var (taxRateId, taxRatePercent) = await TaxRateResolver.ResolveAsync(
            _taxRateRepository, _productRepository, null, productId);

        return new ProductDetailDto
        {
            ProductId = product.Id,
            Name = product.Name,
            Description = product.Name,  // Use product name as description
            UnitPrice = unitPrice,
            Cost = product.Cost,
            TaxRateId = taxRateId,
            TaxRatePercent = taxRatePercent
        };
    }
}
