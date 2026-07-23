using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Pos;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Inventory;
using Leitor.Erp.Services.Sales;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Pos;

// A completed-at-the-till sale, paid in full immediately - no separate Invoice/Payment step.
// CompleteSaleAsync posts stock movements via the same InventoryPostingService.PostAsync helper
// OrderAppService.OnOrderFulfilledAsync uses, and GL entries via JournalPostingService.PostAsync -
// Dr Cash/Cr Revenue for the sale total, then Dr Expense/Cr Inventory for COGS on tracked lines,
// exactly mirroring how a regular sale is accounted for (see OrderAppService.OnOrderFulfilledAsync
// and InvoiceAppService.PostToLedgerAsync).
[RequiresFeature(ErpFeatures.PointOfSale)]
public class PosSaleAppService : ApplicationService
{
    private readonly IRepository<PosSale, Guid> _saleRepository;
    private readonly IRepository<PosSaleLine, Guid> _lineRepository;
    private readonly IRepository<PosPayment, Guid> _paymentRepository;
    private readonly IRepository<PosSession, Guid> _sessionRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<StockMovement, Guid> _stockMovementRepository;
    private readonly IRepository<Account, Guid> _accountRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;
    private readonly IRepository<JournalEntry, Guid> _journalEntryRepository;
    private readonly IRepository<JournalEntryLine, Guid> _journalEntryLineRepository;
    private readonly IRepository<FiscalPeriod, Guid> _fiscalPeriodRepository;
    private readonly IDataFilter _dataFilter;

    public PosSaleAppService(
        IRepository<PosSale, Guid> saleRepository,
        IRepository<PosSaleLine, Guid> lineRepository,
        IRepository<PosPayment, Guid> paymentRepository,
        IRepository<PosSession, Guid> sessionRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<StockMovement, Guid> stockMovementRepository,
        IRepository<Account, Guid> accountRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IDataFilter dataFilter)
    {
        _saleRepository = saleRepository;
        _lineRepository = lineRepository;
        _paymentRepository = paymentRepository;
        _sessionRepository = sessionRepository;
        _productRepository = productRepository;
        _taxRateRepository = taxRateRepository;
        _customerRepository = customerRepository;
        _identityUserRepository = identityUserRepository;
        _stockMovementRepository = stockMovementRepository;
        _accountRepository = accountRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        _journalEntryRepository = journalEntryRepository;
        _journalEntryLineRepository = journalEntryLineRepository;
        _fiscalPeriodRepository = fiscalPeriodRepository;
        _dataFilter = dataFilter;
    }

    public async Task<List<ProductSearchResultDto>> SearchProductsAsync(ProductSearchInputDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Default);

        var term = input.Term;
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            return new List<ProductSearchResultDto>();
        }

        var products = (await _productRepository.GetListAsync(x =>
                x.IsActive &&
                (x.Name.Contains(term) || (x.Sku != null && x.Sku.Contains(term)) || (x.Barcode != null && x.Barcode.Contains(term)))))
            .Take(20)
            .ToList();

        var productIds = products.Select(x => x.Id).ToList();
        var onHandByProductId = productIds.Count > 0
            ? (await _stockMovementRepository.GetListAsync(x => x.WarehouseId == input.WarehouseId && productIds.Contains(x.ProductId)))
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity))
            : new Dictionary<Guid, decimal>();

        return products.Select(x => new ProductSearchResultDto
        {
            Id = x.Id,
            Name = x.Name,
            Sku = x.Sku,
            Barcode = x.Barcode,
            UnitPrice = x.UnitPrice,
            TrackInventory = x.TrackInventory,
            StockOnHand = x.TrackInventory ? onHandByProductId.GetValueOrDefault(x.Id) : null
        }).ToList();
    }

    public async Task<PosSaleDto> CompleteSaleAsync(CreatePosSaleDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Create);

        var session = await _sessionRepository.GetAsync(input.PosSessionId);
        if (session.Status != PosSessionStatus.Open)
        {
            throw new UserFriendlyException("This session is not open - open a session before ringing up a sale.");
        }

        var lines = input.Lines.Where(x => x.Quantity > 0).ToList();
        if (lines.Count == 0)
        {
            throw new UserFriendlyException("Add at least one item before completing the sale.");
        }

        var saleDate = Clock.Now;
        var exchangeRateToBase = await CurrencyRateResolver.ResolveAsync(_currencyRepository, _exchangeRateRepository, input.CurrencyCode, saleDate);

        var saleNumber = await DocumentNumbering.NextAsync(_saleRepository, _dataFilter, "POS-");
        var sale = new PosSale(GuidGenerator.Create(), saleNumber, session.Id, session.WarehouseId, CurrentUser.Id!.Value, saleDate)
        {
            CustomerId = input.CustomerId,
            CurrencyCode = input.CurrencyCode,
            ExchangeRateToBase = exchangeRateToBase,
            Notes = input.Notes
        };
        await _saleRepository.InsertAsync(sale, autoSave: true);

        var savedLines = new List<PosSaleLine>();
        foreach (var line in lines)
        {
            var product = line.ProductId.HasValue ? await _productRepository.FindAsync(line.ProductId.Value) : null;
            var (taxRateId, taxRatePercent) = await TaxRateResolver.ResolveAsync(_taxRateRepository, _productRepository, null, line.ProductId);

            var saleLine = new PosSaleLine(GuidGenerator.Create(), sale.Id, line.Description, line.UnitPrice)
            {
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                DiscountPercent = line.DiscountPercent,
                Cost = product?.Cost ?? 0,
                TaxRateId = taxRateId,
                TaxRatePercent = taxRatePercent
            };
            await _lineRepository.InsertAsync(saleLine, autoSave: true);
            savedLines.Add(saleLine);
        }

        var total = savedLines.Sum(x => x.Total());
        var amountTendered = input.Payments.Sum(x => x.Amount);
        if (Math.Round(amountTendered - total, 2) != 0)
        {
            throw new UserFriendlyException($"Payment total {amountTendered:N2} does not match the sale total {total:N2}.");
        }

        foreach (var payment in input.Payments)
        {
            await _paymentRepository.InsertAsync(
                new PosPayment(GuidGenerator.Create(), sale.Id, payment.Amount, payment.Method) { Reference = payment.Reference },
                autoSave: true);
        }

        await PostStockAndLedgerAsync(sale, savedLines, total);

        return await GetAsync(sale.Id);
    }

    public async Task<PosSaleDto> VoidAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Void);

        var sale = await _saleRepository.GetAsync(id);
        if (sale.Status == PosSaleStatus.Voided)
        {
            throw new UserFriendlyException("This sale has already been voided.");
        }

        var lines = await _lineRepository.GetListAsync(x => x.PosSaleId == id);
        var total = lines.Sum(x => x.Total());

        // Restock and reverse the GL with the exact opposite of what CompleteSaleAsync posted -
        // same "post an equal-and-opposite entry" philosophy as JournalEntryAppService.ReverseAsync,
        // done directly here since a void touches both stock and two possible GL entries at once.
        var productIds = lines.Where(x => x.ProductId.HasValue).Select(x => x.ProductId!.Value).Distinct().ToList();
        var trackedProducts = productIds.Count > 0
            ? (await _productRepository.GetListAsync(x => productIds.Contains(x.Id) && x.TrackInventory)).ToDictionary(x => x.Id)
            : new Dictionary<Guid, Product>();

        decimal cogsTotal = 0;
        foreach (var line in lines.Where(x => x.ProductId.HasValue && trackedProducts.ContainsKey(x.ProductId!.Value)))
        {
            await InventoryPostingService.PostAsync(
                _stockMovementRepository, GuidGenerator, line.ProductId!.Value, sale.WarehouseId,
                Clock.Now, line.Quantity, StockMovementType.AdjustmentIncrease,
                InventoryPostingService.SourceDocumentTypes.PosSale, sale.Id, "Void restock");

            cogsTotal += trackedProducts[line.ProductId!.Value].Cost * line.Quantity;
        }

        if (total > 0)
        {
            await JournalPostingService.PostAsync(
                _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
                Clock.Now, JournalPostingService.SourceDocumentTypes.PosSale, sale.Id,
                $"Void - POS Sale {sale.SaleNumber}",
                SystemAccountRole.Revenue, SystemAccountRole.Cash,
                total, sale.CurrencyCode, sale.ExchangeRateToBase);
        }

        if (cogsTotal > 0)
        {
            await JournalPostingService.PostAsync(
                _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
                Clock.Now, JournalPostingService.SourceDocumentTypes.PosSale, sale.Id,
                $"Void COGS reversal - POS Sale {sale.SaleNumber}",
                SystemAccountRole.Inventory, SystemAccountRole.Expense,
                cogsTotal, sale.CurrencyCode, sale.ExchangeRateToBase);
        }

        sale.Status = PosSaleStatus.Voided;
        await _saleRepository.UpdateAsync(sale, autoSave: true);

        return await GetAsync(sale.Id);
    }

    public async Task<PosSaleDto> GetAsync(Guid id)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Default);

        var sale = await _saleRepository.GetAsync(id);
        var lines = await _lineRepository.GetListAsync(x => x.PosSaleId == id);
        var payments = await _paymentRepository.GetListAsync(x => x.PosSaleId == id);

        return await ToDtoAsync(sale, lines, payments);
    }

    public async Task<List<PosSaleDto>> GetListBySessionAsync(Guid posSessionId)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Default);

        var sales = (await _saleRepository.GetListAsync(x => x.PosSessionId == posSessionId)).OrderByDescending(x => x.SaleDate).ToList();
        var saleIds = sales.Select(x => x.Id).ToList();

        var allLines = saleIds.Count > 0 ? (await _lineRepository.GetListAsync(x => saleIds.Contains(x.PosSaleId))).ToLookup(x => x.PosSaleId) : Enumerable.Empty<PosSaleLine>().ToLookup(x => x.PosSaleId);
        var allPayments = saleIds.Count > 0 ? (await _paymentRepository.GetListAsync(x => saleIds.Contains(x.PosSaleId))).ToLookup(x => x.PosSaleId) : Enumerable.Empty<PosPayment>().ToLookup(x => x.PosSaleId);

        var dtos = new List<PosSaleDto>();
        foreach (var sale in sales)
        {
            dtos.Add(await ToDtoAsync(sale, allLines[sale.Id].ToList(), allPayments[sale.Id].ToList()));
        }

        return dtos;
    }

    // Sales History page - across every session/warehouse, most recent first, capped to a
    // reasonable window rather than ever loading the whole table.
    public async Task<List<PosSaleDto>> GetRecentAsync(int days = 30)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Default);

        var since = Clock.Now.AddDays(-days);
        var sales = (await _saleRepository.GetListAsync(x => x.SaleDate >= since)).OrderByDescending(x => x.SaleDate).ToList();
        var saleIds = sales.Select(x => x.Id).ToList();

        var allLines = saleIds.Count > 0 ? (await _lineRepository.GetListAsync(x => saleIds.Contains(x.PosSaleId))).ToLookup(x => x.PosSaleId) : Enumerable.Empty<PosSaleLine>().ToLookup(x => x.PosSaleId);
        var allPayments = saleIds.Count > 0 ? (await _paymentRepository.GetListAsync(x => saleIds.Contains(x.PosSaleId))).ToLookup(x => x.PosSaleId) : Enumerable.Empty<PosPayment>().ToLookup(x => x.PosSaleId);

        var dtos = new List<PosSaleDto>();
        foreach (var sale in sales)
        {
            dtos.Add(await ToDtoAsync(sale, allLines[sale.Id].ToList(), allPayments[sale.Id].ToList()));
        }

        return dtos;
    }

    private async Task PostStockAndLedgerAsync(PosSale sale, List<PosSaleLine> lines, decimal total)
    {
        var trackedLines = new List<(PosSaleLine Line, Product Product)>();
        foreach (var line in lines.Where(x => x.ProductId.HasValue))
        {
            var product = await _productRepository.FindAsync(line.ProductId!.Value);
            if (product is { TrackInventory: true })
            {
                trackedLines.Add((line, product));
            }
        }

        foreach (var (line, _) in trackedLines)
        {
            await InventoryPostingService.PostAsync(
                _stockMovementRepository, GuidGenerator, line.ProductId!.Value, sale.WarehouseId,
                sale.SaleDate, line.Quantity, StockMovementType.Issue,
                InventoryPostingService.SourceDocumentTypes.PosSale, sale.Id);
        }

        if (total > 0)
        {
            await JournalPostingService.PostAsync(
                _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
                sale.SaleDate, JournalPostingService.SourceDocumentTypes.PosSale, sale.Id,
                $"POS Sale {sale.SaleNumber}",
                SystemAccountRole.Cash, SystemAccountRole.Revenue,
                total, sale.CurrencyCode, sale.ExchangeRateToBase);
        }

        var cogsTotal = trackedLines.Sum(x => x.Product.Cost * x.Line.Quantity);
        if (cogsTotal > 0)
        {
            await JournalPostingService.PostAsync(
                _accountRepository, _journalEntryRepository, _journalEntryLineRepository, _fiscalPeriodRepository, GuidGenerator, _dataFilter,
                sale.SaleDate, JournalPostingService.SourceDocumentTypes.PosSale, sale.Id,
                $"Cost of Goods Sold - POS Sale {sale.SaleNumber}",
                SystemAccountRole.Expense, SystemAccountRole.Inventory,
                cogsTotal, sale.CurrencyCode, sale.ExchangeRateToBase);
        }
    }

    private async Task<PosSaleDto> ToDtoAsync(PosSale sale, List<PosSaleLine> lines, List<PosPayment> payments)
    {
        string? customerName = null;
        if (sale.CustomerId.HasValue)
        {
            var customer = await _customerRepository.FindAsync(sale.CustomerId.Value);
            customerName = customer?.Name;
        }

        var salesperson = await _identityUserRepository.FindAsync(sale.SalespersonUserId);

        var lineDtos = lines.Select(x => new PosSaleLineDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            Description = x.Description,
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity,
            DiscountPercent = x.DiscountPercent,
            Cost = x.Cost,
            TaxRateId = x.TaxRateId,
            TaxRatePercent = x.TaxRatePercent,
            LineTotal = x.Total()
        }).ToList();

        var subtotal = lines.Sum(x => x.Subtotal());
        var total = lines.Sum(x => x.Total());

        return new PosSaleDto
        {
            Id = sale.Id,
            SaleNumber = sale.SaleNumber,
            PosSessionId = sale.PosSessionId,
            WarehouseId = sale.WarehouseId,
            CustomerId = sale.CustomerId,
            SalespersonUserId = sale.SalespersonUserId,
            SaleDate = sale.SaleDate,
            Status = sale.Status,
            CurrencyCode = sale.CurrencyCode,
            ExchangeRateToBase = sale.ExchangeRateToBase,
            Notes = sale.Notes,
            CustomerName = customerName,
            SalespersonUserName = salesperson?.UserName,
            Lines = lineDtos,
            Payments = payments.Select(x => new PosPaymentDto { Id = x.Id, Amount = x.Amount, Method = x.Method, Reference = x.Reference }).ToList(),
            Subtotal = subtotal,
            TaxAmount = total - subtotal,
            Total = total,
            AmountTendered = payments.Sum(x => x.Amount)
        };
    }
}
