using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Opportunities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

// Unlike Order/Quote/Invoice/PurchaseOrder, a Proposal has no line-item table - it's a narrative
// document, so ComposeContent renders each non-empty section as a heading + paragraph instead of
// PdfLayoutHelpers.ComposeLinesTable.
public class ProposalPdfDocument : IDocument
{
    private readonly ProposalDto _proposal;
    private readonly Customer _customer;
    private readonly ErpCompanyOptions _company;

    private ProposalPdfDocument(ProposalDto proposal, Customer customer, ErpCompanyOptions company)
    {
        _proposal = proposal;
        _customer = customer;
        _company = company;
    }

    public static byte[] Generate(ProposalDto proposal, Customer customer, ErpCompanyOptions company) =>
        new ProposalPdfDocument(proposal, customer, company).GeneratePdf();

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
        PdfLayoutHelpers.ComposeHeader(container, _company, "PROPOSAL", _proposal.ProposalNumber, System.DateTime.Now, PdfLayoutHelpers.Humanize(_proposal.Status));
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(15).Column(column =>
        {
            column.Spacing(15);

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

            ComposeSection(column, "Summary", _proposal.Summary);
            ComposeSection(column, "Proposed Solution", _proposal.ProposedSolution);
            ComposeSection(column, "Scope", _proposal.Scope);
            ComposeSection(column, "Timeline", _proposal.Timeline);
            ComposeSection(column, "Assumptions", _proposal.Assumptions);
            ComposeSection(column, "Exclusions", _proposal.Exclusions);
            ComposeSection(column, "Warranty & Support", _proposal.WarrantyAndSupport);
            ComposeSection(column, "Terms", _proposal.Terms);
        });
    }

    private static void ComposeSection(ColumnDescriptor column, string heading, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        column.Item().Text(heading).FontSize(11).Bold();
        column.Item().Text(text).FontSize(9);
    }
}
