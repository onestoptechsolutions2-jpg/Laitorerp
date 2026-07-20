using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Sales.Orders;

[Authorize(Policy = ErpPermissions.Sales.Edit)]
public class EditModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public EditModel(
        OrderAppService orderAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _orderAppService = orderAppService;
        _customerRepository = customerRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateOrderDto Order { get; set; } = new();

    [BindProperty]
    public string UnlockReason { get; set; } = string.Empty;

    public OrderDto OrderDetails { get; set; } = null!;
    public bool CanUnlock { get; set; }
    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        CanUnlock = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Unlock);
        OrderDetails = await _orderAppService.GetAsync(Id);
        Order = new CreateUpdateOrderDto
        {
            CustomerId = OrderDetails.CustomerId,
            QuoteId = OrderDetails.QuoteId,
            Status = OrderDetails.Status,
            OrderDate = OrderDetails.OrderDate,
            Notes = OrderDetails.Notes,
            PaymentTerms = OrderDetails.PaymentTerms,
            CurrencyCode = OrderDetails.CurrencyCode
        };

        await LoadCustomerOptionsAsync();
        await LoadCurrencyOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomerOptionsAsync();
            await LoadCurrencyOptionsAsync();
            return Page();
        }

        await _orderAppService.UpdateAsync(Id, Order);
        return RedirectToPage("./Detail", new { id = Id });
    }

    public async Task<IActionResult> OnPostUnlockAsync()
    {
        await _orderAppService.UnlockForRevisionAsync(Id, UnlockReason);
        return RedirectToPage(new { id = Id });
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
    }
}
