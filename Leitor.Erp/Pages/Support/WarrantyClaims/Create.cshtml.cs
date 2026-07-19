using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Support.WarrantyClaims;

[Authorize(Policy = ErpPermissions.Support.Create)]
public class CreateModel : AbpPageModel
{
    private readonly WarrantyClaimAppService _warrantyClaimAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<CustomerContract, Guid> _contractRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;

    public CreateModel(
        WarrantyClaimAppService warrantyClaimAppService,
        IRepository<Customer, Guid> customerRepository,
        IRepository<CustomerContract, Guid> contractRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<Ticket, Guid> ticketRepository)
    {
        _warrantyClaimAppService = warrantyClaimAppService;
        _customerRepository = customerRepository;
        _contractRepository = contractRepository;
        _jobRepository = jobRepository;
        _ticketRepository = ticketRepository;
    }

    [BindProperty]
    public CreateUpdateWarrantyClaimDto WarrantyClaim { get; set; } = new()
    {
        FiledDate = DateTime.Today
    };

    // Set when navigating here from Customer Detail's "New Warranty Claim" link.
    [BindProperty(SupportsGet = true)]
    public Guid? CustomerId { get; set; }

    public List<SelectListItem> CustomerOptions { get; set; } = new();
    public List<SelectListItem> ContractOptions { get; set; } = new();
    public List<SelectListItem> JobOptions { get; set; } = new();
    public List<SelectListItem> TicketOptions { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (CustomerId.HasValue)
        {
            WarrantyClaim.CustomerId = CustomerId.Value;
        }

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        var claim = await _warrantyClaimAppService.CreateAsync(WarrantyClaim);
        return RedirectToPage("./Detail", new { id = claim.Id });
    }

    private async Task LoadOptionsAsync()
    {
        var customers = await _customerRepository.GetListAsync();
        var customerNamesById = customers.ToDictionary(x => x.Id, x => x.Name);
        CustomerOptions = customers
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString()))
            .ToList();

        var contracts = await _contractRepository.GetListAsync();
        ContractOptions = new List<SelectListItem> { new(L["None"], "") };
        ContractOptions.AddRange(
            contracts.OrderBy(x => x.ContractNumber).Select(x => new SelectListItem(
                $"{x.ContractNumber} ({(customerNamesById.TryGetValue(x.CustomerId, out var n) ? n : "")})",
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

        var tickets = await _ticketRepository.GetListAsync();
        TicketOptions = new List<SelectListItem> { new(L["None"], "") };
        TicketOptions.AddRange(
            tickets.OrderByDescending(x => x.CreationTime).Select(x => new SelectListItem(
                $"{x.TicketNumber} - {x.Subject}",
                x.Id.ToString()
            ))
        );
    }
}
