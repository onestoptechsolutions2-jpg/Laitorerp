using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
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

[Authorize(Policy = ErpPermissions.FieldService.Edit)]
public class EditModel : AbpPageModel
{
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<CustomerContract, Guid> _contractRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public EditModel(
        FieldServiceJobAppService fieldServiceJobAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<CustomerContract, Guid> contractRepository,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _contractRepository = contractRepository;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateFieldServiceJobDto Job { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> OrderOptions { get; set; } = new();
    public List<SelectListItem> ContractOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var job = await _fieldServiceJobAppService.GetAsync(Id);
        Job = new CreateUpdateFieldServiceJobDto
        {
            CustomerId = job.CustomerId,
            OrderId = job.OrderId,
            ContractId = job.ContractId,
            Type = job.Type,
            Status = job.Status,
            ScheduledDate = job.ScheduledDate,
            AssignedToUserId = job.AssignedToUserId,
            SiteAddress = job.SiteAddress,
            Description = job.Description
        };

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _fieldServiceJobAppService.UpdateAsync(Id, Job);
        return RedirectToPage("./Detail", new { id = Id });
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
    }
}
