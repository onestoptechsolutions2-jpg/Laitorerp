using System.Collections.Generic;
using System.Linq;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Services.Dtos.Procurement;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

public class PurchaseOrderPdfDocument : IDocument
{
    private readonly PurchaseOrderDto _purchaseOrder;
    private readonly IReadOnlyList<PurchaseOrderLineDto> _lines;
    private readonly Vendor _vendor;
    private readonly ErpCompanyOptions _company;

    private PurchaseOrderPdfDocument(PurchaseOrderDto purchaseOrder, IReadOnlyList<PurchaseOrderLineDto> lines, Vendor vendor, ErpCompanyOptions company)
    {
        _purchaseOrder = purchaseOrder;
        _lines = lines;
        _vendor = vendor;
        _company = company;
    }

    public static byte[] Generate(PurchaseOrderDto purchaseOrder, IReadOnlyList<PurchaseOrderLineDto> lines, Vendor vendor, ErpCompanyOptions company) =>
        new PurchaseOrderPdfDocument(purchaseOrder, lines, vendor, company).GeneratePdf();

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
        PdfLayoutHelpers.ComposeHeader(container, _company, "PURCHASE ORDER", _purchaseOrder.PONumber, _purchaseOrder.OrderDate, PdfLayoutHelpers.Humanize(_purchaseOrder.Status));
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(15);

            column.Item().Element(c => PdfLayoutHelpers.ComposePartyBlock(
                c,
                "VENDOR",
                _vendor.Name,
                _vendor.AddressLine,
                _vendor.City,
                _vendor.State,
                _vendor.PostalCode,
                _vendor.Country,
                _vendor.Phone,
                _vendor.Email
            ));

            var rows = _lines
                .Select(x => new LineItemRow(x.Description, x.UnitPrice, x.Quantity, x.DiscountPercent, x.LineTotal))
                .ToList();
            column.Item().Element(c => PdfLayoutHelpers.ComposeLinesTable(c, rows));

            column.Item().Element(c => PdfLayoutHelpers.ComposeTotal(c, "Total", _purchaseOrder.Total, emphasize: true));

            if (_purchaseOrder.ExpectedDeliveryDate.HasValue)
            {
                column.Item().PaddingTop(10).Text($"Expected Delivery: {_purchaseOrder.ExpectedDeliveryDate.Value:d}").FontSize(9);
            }

            if (!string.IsNullOrWhiteSpace(_purchaseOrder.Notes))
            {
                column.Item().PaddingTop(10).Text("Notes").FontSize(11).Bold();
                column.Item().Text(_purchaseOrder.Notes).FontSize(9);
            }
        });
    }
}
