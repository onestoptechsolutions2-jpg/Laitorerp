using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Procurement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Timing;
using VendorEntity = Leitor.Erp.Entities.Procurement.Vendor;

namespace Leitor.Erp.Pages.Portal.Vendor;

// Vendor Portal: one login covers both supplier (Purchase Orders) and subcontracted-technician
// (Field Service Jobs) access, since both already reference Vendor. Same repository-direct
// approach as the Client Portal (see Pages/Portal/Client/Index.cshtml.cs) and for the same
// reason - the module AppServices' Procurement.Edit/FieldService.Edit permissions aren't scoped
// by vendor ownership, so granting them to a portal login would let one vendor touch another's
// records. The PortalUserId linkage, checked per-request, is the only authorization here.
[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly IRepository<VendorEntity, Guid> _vendorRepository;
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;
    private readonly IRepository<FieldServiceJob, Guid> _jobRepository;
    private readonly IRepository<FieldServiceJobNote, Guid> _jobNoteRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IClock _clock;

    public IndexModel(
        IRepository<VendorEntity, Guid> vendorRepository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository,
        IRepository<FieldServiceJob, Guid> jobRepository,
        IRepository<FieldServiceJobNote, Guid> jobNoteRepository,
        IRepository<Customer, Guid> customerRepository,
        IClock clock)
    {
        _vendorRepository = vendorRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
        _jobRepository = jobRepository;
        _jobNoteRepository = jobNoteRepository;
        _customerRepository = customerRepository;
        _clock = clock;
    }

    public bool IsLinked { get; set; }
    public VendorEntity Vendor { get; set; } = null!;
    public IReadOnlyList<PurchaseOrder> PurchaseOrders { get; set; } = Array.Empty<PurchaseOrder>();
    public Dictionary<Guid, decimal> PurchaseOrderTotals { get; set; } = new();
    public IReadOnlyList<FieldServiceJob> Jobs { get; set; } = Array.Empty<FieldServiceJob>();
    public Dictionary<Guid, string> JobCustomerNames { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task<VendorEntity?> ResolveLinkedVendorAsync()
    {
        if (!CurrentUser.Id.HasValue)
        {
            return null;
        }

        return (await _vendorRepository.GetListAsync(x => x.PortalUserId == CurrentUser.Id.Value)).FirstOrDefault();
    }

    private async Task LoadAsync()
    {
        var vendor = await ResolveLinkedVendorAsync();
        if (vendor == null)
        {
            IsLinked = false;
            return;
        }

        IsLinked = true;
        Vendor = vendor;

        PurchaseOrders = (await _purchaseOrderRepository.GetListAsync(x => x.VendorId == vendor.Id))
            .OrderByDescending(x => x.OrderDate)
            .ToList();
        var poIds = PurchaseOrders.Select(x => x.Id).ToList();
        var lines = poIds.Count > 0 ? await _purchaseOrderLineRepository.GetListAsync(x => poIds.Contains(x.PurchaseOrderId)) : new List<PurchaseOrderLine>();
        PurchaseOrderTotals = lines
            .GroupBy(x => x.PurchaseOrderId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m)));

        Jobs = (await _jobRepository.GetListAsync(x => x.VendorId == vendor.Id))
            .OrderByDescending(x => x.ScheduledDate)
            .ToList();
        var customerIds = Jobs.Select(x => x.CustomerId).Distinct().ToList();
        JobCustomerNames = customerIds.Count > 0
            ? (await _customerRepository.GetListAsync(x => customerIds.Contains(x.Id))).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();
    }

    public async Task<IActionResult> OnPostConfirmPurchaseOrderAsync(Guid purchaseOrderId)
    {
        var vendor = await ResolveLinkedVendorAsync();
        if (vendor == null)
        {
            return RedirectToPage();
        }

        var purchaseOrder = await _purchaseOrderRepository.FindAsync(purchaseOrderId);
        if (purchaseOrder != null && purchaseOrder.VendorId == vendor.Id && purchaseOrder.Status == PurchaseOrderStatus.Sent)
        {
            purchaseOrder.Status = PurchaseOrderStatus.Confirmed;
            await _purchaseOrderRepository.UpdateAsync(purchaseOrder, autoSave: true);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteJobAsync(Guid jobId)
    {
        var vendor = await ResolveLinkedVendorAsync();
        if (vendor == null)
        {
            return RedirectToPage();
        }

        var job = await _jobRepository.FindAsync(jobId);
        if (job != null && job.VendorId == vendor.Id &&
            job.Status is FieldServiceJobStatus.Scheduled or FieldServiceJobStatus.InProgress)
        {
            job.Status = FieldServiceJobStatus.Completed;
            job.CompletedDate = _clock.Now;
            await _jobRepository.UpdateAsync(job, autoSave: true);
        }

        return RedirectToPage();
    }
}
