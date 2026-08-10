using System;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Leitor.Erp.Settings;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Pos.Sales;

[Authorize(Policy = ErpPermissions.Pos.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PosSaleAppService _posSaleAppService;
    private readonly ErpCompanyProfileProvider _companyProfileProvider;

    public DetailModel(PosSaleAppService posSaleAppService, ErpCompanyProfileProvider companyProfileProvider)
    {
        _posSaleAppService = posSaleAppService;
        _companyProfileProvider = companyProfileProvider;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public PosSaleDto Sale { get; set; } = null!;
    public bool CanVoid { get; set; }

    public async Task OnGetAsync()
    {
        CanVoid = await AuthorizationService.IsGrantedAsync(ErpPermissions.Pos.Void);
        Sale = await _posSaleAppService.GetAsync(Id);
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
