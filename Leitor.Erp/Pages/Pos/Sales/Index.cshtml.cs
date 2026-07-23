using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Pos.Sales;

[Authorize(Policy = ErpPermissions.Pos.Default)]
public class IndexModel : AbpPageModel
{
    private readonly PosSaleAppService _posSaleAppService;

    public IndexModel(PosSaleAppService posSaleAppService)
    {
        _posSaleAppService = posSaleAppService;
    }

    public List<PosSaleDto> Sales { get; set; } = new();

    public async Task OnGetAsync()
    {
        Sales = await _posSaleAppService.GetRecentAsync();
    }
}
