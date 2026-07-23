using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Pos.Register;

[Authorize(Policy = ErpPermissions.Pos.Default)]
public class IndexModel : AbpPageModel
{
    private readonly PosSessionAppService _posSessionAppService;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<TaxRate, Guid> _taxRateRepository;

    public IndexModel(
        PosSessionAppService posSessionAppService,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<TaxRate, Guid> taxRateRepository)
    {
        _posSessionAppService = posSessionAppService;
        _warehouseRepository = warehouseRepository;
        _customerRepository = customerRepository;
        _currencyRepository = currencyRepository;
        _taxRateRepository = taxRateRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? WarehouseId { get; set; }

    public List<SelectListItem> WarehouseOptions { get; set; } = new();
    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public string BaseCurrencyCode { get; set; } = string.Empty;

    // Used only for a live, client-side "estimated total" while building the cart - the
    // authoritative total (which can differ if a specific product overrides the tax rate) is
    // always recomputed server-side in PosSaleAppService.CompleteSaleAsync.
    public decimal DefaultTaxRatePercent { get; set; }
    public PosSessionDto? OpenSession { get; set; }
    public bool CanManageSessions { get; set; }
    public bool CanVoid { get; set; }

    [BindProperty]
    public OpenPosSessionDto OpenInput { get; set; } = new();

    [BindProperty]
    public ClosePosSessionDto CloseInput { get; set; } = new();

    public async Task OnGetAsync()
    {
        CanManageSessions = await AuthorizationService.IsGrantedAsync(ErpPermissions.Pos.ManageSessions);
        CanVoid = await AuthorizationService.IsGrantedAsync(ErpPermissions.Pos.Void);

        var warehouses = await _warehouseRepository.GetListAsync(x => x.IsActive);
        WarehouseOptions = warehouses.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        if (!WarehouseId.HasValue)
        {
            WarehouseId = warehouses.FirstOrDefault(x => x.IsDefault)?.Id ?? warehouses.FirstOrDefault()?.Id;
        }

        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();

        BaseCurrencyCode = (await _currencyRepository.GetListAsync(x => x.IsBaseCurrency)).FirstOrDefault()?.Code ?? string.Empty;
        DefaultTaxRatePercent = (await _taxRateRepository.GetListAsync(x => x.IsDefault && x.TaxType == TaxType.Vat)).FirstOrDefault()?.Percent ?? 0;

        if (WarehouseId.HasValue)
        {
            OpenSession = await _posSessionAppService.GetCurrentOpenAsync(WarehouseId.Value);
        }
    }

    public async Task<IActionResult> OnPostOpenSessionAsync()
    {
        await _posSessionAppService.OpenAsync(OpenInput);
        return RedirectToPage(new { WarehouseId = OpenInput.WarehouseId });
    }

    public async Task<IActionResult> OnPostCloseSessionAsync(Guid sessionId, Guid warehouseId)
    {
        await _posSessionAppService.CloseAsync(sessionId, CloseInput);
        return RedirectToPage(new { WarehouseId = warehouseId });
    }
}
