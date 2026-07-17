using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Support.Tickets;

[Authorize(Policy = ErpPermissions.Support.Edit)]
public class EditModel : AbpPageModel
{
    private readonly TicketAppService _ticketAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<CustomerContract, Guid> _contractRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public EditModel(
        TicketAppService ticketAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<CustomerContract, Guid> contractRepository,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _ticketAppService = ticketAppService;
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _jobRepository = jobRepository;
        _contractRepository = contractRepository;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public CreateUpdateTicketDto Ticket { get; set; } = new();

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> OrderOptions { get; set; } = new();
    public List<SelectListItem> JobOptions { get; set; } = new();
    public List<SelectListItem> ContractOptions { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ticket = await _ticketAppService.GetAsync(Id);
        Ticket = new CreateUpdateTicketDto
        {
            CustomerId = ticket.CustomerId,
            OrderId = ticket.OrderId,
            JobId = ticket.JobId,
            ContractId = ticket.ContractId,
            Subject = ticket.Subject,
            Type = ticket.Type,
            Status = ticket.Status,
            Priority = ticket.Priority,
            AssignedToUserId = ticket.AssignedToUserId,
            CustomerSatisfactionRating = ticket.CustomerSatisfactionRating
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

        await _ticketAppService.UpdateAsync(Id, Ticket);
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

        var jobs = await _jobRepository.GetListAsync();
        JobOptions = new List<SelectListItem> { new(L["None"], "") };
        JobOptions.AddRange(
            jobs.OrderByDescending(x => x.ScheduledDate).Select(x => new SelectListItem(
                $"{x.ScheduledDate:d} ({(customerNamesById.TryGetValue(x.CustomerId, out var n) ? n : "")})",
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
