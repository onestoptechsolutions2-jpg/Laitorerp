using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Accounting.ExchangeRates;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly ExchangeRateAppService _exchangeRateAppService;
    private readonly IRepository<Currency, Guid> _currencyRepository;

    public IndexModel(ExchangeRateAppService exchangeRateAppService, IRepository<Currency, Guid> currencyRepository)
    {
        _exchangeRateAppService = exchangeRateAppService;
        _currencyRepository = currencyRepository;
    }

    public IReadOnlyList<ExchangeRateDto> ExchangeRates { get; set; } = Array.Empty<ExchangeRateDto>();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

    [BindProperty]
    public CreateUpdateExchangeRateDto NewRate { get; set; } = new()
    {
        RateDate = DateTime.Today
    };

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Accounting.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var result = await _exchangeRateAppService.GetListAsync(new GetExchangeRateListInput
        {
            MaxResultCount = 1000,
            Sorting = "RateDate desc"
        });
        ExchangeRates = result.Items;

        var currencies = await _currencyRepository.GetListAsync(x => x.IsActive && !x.IsBaseCurrency);
        CurrencyOptions = currencies
            .OrderBy(x => x.Code)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Code))
            .ToList();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        await _exchangeRateAppService.CreateAsync(NewRate);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _exchangeRateAppService.DeleteAsync(id);
        return RedirectToPage();
    }
}
