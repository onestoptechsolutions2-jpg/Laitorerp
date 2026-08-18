using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Dtos.Opportunities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

// Same narrative-document shape as ProposalPdfDocument (no line-item table) - built for the
// 2026-08-18 "Share Package" action on Opportunity Detail, which attaches the assessment
// alongside the Proposal/Quote PDFs so a customer gets the full picture in one email.
public class NeedsAssessmentPdfDocument : IDocument
{
    private readonly NeedsAssessmentDto _assessment;
    private readonly Customer _customer;
    private readonly ErpCompanyOptions _company;

    private NeedsAssessmentPdfDocument(NeedsAssessmentDto assessment, Customer customer, ErpCompanyOptions company)
    {
        _assessment = assessment;
        _customer = customer;
        _company = company;
    }

    public static byte[] Generate(NeedsAssessmentDto assessment, Customer customer, ErpCompanyOptions company) =>
        new NeedsAssessmentPdfDocument(assessment, customer, company).GeneratePdf();

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
        PdfLayoutHelpers.ComposeHeader(container, _company, "NEEDS ASSESSMENT", _assessment.ConductedDate.ToString("yyyy-MM-dd"), System.DateTime.Now, PdfLayoutHelpers.Humanize(_assessment.Type));
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

            ComposeSection(column, "Findings", _assessment.Findings);
            ComposeSection(column, "Customer Requirements", _assessment.CustomerRequirements);
            ComposeSection(column, "Recommendations", _assessment.Recommendations);
            ComposeSection(column, "Risks", _assessment.Risks);
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
