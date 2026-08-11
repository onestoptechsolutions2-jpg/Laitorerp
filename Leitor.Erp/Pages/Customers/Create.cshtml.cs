using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Customers;

[Authorize(Policy = ErpPermissions.Customers.Create)]
public class CreateModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;
    private readonly IRepository<IdentityUser, System.Guid> _identityUserRepository;
    private readonly IRepository<Currency, System.Guid> _currencyRepository;

    public CreateModel(
        CustomerAppService customerAppService,
        IRepository<IdentityUser, System.Guid> identityUserRepository,
        IRepository<Currency, System.Guid> currencyRepository)
    {
        _customerAppService = customerAppService;
        _identityUserRepository = identityUserRepository;
        _currencyRepository = currencyRepository;
    }

    [BindProperty]
    public CreateUpdateCustomerDto Customer { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> CurrencyOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadUserOptionsAsync();
        await LoadCurrencyOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserOptionsAsync();
            await LoadCurrencyOptionsAsync();
            return Page();
        }

        var customer = await _customerAppService.CreateAsync(Customer);

        if (OverlayRequest.Is(Request))
        {
            return new JsonResult(new { redirectUrl = Url.Page("./Detail", new { id = customer.Id }) });
        }

        return RedirectToPage("./Detail", new { id = customer.Id });
    }

    private async Task LoadUserOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }

    private async Task LoadCurrencyOptionsAsync()
    {
        var currencies = await _currencyRepository.GetListAsync(x => x.IsActive);
        CurrencyOptions = new List<SelectListItem> { new(L["None"], "") };
        CurrencyOptions.AddRange(
            currencies.OrderBy(x => x.Code).Select(x => new SelectListItem(x.Code, x.Code))
        );
    }
}
