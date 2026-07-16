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

namespace Leitor.Erp.Pages.Sales.Quotes;

[Authorize(Policy = ErpPermissions.Sales.Create)]
public class CreateModel : AbpPageModel
{
    private readonly QuoteAppService _quoteAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;

    public CreateModel(
        QuoteAppService quoteAppService,
        IRepository<Customer, Guid> customerRepository)
    {
        _quoteAppService = quoteAppService;
        _customerRepository = customerRepository;
    }

    [BindProperty]
    public CreateUpdateQuoteDto Quote { get; set; } = new()
    {
        IssueDate = DateTime.Today
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

        var quote = await _quoteAppService.CreateAsync(Quote);
        return RedirectToPage("./Detail", new { id = quote.Id });
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
