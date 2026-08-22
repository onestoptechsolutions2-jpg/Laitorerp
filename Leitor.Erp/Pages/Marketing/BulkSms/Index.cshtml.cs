using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sms;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sms;
using Leitor.Erp.Services.Sms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace Leitor.Erp.Pages.Marketing.BulkSms;

[Authorize(Policy = ErpPermissions.BulkSms.Send)]
public class IndexModel : AbpPageModel
{
    private readonly BulkSmsAppService _bulkSmsAppService;
    private readonly IHttpSmsClient _httpSmsClient;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public IndexModel(
        BulkSmsAppService bulkSmsAppService,
        IHttpSmsClient httpSmsClient,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _bulkSmsAppService = bulkSmsAppService;
        _httpSmsClient = httpSmsClient;
        _identityUserRepository = identityUserRepository;
    }

    [BindProperty]
    public ComposeInput Compose { get; set; } = new();

    public bool IsConfigured { get; set; }
    public List<BulkSmsBatchDto> Batches { get; set; } = new();
    public List<SelectListItem> UserOptions { get; set; } = new();

    [TempData]
    public string? QueueResultMessage { get; set; }

    public async Task OnGetAsync()
    {
        IsConfigured = await _httpSmsClient.IsConfiguredAsync();
        Batches = await _bulkSmsAppService.GetBatchesAsync();

        var users = await _identityUserRepository.GetListAsync();
        UserOptions = new List<SelectListItem> { new(L["None"], "") };
        UserOptions.AddRange(
            users.OrderBy(x => x.UserName).Select(x => new SelectListItem(x.UserName, x.Id.ToString()))
        );
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await _bulkSmsAppService.QueueBatchAsync(new BulkSmsQueueInput
        {
            Content = Compose.Content,
            Source = Compose.Source,
            LeadStatus = Compose.LeadStatus,
            AssignedToUserId = Compose.AssignedToUserId,
            CustomerStatus = Compose.CustomerStatus,
            ManualPhoneNumbers = Compose.ManualPhoneNumbers
        });

        QueueResultMessage = string.Format(L["BulkSmsQueueResult"].Value, result.QueuedCount, result.SkippedCount);
        return RedirectToPage();
    }

    public class ComposeInput
    {
        [Required]
        [StringLength(1600)]
        public string Content { get; set; } = string.Empty;

        public BulkSmsRecipientType Source { get; set; } = BulkSmsRecipientType.Lead;

        public LeadStatus? LeadStatus { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public CustomerStatus? CustomerStatus { get; set; }
        public string? ManualPhoneNumbers { get; set; }
    }
}
