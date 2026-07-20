using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Accounting.JournalEntries;

[Authorize(Policy = ErpPermissions.Accounting.Default)]
public class IndexModel : AbpPageModel
{
    private readonly JournalEntryAppService _journalEntryAppService;

    public IndexModel(JournalEntryAppService journalEntryAppService)
    {
        _journalEntryAppService = journalEntryAppService;
    }

    public IReadOnlyList<JournalEntryDto> Entries { get; set; } = Array.Empty<JournalEntryDto>();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Accounting.Edit);
        Entries = await _journalEntryAppService.GetListAsync(new GetJournalEntryListInput());
    }
}
