namespace Leitor.Erp.Entities.Partners;

// Fixed is a flat currency amount (Rate is read directly as the payout); the other three are a
// percentage (Rate 0-100) applied against Commission.BaseAmount - Percentage against revenue,
// Margin against gross margin, RevenueShare against the partner/client split of a shared deal.
// Tiered commission (mentioned in the platform brief) is a deliberate scope cut - no real Laitor
// deal has needed it yet; add it only once one does.
public enum CommissionBasis
{
    Percentage = 0,
    Margin = 1,
    RevenueShare = 2,
    Fixed = 3
}
