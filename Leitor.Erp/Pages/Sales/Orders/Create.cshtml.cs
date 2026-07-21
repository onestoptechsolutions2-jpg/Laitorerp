using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Projects;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Sales.Orders;

[Authorize(Policy = ErpPermissions.Sales.Create)]
public class CreateModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IFeatureChecker _featureChecker;

    public CreateModel(
        OrderAppService orderAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<Project, Guid> projectRepository,
        IFeatureChecker featureChecker)
    {
        _orderAppService = orderAppService;
        _customerRepository = customerRepository;
        _currencyRepository = currencyRepository;
        _warehouseRepository = warehouseRepository;
        _projectRepository = projectRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty]
    public CreateUpdateOrderDto Order { get; set; } = new()
    {
        OrderDate = DateTime.Today
    };

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();
    public List<SelectListItem> WarehouseOptions { get; set; } = new();
    public List<SelectListItem> ProjectOptions { get; set; } = new();
    public bool CanUseProjects { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCustomerOptionsAsync();
        await LoadCurrencyOptionsAsync();
        await LoadWarehouseOptionsAsync();
        await LoadProjectOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomerOptionsAsync();
            await LoadCurrencyOptionsAsync();
            await LoadWarehouseOptionsAsync();
            await LoadProjectOptionsAsync();
            return Page();
        }

        var order = await _orderAppService.CreateAsync(Order);
        return RedirectToPage("./Detail", new { id = order.Id });
    }

    private async Task LoadCustomerOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();
    }

    private async Task LoadCurrencyOptionsAsync()
    {
        var currencies = await _currencyRepository.GetListAsync(x => x.IsActive);
        CurrencyOptions = currencies
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code, x.Code))
            .ToList();

        if (string.IsNullOrWhiteSpace(Order.CurrencyCode))
        {
            Order.CurrencyCode = currencies.FirstOrDefault(x => x.IsBaseCurrency)?.Code ?? string.Empty;
        }
    }

    private async Task LoadWarehouseOptionsAsync()
    {
        var warehouses = await _warehouseRepository.GetListAsync(x => x.IsActive);
        WarehouseOptions = warehouses
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        Order.WarehouseId ??= warehouses.FirstOrDefault(x => x.IsDefault)?.Id;
    }

    private async Task LoadProjectOptionsAsync()
    {
        CanUseProjects = await _featureChecker.IsEnabledAsync(ErpFeatures.ProjectManagement);
        if (!CanUseProjects)
        {
            return;
        }

        var projects = await _projectRepository.GetListAsync();
        ProjectOptions = new List<SelectListItem> { new(L["None"], "") };
        ProjectOptions.AddRange(
            projects.OrderByDescending(x => x.StartDate).Select(x => new SelectListItem($"{x.ProjectNumber} - {x.Title}", x.Id.ToString()))
        );
    }
}
