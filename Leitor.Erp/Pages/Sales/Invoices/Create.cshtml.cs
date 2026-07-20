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

namespace Leitor.Erp.Pages.Sales.Invoices;

[Authorize(Policy = ErpPermissions.Sales.Create)]
public class CreateModel : AbpPageModel
{
    private readonly InvoiceAppService _invoiceAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public CreateModel(
        InvoiceAppService invoiceAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _invoiceAppService = invoiceAppService;
        _customerRepository = customerRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty]
    public CreateUpdateInvoiceDto Invoice { get; set; } = new()
    {
        IssueDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(30)
    };

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

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

        var invoice = await _invoiceAppService.CreateAsync(Invoice);
        return RedirectToPage("./Detail", new { id = invoice.Id });
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

        if (string.IsNullOrWhiteSpace(Invoice.CurrencyCode))
        {
            Invoice.CurrencyCode = currencies.FirstOrDefault(x => x.IsBaseCurrency)?.Code ?? string.Empty;
        }
    }
}
