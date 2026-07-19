using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.FieldService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.FieldService.Jobs;

[Authorize(Policy = ErpPermissions.FieldService.Create)]
public class CreateModel : AbpPageModel
{
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<CustomerContract, Guid> _contractRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;

    public CreateModel(
        FieldServiceJobAppService fieldServiceJobAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<CustomerContract, Guid> contractRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<Vendor, Guid> vendorRepository)
    {
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _contractRepository = contractRepository;
        _identityUserRepository = identityUserRepository;
        _vendorRepository = vendorRepository;
    }

    [BindProperty]
    public CreateUpdateFieldServiceJobDto Job { get; set; } = new()
    {
        ScheduledDate = DateTime.Today
    };

    // Set when navigating here from an Order's "Schedule Installation" link
    // (Pages/Sales/Orders/Detail.cshtml) - prefills the Order/Customer below instead of the
    // dropdowns arriving blank.
    [BindProperty(SupportsGet = true)]
    public Guid? OrderId { get; set; }

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> OrderOptions { get; set; } = new();
    public List<SelectListItem> ContractOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();
    public List<SelectListItem> VendorOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();

        if (OrderId.HasValue)
        {
            var order = await _orderRepository.GetAsync(OrderId.Value);
            Job.CustomerId = order.CustomerId;
            Job.OrderId = OrderId.Value;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var job = await _fieldServiceJobAppService.CreateAsync(Job);
        return RedirectToPage("./Detail", new { id = job.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        var customerNamesById = customers.ToDictionary(x => x.Id, x => x.Name);
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var orders = await _orderRepository.GetListAsync();
        OrderOptions = new List<SelectListItem> { new(L["None"], "") };
        OrderOptions.AddRange(
            orders.OrderByDescending(x => x.OrderDate).Select(x => new SelectListItem(
                $"{x.OrderNumber} ({(customerNamesById.TryGetValue(x.CustomerId, out var n) ? n : "")})",
                x.Id.ToString()
            ))
        );

        var contracts = await _contractRepository.GetListAsync();
        ContractOptions = new List<SelectListItem> { new(L["None"], "") };
        ContractOptions.AddRange(
            contracts.OrderBy(x => x.ContractNumber).Select(x => new SelectListItem(
                $"{x.ContractNumber} ({(customerNamesById.TryGetValue(x.CustomerId, out var n) ? n : "")})",
                x.Id.ToString()
            ))
        );

        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );

        var vendors = await _vendorRepository.GetListAsync();
        VendorOptions = new List<SelectListItem> { new(L["None"], "") };
        VendorOptions.AddRange(
            vendors.OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString()))
        );
    }
}
