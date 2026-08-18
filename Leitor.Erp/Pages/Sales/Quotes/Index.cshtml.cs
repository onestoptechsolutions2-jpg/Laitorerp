using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Sales.Quotes;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class IndexModel : AbpPageModel
{
    private readonly QuoteAppService _quoteAppService;

    public IndexModel(QuoteAppService quoteAppService)
    {
        _quoteAppService = quoteAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public QuoteStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<QuoteDto> Quotes { get; set; } = Array.Empty<QuoteDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _quoteAppService.GetListAsync(new GetQuoteListInput
        {
            Filter = Filter,
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Quotes = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _quoteAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, Status, PageIndex });
    }

    // CSV of whatever the current filter/status is showing, not just the current page - matches
    // the same convention as Leads/Index.cshtml.cs's OnGetExportAsync.
    public async Task<IActionResult> OnGetExportAsync()
    {
        var quotes = new List<QuoteDto>();
        var skip = 0;
        const int batchSize = 1000;
        while (true)
        {
            var batch = await _quoteAppService.GetListAsync(new GetQuoteListInput
            {
                Filter = Filter,
                Status = Status,
                SkipCount = skip,
                MaxResultCount = batchSize
            });

            quotes.AddRange(batch.Items);
            if (batch.Items.Count < batchSize)
            {
                break;
            }

            skip += batchSize;
        }

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", new[]
        {
            "QuoteNumber", "Title", "Customer", "Status", "IssueDate", "ExpiryDate", "Total", "CurrencyCode"
        }.Select(CsvEscape)));

        foreach (var quote in quotes)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                quote.QuoteNumber,
                quote.Title,
                quote.CustomerName,
                quote.Status.ToString(),
                quote.IssueDate.ToString("yyyy-MM-dd"),
                quote.ExpiryDate?.ToString("yyyy-MM-dd"),
                quote.Total.ToString("N2"),
                quote.CurrencyCode
            }.Select(CsvEscape)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"quotes-{Clock.Now:yyyyMMdd-HHmmss}.csv");
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
