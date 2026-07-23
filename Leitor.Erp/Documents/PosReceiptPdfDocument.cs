using System.Linq;
using Leitor.Erp.Services.Dtos.Pos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

// Same static Generate(...) : byte[] pattern as InvoicePdfDocument/QuotePdfDocument/etc - a POS
// sale is paid in full at the till, so there's no "Amount Paid"/"Balance Due" split to show, just
// the tender breakdown (cash/card/etc, possibly split across several PosPayment rows).
public class PosReceiptPdfDocument : IDocument
{
    private readonly PosSaleDto _sale;
    private readonly ErpCompanyOptions _company;

    private PosReceiptPdfDocument(PosSaleDto sale, ErpCompanyOptions company)
    {
        _sale = sale;
        _company = company;
    }

    public static byte[] Generate(PosSaleDto sale, ErpCompanyOptions company) =>
        new PosReceiptPdfDocument(sale, company).GeneratePdf();

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(PdfLayoutHelpers.ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        PdfLayoutHelpers.ComposeHeader(
            container,
            _company,
            "RECEIPT",
            _sale.SaleNumber,
            _sale.SaleDate,
            _sale.Status == Entities.Pos.PosSaleStatus.Voided ? "VOIDED" : null
        );
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(12);

            if (!string.IsNullOrWhiteSpace(_sale.CustomerName))
            {
                column.Item().Text($"Customer: {_sale.CustomerName}").FontSize(10);
            }

            var rows = _sale.Lines
                .Select(x => new LineItemRow(x.Description, x.UnitPrice, x.Quantity, x.DiscountPercent, x.LineTotal))
                .ToList();
            column.Item().Element(c => PdfLayoutHelpers.ComposeLinesTable(c, rows));

            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Subtotal", _sale.Subtotal));
            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Tax", _sale.TaxAmount));
            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Total", _sale.Total, emphasize: true));

            column.Item().PaddingTop(6).Text("Payment").FontSize(11).Bold();
            foreach (var payment in _sale.Payments)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text(PdfLayoutHelpers.Humanize(payment.Method));
                    row.RelativeItem().AlignRight().Text(payment.Amount.ToString("N2"));
                });
            }

            if (!string.IsNullOrWhiteSpace(_sale.Notes))
            {
                column.Item().PaddingTop(10).Text("Notes").FontSize(11).Bold();
                column.Item().Text(_sale.Notes).FontSize(9);
            }
        });
    }
}
