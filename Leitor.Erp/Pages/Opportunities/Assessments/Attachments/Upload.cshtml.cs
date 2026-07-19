using System;
using System.IO;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Opportunities.Assessments.Attachments;

// Property is "UploadedFile", not "File" - PageModel already declares a File(...) helper method
// (for returning FileContentResult) that a same-named property would shadow.
[Authorize(Policy = ErpPermissions.Opportunities.Edit)]
public class UploadModel : AbpPageModel
{
    private readonly NeedsAssessmentAttachmentAppService _needsAssessmentAttachmentAppService;

    public UploadModel(NeedsAssessmentAttachmentAppService needsAssessmentAttachmentAppService)
    {
        _needsAssessmentAttachmentAppService = needsAssessmentAttachmentAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid NeedsAssessmentId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid OpportunityId { get; set; }

    [BindProperty]
    public IFormFile? UploadedFile { get; set; }

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

        if (UploadedFile.Length > NeedsAssessmentAttachmentAppService.MaxFileSizeBytes)
        {
            ModelState.AddModelError(nameof(UploadedFile), L["FileTooLarge"]);
            return Page();
        }

        await using var stream = new MemoryStream();
        await UploadedFile.CopyToAsync(stream);

        await _needsAssessmentAttachmentAppService.UploadAsync(new CreateNeedsAssessmentAttachmentDto
        {
            NeedsAssessmentId = NeedsAssessmentId,
            FileName = UploadedFile.FileName,
            ContentType = string.IsNullOrWhiteSpace(UploadedFile.ContentType) ? "application/octet-stream" : UploadedFile.ContentType,
            Content = stream.ToArray()
        });

        return RedirectToPage("/Opportunities/Assessments/Edit", new { id = NeedsAssessmentId, opportunityId = OpportunityId });
    }
}
