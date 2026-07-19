using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Pages.Support.WarrantyClaims;

[Authorize(Policy = ErpPermissions.Support.Default)]
public class DetailModel : AbpPageModel
{
    private readonly WarrantyClaimAppService _warrantyClaimAppService;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;

    public DetailModel(
        WarrantyClaimAppService warrantyClaimAppService,
        IRepository<DeletionRequest, Guid> deletionRequestRepository)
    {
        _warrantyClaimAppService = warrantyClaimAppService;
        _deletionRequestRepository = deletionRequestRepository;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public WarrantyClaimDto WarrantyClaim { get; set; } = null!;

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "WarrantyClaim", Id);
        WarrantyClaim = await _warrantyClaimAppService.GetAsync(Id);
    }

    public async Task<IActionResult> OnPostSetStatusAsync(WarrantyClaimStatus status)
    {
        var claim = await _warrantyClaimAppService.GetAsync(Id);
        await _warrantyClaimAppService.UpdateAsync(Id, new CreateUpdateWarrantyClaimDto
        {
            CustomerId = claim.CustomerId,
            ContractId = claim.ContractId,
            JobId = claim.JobId,
            TicketId = claim.TicketId,
            Description = claim.Description,
            Status = status,
            FiledDate = claim.FiledDate
        });

        return RedirectToPage(new { id = Id });
    }
}
