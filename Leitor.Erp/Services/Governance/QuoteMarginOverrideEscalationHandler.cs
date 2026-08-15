using System.Text.Json;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;
using Leitor.Erp.Services.Sales;
using Volo.Abp;

namespace Leitor.Erp.Services.Governance;

// Registered explicitly in ErpModule.ConfigureServices (not via ITransientDependency convention -
// see that registration's own comment) - resolved into EscalationItemAppService's
// IEnumerable<IEscalationActionHandler>. Calls QuoteAppService.SendAsync (not
// UpdateAsync) so it doesn't need to reconstruct a full CreateUpdateQuoteDto just to flip Status -
// see SendAsync's own comment. Re-runs EnforceMarginGateAsync in full rather than trusting the
// margin computed at filing time (deliberate - see the plan's "handler re-validation strategy").
//
// No infinite-recursion risk: by the time this runs, CurrentUser (inside QuoteAppService) is the
// approver who reached EscalationItemAppService.ApproveAsync, not the original filer. If the
// approver holds Sales.OverrideMarginGate, EnforceMarginGateAsync takes the "apply" branch. If the
// approver only holds the Escalations.Decide catch-all (not the domain permission itself),
// EnforceMarginGateAsync's own independent check fails and it tries to re-file - which throws a
// UserFriendlyException out of this method, caught by ApproveAsync and recorded as a Failed
// outcome with a clear message. A legitimate, comprehensible failure mode, not a loop.
public class QuoteMarginOverrideEscalationHandler : IEscalationActionHandler
{
    private readonly QuoteAppService _quoteAppService;

    public QuoteMarginOverrideEscalationHandler(QuoteAppService quoteAppService)
    {
        _quoteAppService = quoteAppService;
    }

    public string ActionType => "Quote.MarginOverride";

    public async Task ExecuteAsync(EscalationItem item)
    {
        if (string.IsNullOrWhiteSpace(item.PayloadJson))
        {
            throw new UserFriendlyException("This escalation is missing its override-reason payload and can't be executed.");
        }

        var payload = JsonSerializer.Deserialize<MarginOverridePayload>(item.PayloadJson)
            ?? throw new UserFriendlyException("This escalation's payload could not be read.");

        await _quoteAppService.SendAsync(item.EntityId, payload.OverrideReason);
    }
}
