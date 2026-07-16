using System;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers.Attachments;

[Authorize(Policy = ErpPermissions.Customers.Default)]
public class DownloadModel : AbpPageModel
{
    private readonly CustomerAttachmentAppService _customerAttachmentAppService;

    public DownloadModel(CustomerAttachmentAppService customerAttachmentAppService)
    {
        _customerAttachmentAppService = customerAttachmentAppService;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var content = await _customerAttachmentAppService.GetContentAsync(id);
        return File(content.Content, content.ContentType, content.FileName);
    }
}
