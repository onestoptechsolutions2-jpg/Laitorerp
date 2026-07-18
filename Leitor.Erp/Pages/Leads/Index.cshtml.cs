using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Leads;

[Authorize(Policy = ErpPermissions.Leads.Default)]
public class IndexModel : AbpPageModel
{
    private readonly LeadAppService _leadAppService;

    public IndexModel(LeadAppService leadAppService)
    {
        _leadAppService = leadAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public LeadStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<LeadDto> Leads { get; set; } = Array.Empty<LeadDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _leadAppService.GetListAsync(new GetLeadListInput
        {
            Filter = Filter,
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Leads = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _leadAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, Status, PageIndex });
    }
}
