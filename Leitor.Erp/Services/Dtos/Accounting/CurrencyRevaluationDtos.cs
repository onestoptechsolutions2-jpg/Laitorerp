using System;
using System.Collections.Generic;

namespace Leitor.Erp.Services.Dtos.Accounting;

public class CurrencyRevaluationPreviewDto
{
    public DateTime AsOfDate { get; set; }
    public string BaseCurrencyCode { get; set; } = string.Empty;
    public List<RevaluationLineDto> Lines { get; set; } = new();
    public decimal NetArChangeBase { get; set; }
    public decimal NetApChangeBase { get; set; }

    // Positive = net unrealized gain, negative = net unrealized loss.
    public decimal NetGainLoss { get; set; }
}

// One open foreign-currency document's exposure - shown for transparency even though the actual
// posting nets everything into at most 3 lines (AR adjustment, AP adjustment, FX gain/loss).
public class RevaluationLineDto
{
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal AmountDueForeign { get; set; }
    public decimal OriginalRate { get; set; }
    public decimal CurrentRate { get; set; }
    public decimal GainLossBase { get; set; }
}
