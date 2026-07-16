using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.FieldService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.FieldService.Jobs;

[Authorize(Policy = ErpPermissions.FieldService.Default)]
public class DetailModel : AbpPageModel
{
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly FieldServiceJobNoteAppService _fieldServiceJobNoteAppService;
    private readonly FieldServiceJobPartAppService _fieldServiceJobPartAppService;

    public DetailModel(
        FieldServiceJobAppService fieldServiceJobAppService,
        FieldServiceJobNoteAppService fieldServiceJobNoteAppService,
        FieldServiceJobPartAppService fieldServiceJobPartAppService)
    {
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _fieldServiceJobNoteAppService = fieldServiceJobNoteAppService;
        _fieldServiceJobPartAppService = fieldServiceJobPartAppService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public FieldServiceJobDto Job { get; set; } = null!;
    public IReadOnlyList<FieldServiceJobNoteDto> Notes { get; set; } = Array.Empty<FieldServiceJobNoteDto>();
    public IReadOnlyList<FieldServiceJobPartDto> Parts { get; set; } = Array.Empty<FieldServiceJobPartDto>();

    [BindProperty]
    public CreateFieldServiceJobNoteDto NewNote { get; set; } = new();

    [BindProperty]
    public CreateUpdateFieldServiceJobPartDto NewPart { get; set; } = new()
    {
        Quantity = 1
    };

    public bool CanEdit { get; set; }

    public async Task OnGetAsync()
    {
        CanEdit = await AuthorizationService.IsGrantedAsync(ErpPermissions.FieldService.Edit);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Job = await _fieldServiceJobAppService.GetAsync(Id);

        var notes = await _fieldServiceJobNoteAppService.GetListAsync(new GetFieldServiceJobNoteListInput
        {
            JobId = Id,
            MaxResultCount = 1000
        });
        Notes = notes.Items;

        var parts = await _fieldServiceJobPartAppService.GetListAsync(new GetFieldServiceJobPartListInput
        {
            JobId = Id,
            MaxResultCount = 1000
        });
        Parts = parts.Items;
    }

    public async Task<IActionResult> OnPostSetStatusAsync(FieldServiceJobStatus status)
    {
        var job = await _fieldServiceJobAppService.GetAsync(Id);
        await _fieldServiceJobAppService.UpdateAsync(Id, new CreateUpdateFieldServiceJobDto
        {
            CustomerId = job.CustomerId,
            OrderId = job.OrderId,
            ContractId = job.ContractId,
            Type = job.Type,
            Status = status,
            ScheduledDate = job.ScheduledDate,
            AssignedToUserId = job.AssignedToUserId,
            SiteAddress = job.SiteAddress,
            Description = job.Description
        });

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddNoteAsync()
    {
        NewNote.JobId = Id;
        if (!string.IsNullOrWhiteSpace(NewNote.Text))
        {
            await _fieldServiceJobNoteAppService.CreateAsync(NewNote);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteNoteAsync(Guid noteId)
    {
        await _fieldServiceJobNoteAppService.DeleteAsync(noteId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddPartAsync()
    {
        NewPart.JobId = Id;
        await _fieldServiceJobPartAppService.CreateAsync(NewPart);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeletePartAsync(Guid partId)
    {
        await _fieldServiceJobPartAppService.DeleteAsync(partId);
        return RedirectToPage(new { id = Id });
    }
}
