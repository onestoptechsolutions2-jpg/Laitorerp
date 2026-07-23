using System;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Pos.Sales;

[Authorize(Policy = ErpPermissions.Pos.Default)]
public class DetailModel : AbpPageModel
{
    private readonly PosSaleAppService _posSaleAppService;
    private readonly ErpCompanyOptions _companyOptions;

    public DetailModel(PosSaleAppService posSaleAppService, IOptions<ErpCompanyOptions> companyOptions)
    {
        _posSaleAppService = posSaleAppService;
        _companyOptions = companyOptions.Value;
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
        var pdfBytes = PosReceiptPdfDocument.Generate(Sale, _companyOptions);
        return File(pdfBytes, "application/pdf", $"{Sale.SaleNumber}.pdf");
    }
}
