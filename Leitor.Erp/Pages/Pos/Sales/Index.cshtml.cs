using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Pos.Sales;

[Authorize(Policy = ErpPermissions.Pos.Default)]
public class IndexModel : AbpPageModel
{
    private readonly PosSaleAppService _posSaleAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(PosSaleAppService posSaleAppService, IFeatureChecker featureChecker)
    {
        _posSaleAppService = posSaleAppService;
        _featureChecker = featureChecker;
    }

    public List<PosSaleDto> Sales { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.PointOfSale))
        {
            return NotFound();
        }

        Sales = await _posSaleAppService.GetRecentAsync();
        return Page();
    }
}
