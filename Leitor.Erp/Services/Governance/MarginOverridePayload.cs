namespace Leitor.Erp.Services.Governance;

// Serialized into EscalationItem.PayloadJson for "Quote.MarginOverride"/"Order.MarginOverride"
// items - deserialized back by QuoteMarginOverrideEscalationHandler/
// OrderMarginOverrideEscalationHandler on approval.
public class MarginOverridePayload
{
    public string OverrideReason { get; set; } = string.Empty;
}
