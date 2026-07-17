using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

public class OrderPdfDocument : IDocument
{
    private readonly OrderDto _order;
    private readonly IReadOnlyList<OrderLineDto> _lines;
    private readonly Customer _customer;
    private readonly ErpCompanyOptions _company;

    private OrderPdfDocument(OrderDto order, IReadOnlyList<OrderLineDto> lines, Customer customer, ErpCompanyOptions company)
    {
        _order = order;
        _lines = lines;
        _customer = customer;
        _company = company;
    }

    public static byte[] Generate(OrderDto order, IReadOnlyList<OrderLineDto> lines, Customer customer, ErpCompanyOptions company) =>
        new OrderPdfDocument(order, lines, customer, company).GeneratePdf();

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
        PdfLayoutHelpers.ComposeHeader(container, _company, "SALES ORDER", _order.OrderNumber, _order.OrderDate, PdfLayoutHelpers.Humanize(_order.Status));
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(15);

            column.Item().Element(c => PdfLayoutHelpers.ComposePartyBlock(
                c,
                "CUSTOMER",
                _customer.Name,
                _customer.AddressLine,
                _customer.City,
                _customer.State,
                _customer.PostalCode,
                _customer.Country,
                _customer.PhoneNumber,
                _customer.Email
            ));

            var rows = _lines
                .Select(x => new LineItemRow(x.Description, x.UnitPrice, x.Quantity, x.DiscountPercent, x.LineTotal))
                .ToList();
            column.Item().Element(c => PdfLayoutHelpers.ComposeLinesTable(c, rows));

            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Total", _order.Total, emphasize: true));

            if (!string.IsNullOrWhiteSpace(_order.Notes))
            {
                column.Item().PaddingTop(10).Text("Notes").FontSize(11).Bold();
                column.Item().Text(_order.Notes).FontSize(9);
            }
        });
    }
}
