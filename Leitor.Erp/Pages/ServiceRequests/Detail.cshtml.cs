using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Entities.ServiceRequests;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.ServiceRequests;
using Leitor.Erp.Services.Governance;
using Leitor.Erp.Services.ServiceRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.ServiceRequests;

[Authorize(Policy = ErpPermissions.ServiceRequests.Default)]
public class DetailModel : AbpPageModel
{
    private readonly ServiceRequestAppService _serviceRequestAppService;
    private readonly IRepository<DeletionRequest, Guid> _deletionRequestRepository;
    private readonly IFeatureChecker _featureChecker;

    public DetailModel(
        ServiceRequestAppService serviceRequestAppService,
        IRepository<DeletionRequest, Guid> deletionRequestRepository,
        IFeatureChecker featureChecker)
    {
        _serviceRequestAppService = serviceRequestAppService;
        _deletionRequestRepository = deletionRequestRepository;
        _featureChecker = featureChecker;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ServiceRequestDto ServiceRequest { get; set; } = null!;

    public bool CanEdit { get; set; }
    public bool HasPendingDeletionRequest { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _featureChecker.IsEnabledAsync(ErpFeatures.ServiceRequestManagement))
        {
            return NotFound();
        }

        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.ServiceRequests.Edit);
        HasPendingDeletionRequest = await DeletionGate.IsPendingAsync(_deletionRequestRepository, "ServiceRequest", Id);
        ServiceRequest = await _serviceRequestAppService.GetAsync(Id);
        return Page();
    }

    public async Task<IActionResult> OnPostSetStatusAsync(ServiceRequestStatus status)
    {
        var request = await _serviceRequestAppService.GetAsync(Id);
        await _serviceRequestAppService.UpdateAsync(Id, new CreateUpdateServiceRequestDto
        {
            CustomerId = request.CustomerId,
            ServiceCatalogItemId = request.ServiceCatalogItemId,
            Description = request.Description,
            Status = status,
            RequestedDate = request.RequestedDate
        });

        return RedirectToPage(new { id = Id });
    }
}
