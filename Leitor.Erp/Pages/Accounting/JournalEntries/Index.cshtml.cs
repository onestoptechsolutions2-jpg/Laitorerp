using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Accounting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<JournalEntryDto> Entries { get; set; } = Array.Empty<JournalEntryDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Accounting.Edit);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _journalEntryAppService.GetListAsync(new GetJournalEntryListInput
        {
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Entries = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }
}
