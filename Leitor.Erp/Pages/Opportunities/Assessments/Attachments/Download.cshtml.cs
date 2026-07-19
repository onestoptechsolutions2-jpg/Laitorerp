using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Opportunities.Assessments.Attachments;

[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class DownloadModel : AbpPageModel
{
    private readonly NeedsAssessmentAttachmentAppService _needsAssessmentAttachmentAppService;

    public DownloadModel(NeedsAssessmentAttachmentAppService needsAssessmentAttachmentAppService)
    {
        _needsAssessmentAttachmentAppService = needsAssessmentAttachmentAppService;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var content = await _needsAssessmentAttachmentAppService.GetContentAsync(id);
        return File(content.Content, content.ContentType, content.FileName);
    }
}
