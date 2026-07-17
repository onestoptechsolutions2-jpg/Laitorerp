using System.Collections.Generic;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.FieldService;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

public class FieldServiceJobPdfDocument : IDocument
{
    private readonly FieldServiceJobDto _job;
    private readonly IReadOnlyList<FieldServiceJobPartDto> _parts;
    private readonly IReadOnlyList<FieldServiceJobNoteDto> _notes;
    private readonly Customer _customer;
    private readonly ErpCompanyOptions _company;

    private FieldServiceJobPdfDocument(
        FieldServiceJobDto job,
        IReadOnlyList<FieldServiceJobPartDto> parts,
        IReadOnlyList<FieldServiceJobNoteDto> notes,
        Customer customer,
        ErpCompanyOptions company)
    {
        _job = job;
        _parts = parts;
        _notes = notes;
        _customer = customer;
        _company = company;
    }

    public static byte[] Generate(
        FieldServiceJobDto job,
        IReadOnlyList<FieldServiceJobPartDto> parts,
        IReadOnlyList<FieldServiceJobNoteDto> notes,
        Customer customer,
        ErpCompanyOptions company) =>
        new FieldServiceJobPdfDocument(job, parts, notes, customer, company).GeneratePdf();

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
            "JOB SHEET",
            $"{PdfLayoutHelpers.Humanize(_job.Type)} - {_job.ScheduledDate:d}",
            _job.ScheduledDate,
            PdfLayoutHelpers.Humanize(_job.Status)
        );
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(15);

            column.Item().Element(c => PdfLayoutHelpers.ComposePartyBlock(
                c,
                "CUSTOMER / SITE",
                _customer.Name,
                _job.SiteAddress ?? _customer.AddressLine,
                _customer.City,
                _customer.State,
                _customer.PostalCode,
                _customer.Country,
                _customer.PhoneNumber,
                _customer.Email
            ));

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Assigned To: {_job.AssignedToUserName ?? "-"}").FontSize(10);
                row.RelativeItem().AlignRight().Text($"Completed: {(_job.CompletedDate.HasValue ? _job.CompletedDate.Value.ToString("d") : "-")}").FontSize(10);
            });

            if (!string.IsNullOrWhiteSpace(_job.Description))
            {
                column.Item().Text("Description").FontSize(11).Bold();
                column.Item().Text(_job.Description).FontSize(9);
            }

            if (_parts.Count > 0)
            {
                column.Item().Text("Parts Used").FontSize(11).Bold();
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Description").Bold();
                        header.Cell().AlignRight().Text("Qty").Bold();
                    });

                    foreach (var part in _parts)
                    {
                        table.Cell().Text(part.Description);
                        table.Cell().AlignRight().Text(part.Quantity.ToString("N2"));
                    }
                });
            }

            if (_notes.Count > 0)
            {
                column.Item().Text("Visit Log").FontSize(11).Bold();
                foreach (var note in _notes)
                {
                    column.Item().Text($"[{PdfLayoutHelpers.Humanize(note.Type)}] {note.CreationTime:g} - {note.Text}").FontSize(9);
                }
            }

            // Standard field-service work-order convention: a blank line for the customer to sign
            // off on-site, acknowledging the visit took place.
            column.Item().PaddingTop(25).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Darken1).Text("Customer Signature").FontSize(8);
                });
                row.ConstantItem(30);
                row.RelativeItem().Column(c =>
                {
                    c.Item().PaddingTop(20).BorderTop(1).BorderColor(Colors.Grey.Darken1).Text("Date").FontSize(8);
                });
            });
        });
    }
}
