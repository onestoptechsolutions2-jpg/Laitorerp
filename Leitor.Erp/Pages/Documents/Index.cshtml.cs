using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Documents;
using Leitor.Erp.Services.Documents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Documents;

// Public, unauthenticated "smart document link" landing page - what a customer/vendor actually
// lands on from a shared WhatsApp/email link, instead of a raw PDF attachment. No [Authorize]
// attribute is needed: this app has no global fallback authentication policy (confirmed - every
// other page opts INTO auth via its own [Authorize], nothing opts OUT of a default-on one), so
// this page is public simply by not declaring one, same as Pages/Account/Login.
public class IndexModel : PageModel
{
    private readonly IRepository<DocumentShareLink, Guid> _shareLinkRepository;
    private readonly PublicDocumentResolver _resolver;

    public IndexModel(IRepository<DocumentShareLink, Guid> shareLinkRepository, PublicDocumentResolver resolver)
    {
        _shareLinkRepository = shareLinkRepository;
        _resolver = resolver;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Token { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool LinkNotFound { get; set; }

    public async Task OnGetAsync()
    {
        var link = await _shareLinkRepository.FindAsync(Token);
        if (link == null)
        {
            LinkNotFound = true;
            return;
        }

        var result = await _resolver.ResolveAsync(link.DocumentType, link.EntityId);
        if (result == null)
        {
            LinkNotFound = true;
            return;
        }

        Title = result.Title;
        Subtitle = result.Subtitle;
        CompanyName = result.CompanyName;
    }

    public async Task<IActionResult> OnGetDownloadAsync()
    {
        var link = await _shareLinkRepository.FindAsync(Token);
        if (link == null)
        {
            return NotFound();
        }

        var result = await _resolver.ResolveAsync(link.DocumentType, link.EntityId);
        if (result == null)
        {
            return NotFound();
        }

        return File(result.PdfBytes, "application/pdf", result.FileName);
    }
}
