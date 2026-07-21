using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers.Contracts;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class EditModel : AbpPageModel
{
    private readonly CustomerContractAppService _customerContractAppService;

    public EditModel(CustomerContractAppService customerContractAppService)
    {
        _customerContractAppService = customerContractAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

    [BindProperty]
    public CreateUpdateCustomerContractDto Contract { get; set; } = new();

    public async Task OnGetAsync()
    {
        var contract = await _customerContractAppService.GetAsync(Id);
        Contract = new CreateUpdateCustomerContractDto
        {
            CustomerId = contract.CustomerId,
            ContractNumber = contract.ContractNumber,
            Title = contract.Title,
            Type = contract.Type,
            Status = contract.Status,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Value = contract.Value,
            Notes = contract.Notes,
            SlaUrgentHours = contract.SlaUrgentHours,
            SlaHighHours = contract.SlaHighHours,
            SlaMediumHours = contract.SlaMediumHours,
            SlaLowHours = contract.SlaLowHours
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Contract.CustomerId = CustomerId;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _customerContractAppService.UpdateAsync(Id, Contract);
        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }
}
