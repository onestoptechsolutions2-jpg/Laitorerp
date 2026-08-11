using System.Collections.Generic;
using Leitor.Erp.Services.Dtos.Customers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Leitor.Erp.Documents;

// A generated legal contract: fixed scaffolding (title block, signature block, footer) composed
// here directly from CustomerContract/Customer/company settings, plus the template's own ordered
// ContractTemplateSections rendered as heading + body (ContractTemplateRenderer substitutes every
// [Placeholder] token before this class ever sees the text). See ProposalPdfDocument.cs for the
// sibling narrative-document pattern this follows - no line-item table, unlike Quote/Order/Invoice.
public class ContractPdfDocument : IDocument
{
    private readonly ContractTemplateDto _template;
    private readonly Dictionary<string, string> _tokens;

    private ContractPdfDocument(ContractTemplateDto template, Dictionary<string, string> tokens)
    {
        _template = template;
        _tokens = tokens;
    }

    public static byte[] Generate(CustomerContractDto contract, ContractTemplateDto template, CustomerDto customer, ErpCompanyOptions company, string? companySignatoryName)
    {
        var tokens = ContractTemplateRenderer.BuildTokens(contract, customer, company, companySignatoryName, template.DefaultTermMonths);
        return new ContractPdfDocument(template, tokens).GeneratePdf();
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Content().Element(ComposeContent);
            page.Footer().Element(PdfLayoutHelpers.ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(column =>
        {
            column.Spacing(12);

            column.Item().AlignCenter().Text(_template.Name.ToUpperInvariant()).FontSize(16).Bold();
            column.Item().AlignCenter().Text("BETWEEN").FontSize(10);
            column.Item().AlignCenter().Text(_tokens["[CompanyName]"]).FontSize(12).Bold();
            column.Item().AlignCenter().Text("AND").FontSize(10);
            column.Item().AlignCenter().Text(_tokens["[ClientName]"]).FontSize(12).Bold();

            if (!string.IsNullOrWhiteSpace(_tokens["[Purpose]"]))
            {
                column.Item().AlignCenter().Text($"FOR: {_tokens["[Purpose]"].ToUpperInvariant()}").FontSize(10);
            }

            column.Item().AlignCenter().Text($"DATED: {_tokens["[StartDate]"]}").FontSize(10);

            column.Item().PaddingTop(10).Text($"THIS AGREEMENT is entered into this {_tokens["[AgreementDate]"]}, between {_tokens["[CompanyName]"]}, of {_tokens["[CompanyAddress]"]} (\"the Service Provider\") and {_tokens["[ClientName]"]}, of {_tokens["[ClientAddress]"]} (\"the Client\").").FontSize(9);

            foreach (var section in _template.Sections)
            {
                var body = ContractTemplateRenderer.Render(section.BodyText, _tokens);
                if (string.IsNullOrWhiteSpace(body))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(section.Heading))
                {
                    column.Item().PaddingTop(6).Text(ContractTemplateRenderer.Render(section.Heading, _tokens)).FontSize(11).Bold();
                }

                column.Item().Text(body).FontSize(9);
            }

            ComposeSignatureBlock(column);
        });
    }

    private void ComposeSignatureBlock(ColumnDescriptor column)
    {
        column.Item().PaddingTop(15).Text("IN WITNESS WHEREOF this is signed on behalf of the Parties by their duly authorized representatives on the day and year hereinbefore written.").FontSize(9);

        ComposeSignatory(column, _tokens["[CompanySignatoryName]"], $"The duly authorized representative of {_tokens["[CompanyName]"]}");
        ComposeSignatory(column, _tokens["[ClientSignatoryName]"], $"The duly authorized representative of {_tokens["[ClientName]"]}");
    }

    private static void ComposeSignatory(ColumnDescriptor column, string signatoryName, string capacity)
    {
        column.Item().PaddingTop(15).Text($"SIGNED BY: {signatoryName}").FontSize(9).Bold();
        column.Item().Text(capacity).FontSize(9);
        column.Item().PaddingTop(10).Text("Signature: __________________________").FontSize(9);
        column.Item().PaddingTop(8).Text("In the presence of:").FontSize(9);
        column.Item().Text("Witness Name: _______________________").FontSize(9);
        column.Item().Text("Witness Signature: __________________________").FontSize(9);
    }
}
