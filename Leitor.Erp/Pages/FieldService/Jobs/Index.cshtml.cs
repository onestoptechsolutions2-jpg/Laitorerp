using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.FieldService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.FieldService.Jobs;

[Authorize(Policy = ErpPermissions.FieldService.Default)]
public class IndexModel : AbpPageModel
{
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;

    public IndexModel(FieldServiceJobAppService fieldServiceJobAppService)
    {
        _fieldServiceJobAppService = fieldServiceJobAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public FieldServiceJobStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<FieldServiceJobDto> Jobs { get; set; } = Array.Empty<FieldServiceJobDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _fieldServiceJobAppService.GetListAsync(new GetFieldServiceJobListInput
        {
            Filter = Filter,
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Jobs = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _fieldServiceJobAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, Status, PageIndex });
    }
}
