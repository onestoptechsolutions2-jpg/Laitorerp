using System;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Leitor.Erp.Settings;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Pos.Sales;

[Authorize(Policy = ErpPermissions.Pos.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PosSaleAppService _posSaleAppService;
    private readonly ErpCompanyProfileProvider _companyProfileProvider;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(PosSaleAppService posSaleAppService, ErpCompanyProfileProvider companyProfileProvider, IFeatureChecker featureChecker)
    {
        _posSaleAppService = posSaleAppService;
        _companyProfileProvider = companyProfileProvider;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PosSaleDto Sale { get; set; } = null!;
    public bool CanVoid { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.PointOfSale))
        {
            return NotFound();
        }

        CanVoid = await AuthorizationService.IsGrantedAsync(ErpPermissions.Pos.Void);
        Sale = await _posSaleAppService.GetAsync(Id);
        return Page();
    }

    public async Task<IActionResult> OnPostVoidAsync()
    {
        await _posSaleAppService.VoidAsync(Id);
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnGetPdfAsync()
    {
        Sale = await _posSaleAppService.GetAsync(Id);
        var companyOptions = await _companyProfileProvider.GetAsync();
        var pdfBytes = PosReceiptPdfDocument.Generate(Sale, companyOptions);
        return File(pdfBytes, "application/pdf", $"{Sale.SaleNumber}.pdf");
    }
}
