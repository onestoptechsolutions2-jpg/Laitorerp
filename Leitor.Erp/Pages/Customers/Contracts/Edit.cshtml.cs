using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers.Contracts;

[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class EditModel : AbpPageModel
{
    private readonly CustomerContractAppService _customerContractAppService;
    private readonly ContractTemplateAppService _contractTemplateAppService;

    public EditModel(CustomerContractAppService customerContractAppService, ContractTemplateAppService contractTemplateAppService)
    {
        _customerContractAppService = customerContractAppService;
        _contractTemplateAppService = contractTemplateAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

    [BindProperty]
    public CreateUpdateCustomerContractDto Contract { get; set; } = new();

    [BindProperty]
    public List<int> SelectedServiceFlags { get; set; } = new();

    public List<SelectListItem> ContractTemplateOptions { get; set; } = new();

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
            SlaLowHours = contract.SlaLowHours,
            ServicesIncluded = contract.ServicesIncluded,
            ContractTemplateId = contract.ContractTemplateId,
            ClientSignatoryName = contract.ClientSignatoryName
        };
        SelectedServiceFlags = ContractServiceScopeOptions.All
            .Where(flag => contract.ServicesIncluded.HasFlag(flag))
            .Select(flag => (int)flag)
            .ToList();

        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Contract.CustomerId = CustomerId;
        Contract.ServicesIncluded = (ContractServiceScope)SelectedServiceFlags.Aggregate(0, (acc, flag) => acc | flag);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        await _customerContractAppService.UpdateAsync(Id, Contract);
        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }

    private async Task LoadOptionsAsync()
    {
        var templates = await _contractTemplateAppService.GetListAsync();
        ContractTemplateOptions = new List<SelectListItem> { new(L["None"], "") };
        ContractTemplateOptions.AddRange(
            templates.Where(x => x.IsActive).Select(x => new SelectListItem(x.Name, x.Id.ToString()))
        );
    }
}
