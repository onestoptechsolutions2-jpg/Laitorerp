using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sms;
using Leitor.Erp.Services.Sms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Marketing.BulkSms;

[Authorize(Policy = ErpPermissions.BulkSms.Send)]
public class DetailModel : AbpPageModel
{
    private readonly BulkSmsAppService _bulkSmsAppService;

    public DetailModel(BulkSmsAppService bulkSmsAppService)
    {
        _bulkSmsAppService = bulkSmsAppService;
    }

    public Guid BatchId { get; set; }
    public List<BulkSmsMessageDto> Messages { get; set; } = new();

    public async Task OnGetAsync(Guid batchId)
    {
        BatchId = batchId;
        Messages = await _bulkSmsAppService.GetBatchMessagesAsync(batchId);
    }
}
