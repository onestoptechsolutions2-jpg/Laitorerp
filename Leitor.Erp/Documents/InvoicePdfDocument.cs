using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

public class InvoicePdfDocument : IDocument
{
    private readonly InvoiceDto _invoice;
    private readonly IReadOnlyList<InvoiceLineDto> _lines;
    private readonly IReadOnlyList<PaymentDto> _payments;
    private readonly Customer _customer;
    private readonly ErpCompanyOptions _company;

    private InvoicePdfDocument(
        InvoiceDto invoice,
        IReadOnlyList<InvoiceLineDto> lines,
        IReadOnlyList<PaymentDto> payments,
        Customer customer,
        ErpCompanyOptions company)
    {
        _invoice = invoice;
        _lines = lines;
        _payments = payments;
        _customer = customer;
        _company = company;
    }

    public static byte[] Generate(
        InvoiceDto invoice,
        IReadOnlyList<InvoiceLineDto> lines,
        IReadOnlyList<PaymentDto> payments,
        Customer customer,
        ErpCompanyOptions company) =>
        new InvoicePdfDocument(invoice, lines, payments, customer, company).GeneratePdf();

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
            "INVOICE",
            _invoice.InvoiceNumber,
            _invoice.IssueDate,
            PdfLayoutHelpers.Humanize(_invoice.PaymentStatus)
        );
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(15);

            column.Item().Element(c => PdfLayoutHelpers.ComposePartyBlock(
                c,
                "BILL TO",
                _customer.Name,
                _customer.AddressLine,
                _customer.City,
                _customer.State,
                _customer.PostalCode,
                _customer.Country,
                _customer.PhoneNumber,
                _customer.Email
            ));

            column.Item().Text($"Due Date: {_invoice.DueDate:d}").FontSize(10);

            var rows = _lines
                .Select(x => new LineItemRow(x.Description, x.UnitPrice, x.Quantity, x.DiscountPercent, x.LineTotal))
                .ToList();
            column.Item().Element(c => PdfLayoutHelpers.ComposeLinesTable(c, rows));

            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Subtotal", _invoice.Subtotal));
            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Tax", _invoice.TaxAmount));
            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Total", _invoice.Total, emphasize: true));
            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Amount Paid", _invoice.AmountPaid));
            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Balance Due", _invoice.Total - _invoice.AmountPaid));

            if (_payments.Count > 0)
            {
                column.Item().PaddingTop(10).Text("Payments").FontSize(11).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(PdfLayoutHelpers.HeaderCell).Text("Date");
                        header.Cell().Element(PdfLayoutHelpers.HeaderCell).AlignRight().Text("Amount");
                        header.Cell().Element(PdfLayoutHelpers.HeaderCell).Text("Method");
                        header.Cell().Element(PdfLayoutHelpers.HeaderCell).Text("Reference");
                    });

                    foreach (var payment in _payments)
                    {
                        table.Cell().Element(PdfLayoutHelpers.BodyCell).Text(payment.PaymentDate.ToString("d"));
                        table.Cell().Element(PdfLayoutHelpers.BodyCell).AlignRight().Text(payment.Amount.ToString("N2"));
                        table.Cell().Element(PdfLayoutHelpers.BodyCell).Text(PdfLayoutHelpers.Humanize(payment.Method));
                        table.Cell().Element(PdfLayoutHelpers.BodyCell).Text(payment.Reference ?? "");
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(_invoice.Notes))
            {
                column.Item().PaddingTop(10).Text("Notes").FontSize(11).Bold();
                column.Item().Text(_invoice.Notes).FontSize(9);
            }
        });
    }
}
