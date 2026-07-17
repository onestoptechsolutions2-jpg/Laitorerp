using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

// Every Quote/Order/Invoice/PurchaseOrder line has the same shape (Description/UnitPrice/
// Quantity/DiscountPercent/LineTotal) - this record lets ComposeLinesTable be shared across all
// of them instead of four near-identical table-composition methods.
public record LineItemRow(string Description, decimal UnitPrice, decimal Quantity, decimal DiscountPercent, decimal LineTotal);

// Shared composition building blocks for every generated document (Invoice/Quote/Order/
// FieldServiceJob/PurchaseOrder) so the letterhead, address blocks, line-items table, and footer
// look identical across all of them.
public static class PdfLayoutHelpers
{
    public static void ComposeHeader(
        IContainer container,
        ErpCompanyOptions company,
        string documentTitle,
        string documentNumber,
        DateTime documentDate,
        string? statusLabel = null)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(company.Name).FontSize(16).Bold();

                var addressParts = new[] { company.AddressLine, JoinCityStatePostal(company), company.Country }
                    .Where(x => !string.IsNullOrWhiteSpace(x));
                foreach (var line in addressParts)
                {
                    column.Item().Text(line).FontSize(9).FontColor(Colors.Grey.Darken1);
                }

                if (!string.IsNullOrWhiteSpace(company.Phone))
                {
                    column.Item().Text(company.Phone).FontSize(9).FontColor(Colors.Grey.Darken1);
                }

                if (!string.IsNullOrWhiteSpace(company.Email))
                {
                    column.Item().Text(company.Email).FontSize(9).FontColor(Colors.Grey.Darken1);
                }
            });

            row.RelativeItem().Column(column =>
            {
                column.Item().AlignRight().Text(documentTitle).FontSize(18).Bold();
                column.Item().AlignRight().Text(documentNumber).FontSize(12).FontColor(Colors.Grey.Darken2);
                column.Item().AlignRight().Text(documentDate.ToString("d")).FontSize(10).FontColor(Colors.Grey.Darken1);

                if (!string.IsNullOrWhiteSpace(statusLabel))
                {
                    column.Item().AlignRight().PaddingTop(4).Text(statusLabel).FontSize(10).Bold();
                }
            });
        });
    }

    public static void ComposePartyBlock(
        IContainer container,
        string heading,
        string name,
        string? addressLine,
        string? city,
        string? state,
        string? postalCode,
        string? country,
        string? phone,
        string? email)
    {
        container.Column(column =>
        {
            column.Item().Text(heading).FontSize(9).FontColor(Colors.Grey.Darken1).Bold();
            column.Item().Text(name).FontSize(11).Bold();

            var cityStatePostal = JoinNonEmpty(", ", city, state, postalCode);
            foreach (var line in new[] { addressLine, cityStatePostal, country }.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                column.Item().Text(line).FontSize(9);
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                column.Item().Text(phone).FontSize(9);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                column.Item().Text(email).FontSize(9);
            }
        });
    }

    public static void ComposeLinesTable(IContainer container, IReadOnlyList<LineItemRow> lines)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Description");
                header.Cell().Element(HeaderCell).AlignRight().Text("Unit Price");
                header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                header.Cell().Element(HeaderCell).AlignRight().Text("Disc %");
                header.Cell().Element(HeaderCell).AlignRight().Text("Line Total");
            });

            foreach (var line in lines)
            {
                table.Cell().Element(BodyCell).Text(line.Description);
                table.Cell().Element(BodyCell).AlignRight().Text(line.UnitPrice.ToString("N2"));
                table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString("N2"));
                table.Cell().Element(BodyCell).AlignRight().Text(line.DiscountPercent.ToString("N0"));
                table.Cell().Element(BodyCell).AlignRight().Text(line.LineTotal.ToString("N2"));
            }
        });
    }

    // Every ad-hoc table (e.g. InvoicePdfDocument's Payments table) shares this cell styling too,
    // so columns never visually run together the way unstyled QuestPDF table cells do by default.
    public static IContainer HeaderCell(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Darken1).PaddingVertical(4).PaddingRight(8).DefaultTextStyle(x => x.Bold());

    public static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingRight(8);

    // Enum.ToString() renders "PartiallyPaid"/"BankTransfer" with no separators - this inserts a
    // space before each internal capital letter so generated documents show "Partially Paid" /
    // "Bank Transfer" instead. English-only for now, same as the rest of these documents (no other
    // locale file has Sales/FieldService keys either - see project memory).
    public static string Humanize(object enumValue)
    {
        var text = enumValue.ToString() ?? string.Empty;
        var builder = new StringBuilder();

        for (var i = 0; i < text.Length; i++)
        {
            if (i > 0 && char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
            {
                builder.Append(' ');
            }

            builder.Append(text[i]);
        }

        return builder.ToString();
    }

    public static void ComposeTotal(IContainer container, string label, decimal amount, bool emphasize = false)
    {
        container.AlignRight().Row(row =>
        {
            row.AutoItem().PaddingRight(10).Text(label).FontSize(emphasize ? 12 : 10).Bold();
            row.AutoItem().Text(amount.ToString("N2")).FontSize(emphasize ? 12 : 10).Bold();
        });
    }

    public static void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Generated {DateTime.Now:g}").FontSize(8).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.DefaultTextStyle(y => y.FontSize(8).FontColor(Colors.Grey.Darken1));
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        });
    }

    private static string? JoinCityStatePostal(ErpCompanyOptions company) =>
        JoinNonEmpty(", ", company.City, company.State, company.PostalCode);

    private static string? JoinNonEmpty(string separator, params string?[] parts)
    {
        var nonEmpty = parts.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return nonEmpty.Count == 0 ? null : string.Join(separator, nonEmpty);
    }
}
