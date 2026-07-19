using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
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

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class EditModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<PriceList, Guid> _priceListRepository;

    public EditModel(
        CustomerAppService customerAppService,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<PriceList, Guid> priceListRepository)
    {
        _customerAppService = customerAppService;
        _identityUserRepository = identityUserRepository;
        _priceListRepository = priceListRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateCustomerDto Customer { get; set; } = new();

    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> PriceListOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var customer = await _customerAppService.GetAsync(Id);
        Customer = new CreateUpdateCustomerDto
        {
            Name = customer.Name,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            AddressLine = customer.AddressLine,
            City = customer.City,
            State = customer.State,
            PostalCode = customer.PostalCode,
            Country = customer.Country,
            Status = customer.Status,
            Notes = customer.Notes,
            AccountOwnerUserId = customer.AccountOwnerUserId,
            PortalUserId = customer.PortalUserId,
            DefaultPaymentTerms = customer.DefaultPaymentTerms,
            DefaultPriceListId = customer.DefaultPriceListId
        };

        await LoadUserOptionsAsync();
        await LoadPriceListOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadUserOptionsAsync();
            await LoadPriceListOptionsAsync();
            return Page();
        }

        await _customerAppService.UpdateAsync(Id, Customer);
        return RedirectToPage("./Detail", new { id = Id });
    }

    private async Task LoadUserOptionsAsync()
    {
        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }

    private async Task LoadPriceListOptionsAsync()
    {
        var priceLists = await _priceListRepository.GetListAsync();
        PriceListOptions = new List<SelectListItem> { new(L["None"], "") };
        PriceListOptions.AddRange(
            priceLists.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString()))
        );
    }
}
