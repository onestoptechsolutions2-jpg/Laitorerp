using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Leads;

// Property is "UploadedFile", not "File" - same reason as Customers/Attachments/Upload.cshtml.cs:
// PageModel already exposes a File(...) helper that a same-named property would shadow.
[Authorize(Policy = ErpPermissions.Leads.Import)]
public class ImportModel : AbpPageModel
{
    private readonly LeadImportAppService _leadImportAppService;

    public ImportModel(LeadImportAppService leadImportAppService)
    {
        _leadImportAppService = leadImportAppService;
    }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UploadedFile == null || UploadedFile.Length == 0)
        {
            ModelState.AddModelError(nameof(UploadedFile), L["File"]);
            return Page();
        }

        if (!UploadedFile.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(UploadedFile), L["ImportLeadsFileTypeError"]);
            return Page();
        }

        if (UploadedFile.Length > LeadImportAppService.MaxFileSizeBytes)
        {
            ModelState.AddModelError(nameof(UploadedFile), L["ImportLeadsFileTooLarge"]);
            return Page();
        }

        await using var stream = new MemoryStream();
        await UploadedFile.CopyToAsync(stream);

        try
        {
            var result = await _leadImportAppService.ImportAsync(stream.ToArray());

            var summary = new StringBuilder();
            summary.Append($"Imported {result.ImportedCount} of {result.TotalRows} row(s).");
            summary.Append($" Skipped: {result.SkippedDuplicateTicket} already imported, {result.SkippedDuplicatePhone} duplicate phone, {result.SkippedInvalidRow} invalid.");
            summary.Append($" New agents created: {result.NewAgentsCreated}.");
            if (result.RowErrors.Count > 0)
            {
                summary.Append('\n').Append(string.Join('\n', result.RowErrors));
            }

            SuccessMessage = summary.ToString();
        }
        catch (UserFriendlyException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
