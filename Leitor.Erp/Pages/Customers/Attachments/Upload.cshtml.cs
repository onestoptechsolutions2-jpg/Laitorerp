using System;
using System.IO;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers.Attachments;

// Property is "UploadedFile", not "File" - PageModel already declares a File(...) helper method
// (for returning FileContentResult) that a same-named property would shadow.
[Authorize(Policy = ErpPermissions.Customers.Edit)]
public class UploadModel : AbpPageModel
{
    private readonly CustomerAttachmentAppService _customerAttachmentAppService;

    public UploadModel(CustomerAttachmentAppService customerAttachmentAppService)
    {
        _customerAttachmentAppService = customerAttachmentAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid CustomerId { get; set; }

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

        if (UploadedFile.Length > CustomerAttachmentAppService.MaxFileSizeBytes)
        {
            ModelState.AddModelError(nameof(UploadedFile), L["FileTooLarge"]);
            return Page();
        }

        await using var stream = new MemoryStream();
        await UploadedFile.CopyToAsync(stream);

        await _customerAttachmentAppService.UploadAsync(new CreateCustomerAttachmentDto
        {
            CustomerId = CustomerId,
            FileName = UploadedFile.FileName,
            ContentType = string.IsNullOrWhiteSpace(UploadedFile.ContentType) ? "application/octet-stream" : UploadedFile.ContentType,
            Content = stream.ToArray()
        });

        return RedirectToPage("/Customers/Detail", new { id = CustomerId });
    }
}
