using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
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

    public IReadOnlyList<FieldServiceJobDto> Jobs { get; set; } = Array.Empty<FieldServiceJobDto>();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Delete);

        var result = await _fieldServiceJobAppService.GetListAsync(new GetFieldServiceJobListInput
        {
            Filter = Filter,
            Status = Status,
            MaxResultCount = 1000
        });

        Jobs = result.Items;
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _fieldServiceJobAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, Status });
    }
}
