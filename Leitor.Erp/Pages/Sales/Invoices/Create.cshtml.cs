using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public CreateModel(
        InvoiceAppService invoiceAppService,
        IRepository<Customer, Guid> customerRepository)
    {
        _invoiceAppService = invoiceAppService;
        _customerRepository = customerRepository;
    }

    [BindProperty]
    public CreateUpdateInvoiceDto Invoice { get; set; } = new()
    {
        IssueDate = DateTime.Today,
        DueDate = DateTime.Today.AddDays(30)
    };

    public List<SelectListItem> CustomerOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadCustomerOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomerOptionsAsync();
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
}
