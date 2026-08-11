using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Leads;

[Authorize(Policy = ErpPermissions.Leads.Default)]
public class IndexModel : AbpPageModel
{
    private readonly LeadAppService _leadAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public IndexModel(
        LeadAppService leadAppService,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _leadAppService = leadAppService;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public LeadStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? AssignedToUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<LeadDto> Leads { get; set; } = Array.Empty<LeadDto>();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanDecideDeletions { get; set; }
    public bool CanImport { get; set; }
    public bool CanExport { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Delete);
        CanDecideDeletions = await AuthorizationService.IsGrantedAsync(ErpPermissions.DeletionApprovals.Decide);
        CanImport = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Import);
        CanExport = await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Export);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _leadAppService.GetListAsync(new GetLeadListInput
        {
            Filter = Filter,
            Status = Status,
            AssignedToUserId = AssignedToUserId,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Leads = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };

        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _leadAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, Status, AssignedToUserId, PageIndex });
    }

    // CSV of whatever the current filter/search is showing, not just the current page - matches
    // the confirmed requirement of "export the current filtered list", not a paged slice of it.
    public async Task<IActionResult> OnGetExportAsync()
    {
        if (!await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Export))
        {
            return Forbid();
        }

        var result = await _leadAppService.GetListAsync(new GetLeadListInput
        {
            Filter = Filter,
            Status = Status,
            AssignedToUserId = AssignedToUserId,
            SkipCount = 0,
            MaxResultCount = 10000
        });

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", new[]
        {
            "Name", "CompanyName", "Email", "Phone", "Territory", "Cluster", "Location", "Estate",
            "AccountNumber", "Source", "Status", "AssignedTo", "ReferrerAgent", "Created"
        }.Select(CsvEscape)));

        foreach (var lead in result.Items)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                lead.Name,
                lead.CompanyName,
                lead.Email,
                lead.Phone,
                lead.Territory,
                lead.Cluster,
                lead.Location,
                lead.Estate,
                lead.ExternalAccountNumber,
                lead.Source.ToString(),
                lead.Status.ToString(),
                lead.AssignedToUserName,
                lead.ReferrerAgentName,
                lead.CreationTime.ToString("yyyy-MM-dd")
            }.Select(CsvEscape)));
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"leads-{Clock.Now:yyyyMMdd-HHmmss}.csv");
    }

    private static string CsvEscape(string? value)
    {
        value ??= string.Empty;
        return value.IndexOfAny(new[] { ',', '"', '\n' }) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
