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

namespace Leitor.Erp.Pages.Sales.Quotes;

[Authorize(Policy = ErpPermissions.Sales.Edit)]
public class EditModel : AbpPageModel
{
    private readonly QuoteAppService _quoteAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public EditModel(
        QuoteAppService quoteAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Currency, Guid> currencyRepository)
    {
        _quoteAppService = quoteAppService;
        _customerRepository = customerRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateQuoteDto Quote { get; set; } = new();

    [BindProperty]
    public string UnlockReason { get; set; } = string.Empty;

    public QuoteDto QuoteDetails { get; set; } = null!;
    public bool CanUnlock { get; set; }
    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        CanUnlock = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Unlock);
        QuoteDetails = await _quoteAppService.GetAsync(Id);
        Quote = new CreateUpdateQuoteDto
        {
            CustomerId = QuoteDetails.CustomerId,
            Title = QuoteDetails.Title,
            Status = QuoteDetails.Status,
            IssueDate = QuoteDetails.IssueDate,
            ExpiryDate = QuoteDetails.ExpiryDate,
            Notes = QuoteDetails.Notes,
            ProposalId = QuoteDetails.ProposalId,
            CurrencyCode = QuoteDetails.CurrencyCode
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

        await _quoteAppService.UpdateAsync(Id, Quote);
        return RedirectToPage("./Detail", new { id = Id });
    }

    public async Task<IActionResult> OnPostUnlockAsync()
    {
        await _quoteAppService.UnlockForRevisionAsync(Id, UnlockReason);
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
