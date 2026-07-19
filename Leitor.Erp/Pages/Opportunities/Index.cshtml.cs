using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Pages.Shared;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Opportunities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Opportunities;

[Authorize(Policy = ErpPermissions.Opportunities.Default)]
public class IndexModel : AbpPageModel
{
    private readonly OpportunityAppService _opportunityAppService;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public IndexModel(
        OpportunityAppService opportunityAppService,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _opportunityAppService = opportunityAppService;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    [BindProperty(SupportsGet = true)]
    public OpportunityStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? AssignedToUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<OpportunityDto> Opportunities { get; set; } = Array.Empty<OpportunityDto>();
    public List<SelectListItem> UserOptions { get; set; } = new();

    public PaginationModel Pagination { get; set; } = new();

    public bool CanCreate { get; set; }
    public bool CanDelete { get; set; }

    public async Task OnGetAsync()
    {
        CanCreate = await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Create);
        CanDelete = await AuthorizationService.IsGrantedAsync(ErpPermissions.Opportunities.Delete);

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }

        var result = await _opportunityAppService.GetListAsync(new GetOpportunityListInput
        {
            Filter = Filter,
            Status = Status,
            AssignedToUserId = AssignedToUserId,
            SkipCount = (PageIndex - 1) * PaginationModel.DefaultPageSize,
            MaxResultCount = PaginationModel.DefaultPageSize
        });

        Opportunities = result.Items;
        Pagination = new PaginationModel { PageIndex = PageIndex, TotalCount = result.TotalCount };

        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _opportunityAppService.DeleteAsync(id);
        return RedirectToPage(new { Filter, Status, AssignedToUserId, PageIndex });
    }
}
