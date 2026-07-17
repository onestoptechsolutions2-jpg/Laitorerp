using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.FieldService;
using Leitor.Erp.Services.FieldService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace Leitor.Erp.Pages.FieldService.Jobs;

[Authorize(Policy = ErpPermissions.FieldService.Default)]
public class DetailModel : AbpPageModel
{
    private readonly FieldServiceJobAppService _fieldServiceJobAppService;
    private readonly FieldServiceJobNoteAppService _fieldServiceJobNoteAppService;
    private readonly FieldServiceJobPartAppService _fieldServiceJobPartAppService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IEmailSender _emailSender;
    private readonly ErpCompanyOptions _companyOptions;

    public DetailModel(
        FieldServiceJobAppService fieldServiceJobAppService,
        FieldServiceJobNoteAppService fieldServiceJobNoteAppService,
        FieldServiceJobPartAppService fieldServiceJobPartAppService,
        IRepository<Customer, Guid> customerRepository,
        IEmailSender emailSender,
        IOptions<ErpCompanyOptions> companyOptions)
    {
        _fieldServiceJobAppService = fieldServiceJobAppService;
        _fieldServiceJobNoteAppService = fieldServiceJobNoteAppService;
        _fieldServiceJobPartAppService = fieldServiceJobPartAppService;
        _customerRepository = customerRepository;
        _emailSender = emailSender;
        _companyOptions = companyOptions.Value;
    }

    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public FieldServiceJobDto Job { get; set; } = null!;
    public IReadOnlyList<FieldServiceJobNoteDto> Notes { get; set; } = Array.Empty<FieldServiceJobNoteDto>();
    public IReadOnlyList<FieldServiceJobPartDto> Parts { get; set; } = Array.Empty<FieldServiceJobPartDto>();
    public Customer Customer { get; set; } = null!;

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
        Customer = await _customerRepository.GetAsync(Job.CustomerId);

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

    public async Task<IActionResult> OnGetPdfAsync()
    {
        await LoadAsync();
        var pdfBytes = FieldServiceJobPdfDocument.Generate(Job, Parts, Notes, Customer, _companyOptions);
        return File(pdfBytes, "application/pdf", $"JobSheet-{Job.ScheduledDate:yyyyMMdd}-{Job.Id.ToString()[..8]}.pdf");
    }

    public async Task<IActionResult> OnPostEmailAsync()
    {
        await LoadAsync();

        if (!string.IsNullOrWhiteSpace(Customer.Email))
        {
            var pdfBytes = FieldServiceJobPdfDocument.Generate(Job, Parts, Notes, Customer, _companyOptions);
            var fileName = $"JobSheet-{Job.ScheduledDate:yyyyMMdd}.pdf";
            await _emailSender.SendAsync(
                Customer.Email,
                $"Job Sheet - {Job.ScheduledDate:d}",
                $"Dear {Customer.Name},\n\nPlease find attached the job sheet for the visit on {Job.ScheduledDate:d}.\n\nRegards,\n{_companyOptions.Name}",
                isBodyHtml: false,
                new AdditionalEmailSendingArgs
                {
                    Attachments = new List<EmailAttachment>
                    {
                        new() { Name = fileName, File = pdfBytes }
                    }
                }
            );
        }

        return RedirectToPage(new { id = Id });
    }
}
