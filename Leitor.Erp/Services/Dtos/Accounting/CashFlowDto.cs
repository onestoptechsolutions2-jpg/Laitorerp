using System;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CashFlowDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public decimal NetIncome { get; set; }
    public decimal DepreciationAddBack { get; set; }
    public decimal AccountsReceivableChange { get; set; }
    public decimal AccountsPayableChange { get; set; }
    public decimal InventoryChange { get; set; }
    public decimal NetCashFromOperating { get; set; }

    // No asset-disposal/sale modeling in v1 - a documented scope cut, not silently assumed zero.
    public decimal NetCashFromInvesting { get; set; }

    // Always 0 - this app has no Loan/Equity-contribution entities to source it from; not
    // fabricated as if it were a real computed figure.
    public decimal NetCashFromFinancing { get; set; }

    public decimal NetChangeInCash { get; set; }

    // Cross-check against the actual change in the Cash-role account balance over the period -
    // should equal NetChangeInCash if every cash movement in this app is already reflected in the
    // GL (it may not exactly match if a Cash-role account was only recently designated).
    public decimal ActualCashChange { get; set; }
}
