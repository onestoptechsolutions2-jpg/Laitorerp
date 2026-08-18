using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Customers;

[Authorize(Policy = ErpPermissions.Customers.Default)]
public class IndexModel : AbpPageModel
{
    private readonly CustomerAppService _customerAppService;

    public IndexModel(CustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<CustomerDto> Customers { get; set; } = Array.Empty<CustomerDto>();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Create);
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Edit);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _customerAppService.GetListAsync(new GetCustomerListInput
        {
            Filter = Filter,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Customers = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _customerAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, PageIndex });
    }

    // CSV of whatever the current filter is showing, not just the current page - matches the
    // same convention as Leads/Index.cshtml.cs's OnGetExportAsync.
    public async Task<IActionResult> OnGetExportAsync()
    {
        var customers = new List<CustomerDto>();
        var skip = 0;
        const int batchSize = 1000;
        while (true)
        {
            var batch = await _customerAppService.GetListAsync(new GetCustomerListInput
            {
                Filter = Filter,
                SkipCount = skip,
                MaxResultCount = batchSize
            });

            customers.AddRange(batch.Items);
            if (batch.Items.Count < batchSize)
            {
                break;
            }

            skip += batchSize;
        }

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", new[]
        {
            "Name", "Email", "PhoneNumber", "City", "Country", "Status", "AccountOwner", "Created"
        }.Select(CsvEscape)));

        foreach (var customer in customers)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                customer.Name,
                customer.Email,
                customer.PhoneNumber,
                customer.City,
                customer.Country,
                customer.Status.ToString(),
                customer.AccountOwnerUserName,
                customer.CreationTime.ToString("yyyy-MM-dd")
            }.Select(CsvEscape)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"customers-{Clock.Now:yyyyMMdd-HHmmss}.csv");
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
