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

namespace Leitor.Erp.Pages.Sales.Orders;

[Authorize(Policy = ErpPermissions.Sales.Edit)]
public class EditModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;

    public EditModel(
        OrderAppService orderAppService,
        IRepository<Customer, Guid> customerRepository)
    {
        _orderAppService = orderAppService;
        _customerRepository = customerRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateOrderDto Order { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var order = await _orderAppService.GetAsync(Id);
        Order = new CreateUpdateOrderDto
        {
            CustomerId = order.CustomerId,
            QuoteId = order.QuoteId,
            Status = order.Status,
            OrderDate = order.OrderDate,
            Notes = order.Notes
        };

        await LoadCustomerOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomerOptionsAsync();
            return Page();
        }

        await _orderAppService.UpdateAsync(Id, Order);
        return RedirectToPage("./Detail", new { id = Id });
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
