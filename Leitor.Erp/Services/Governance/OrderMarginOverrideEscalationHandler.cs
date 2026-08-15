using System.Text.Json;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Services.Sales;
using Volo.Abp;

namespace Leitor.Erp.Services.Governance;

// Order-side counterpart to QuoteMarginOverrideEscalationHandler - see its comment for the
// re-validation and no-infinite-recursion reasoning, identical here (including that it's
// registered explicitly in ErpModule.ConfigureServices, not via convention). Calls the existing
// OrderAppService.ConfirmAsync directly (no dedicated entry point needed - unlike Quote, it
// already re-runs both the credit check and EnsureMarginAvailableAsync and only requires
// OrderStatus.Submitted).
public class OrderMarginOverrideEscalationHandler : IEscalationActionHandler
{
    private readonly OrderAppService _orderAppService;

    public OrderMarginOverrideEscalationHandler(OrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public string ActionType => "Order.MarginOverride";

    public async Task ExecuteAsync(EscalationItem item)
    {
        if (string.IsNullOrWhiteSpace(item.PayloadJson))
        {
            throw new UserFriendlyException("This escalation is missing its override-reason payload and can't be executed.");
        }

        var payload = JsonSerializer.Deserialize<MarginOverridePayload>(item.PayloadJson)
            ?? throw new UserFriendlyException("This escalation's payload could not be read.");

        await _orderAppService.ConfirmAsync(item.EntityId, payload.OverrideReason);
    }
}
