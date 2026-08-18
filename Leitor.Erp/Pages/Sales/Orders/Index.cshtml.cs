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

namespace Leitor.Erp.Pages.Sales.Orders;

[Authorize(Policy = ErpPermissions.Sales.Default)]
public class IndexModel : AbpPageModel
{
    private readonly OrderAppService _orderAppService;

    public IndexModel(OrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public OrderStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<OrderDto> Orders { get; set; } = Array.Empty<OrderDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _orderAppService.GetListAsync(new GetOrderListInput
        {
            Filter = Filter,
            Status = Status,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Orders = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _orderAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, Status, PageIndex });
    }

    // CSV of whatever the current filter/status is showing, not just the current page - matches
    // the same convention as Leads/Index.cshtml.cs's OnGetExportAsync.
    public async Task<IActionResult> OnGetExportAsync()
    {
        var orders = new List<OrderDto>();
        var skip = 0;
        const int batchSize = 1000;
        while (true)
        {
            var batch = await _orderAppService.GetListAsync(new GetOrderListInput
            {
                Filter = Filter,
                Status = Status,
                SkipCount = skip,
                MaxResultCount = batchSize
            });

            orders.AddRange(batch.Items);
            if (batch.Items.Count < batchSize)
            {
                break;
            }

            skip += batchSize;
        }

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", new[]
        {
            "OrderNumber", "Customer", "Status", "OrderDate", "Total", "CurrencyCode"
        }.Select(CsvEscape)));

        foreach (var order in orders)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                order.OrderNumber,
                order.CustomerName,
                order.Status.ToString(),
                order.OrderDate.ToString("yyyy-MM-dd"),
                order.Total.ToString("N2"),
                order.CurrencyCode
            }.Select(CsvEscape)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"orders-{Clock.Now:yyyyMMdd-HHmmss}.csv");
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
