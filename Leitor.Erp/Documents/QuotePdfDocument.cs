using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

public class QuotePdfDocument : IDocument
{
    private readonly QuoteDto _quote;
    private readonly IReadOnlyList<QuoteLineDto> _lines;
    private readonly Customer _customer;
    private readonly ErpCompanyOptions _company;

    private QuotePdfDocument(QuoteDto quote, IReadOnlyList<QuoteLineDto> lines, Customer customer, ErpCompanyOptions company)
    {
        _quote = quote;
        _lines = lines;
        _customer = customer;
        _company = company;
    }

    public static byte[] Generate(QuoteDto quote, IReadOnlyList<QuoteLineDto> lines, Customer customer, ErpCompanyOptions company) =>
        new QuotePdfDocument(quote, lines, customer, company).GeneratePdf();

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
        PdfLayoutHelpers.ComposeHeader(container, _company, "QUOTE", _quote.QuoteNumber, _quote.IssueDate, PdfLayoutHelpers.Humanize(_quote.Status));
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(15);

            column.Item().Text(_quote.Title).FontSize(13).Bold();

            column.Item().Element(c => PdfLayoutHelpers.ComposePartyBlock(
                c,
                "PREPARED FOR",
                _customer.Name,
                _customer.AddressLine,
                _customer.City,
                _customer.State,
                _customer.PostalCode,
                _customer.Country,
                _customer.PhoneNumber,
                _customer.Email
            ));

            if (_quote.ExpiryDate.HasValue)
            {
                column.Item().Text($"Valid Until: {_quote.ExpiryDate:d}").FontSize(10);
            }

            var rows = _lines
                .Select(x => new LineItemRow(x.Description, x.UnitPrice, x.Quantity, x.DiscountPercent, x.LineTotal))
                .ToList();
            column.Item().Element(c => PdfLayoutHelpers.ComposeLinesTable(c, rows));

            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Total", _quote.Total, emphasize: true));

            if (!string.IsNullOrWhiteSpace(_quote.Notes))
            {
                column.Item().PaddingTop(10).Text("Notes").FontSize(11).Bold();
                column.Item().Text(_quote.Notes).FontSize(9);
            }
        });
    }
}
