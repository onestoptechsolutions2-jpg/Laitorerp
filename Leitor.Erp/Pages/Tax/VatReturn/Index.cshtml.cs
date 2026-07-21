using System;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Tax;
using Leitor.Erp.Services.Tax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Tax.VatReturn;

[Authorize(Policy = ErpPermissions.TaxCompliance.Default)]
public class IndexModel : AbpPageModel
{
    private readonly VatReturnAppService _vatReturnAppService;
    private readonly IFeatureChecker _featureChecker;

    public IndexModel(VatReturnAppService vatReturnAppService, IFeatureChecker featureChecker)
    {
        _vatReturnAppService = vatReturnAppService;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime FromDate { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime ToDate { get; set; }

    public VatReturnDto Result { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.TaxCompliance))
        {
            return NotFound();
        }

        if (FromDate == default)
        {
            FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        }

        if (ToDate == default)
        {
            ToDate = FromDate.AddMonths(1).AddDays(-1);
        }

        Result = await _vatReturnAppService.GetVatReturnAsync(FromDate, ToDate);
        return Page();
    }
}
