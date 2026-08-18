using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Partners;
using Leitor.Erp.Services.Partners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Agents;

[Authorize(Policy = ErpPermissions.Partners.Default)]
public class IndexModel : AbpPageModel
{
    private readonly AgentAppService _agentAppService;

    public IndexModel(AgentAppService agentAppService)
    {
        _agentAppService = agentAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<AgentDto> Items { get; set; } = Array.Empty<AgentDto>();
    public PaginationModel Pagination { get; set; } = new();
    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Partners.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Partners.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _agentAppService.GetListAsync(new GetAgentListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Items = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _agentAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }
}
