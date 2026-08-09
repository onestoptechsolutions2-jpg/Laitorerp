using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Sales.Quotes;

[Authorize(Policy = ErpPermissions.Sales.Create)]
public class CreateModel : AbpPageModel
{
    private readonly QuoteAppService _quoteAppService;
    private readonly CustomerAppService _customerAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public CreateModel(
        QuoteAppService quoteAppService,
        CustomerAppService customerAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _quoteAppService = quoteAppService;
        _customerAppService = customerAppService;
        _customerRepository = customerRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty]
    public CreateUpdateQuoteDto Quote { get; set; } = new()
    {
        IssueDate = DateTime.Today
    };

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

    // Keyed by Customer.Id.ToString() so it serializes straight into the page's inline script -
    // lets the Currency field follow the Customer dropdown client-side without a round trip, same
    // "suggest, don't lock" pattern as everywhere else (the field stays a plain editable select).
    public Dictionary<string, string> CustomerDefaultCurrencies { get; set; } = new();

    public async Task OnGetAsync()
    {
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

        var quote = await _quoteAppService.CreateAsync(Quote);
        return RedirectToPage("./Detail", new { id = quote.Id });
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostCreateCustomerAsync([FromBody] CreateCustomerRequest request)
    {
        // AJAX handler for creating a new customer from this form - a brand-new prospect with no
        // existing Customer record is the single most common reason to be creating a Quote at all,
        // so without this the very first Quote for a new business is a dead end.
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return new JsonResult(new { error = "Customer name is required" }) { StatusCode = 400 };
            }

            var customer = await _customerAppService.CreateAsync(new CreateUpdateCustomerDto
            {
                Name = request.Name.Trim(),
                Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim()
            });

            return new JsonResult(new { id = customer.Id, name = customer.Name, defaultCurrencyCode = customer.DefaultCurrencyCode });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = $"Error creating customer: {ex.Message}" }) { StatusCode = 400 };
        }
    }

    private async Task LoadCustomerOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        CustomerDefaultCurrencies = customers
            .Where(x => !string.IsNullOrWhiteSpace(x.DefaultCurrencyCode))
            .ToDictionary(x => x.Id.ToString(), x => x.DefaultCurrencyCode!);
    }

    private async Task LoadCurrencyOptionsAsync()
    {
        var currencies = await _currencyRepository.GetListAsync(x => x.IsActive);
        CurrencyOptions = currencies
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem(x.Code, x.Code))
            .ToList();

        if (string.IsNullOrWhiteSpace(Quote.CurrencyCode))
        {
            Quote.CurrencyCode = currencies.FirstOrDefault(x => x.IsBaseCurrency)?.Code ?? string.Empty;
        }
    }
}
