using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Documents;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Documents;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-22 "smart document link" feature (erp-review backlog item 4):
// DocumentShareLinkService's idempotent get-or-create behavior. PublicDocumentResolver's PDF
// regeneration is exercised indirectly via the same DocumentType constants here (Quote) - a full
// per-document-type PDF byte-content assertion isn't worth the setup cost when the underlying
// *PdfDocument.Generate calls are already exactly what the authenticated Download/Email handlers
// use and are not new code.
public class DocumentShareLinkServiceTests : ErpTestBase
{
    [Fact]
    public async Task GetOrCreateUrlAsync_Same_Document_Returns_Same_Url_Both_Times()
    {
        await EnsureDatabaseCreatedAsync();
        var service = GetRequiredService<DocumentShareLinkService>();
        var entityId = Guid.NewGuid();

        var firstUrl = await service.GetOrCreateUrlAsync(DocumentShareType.Quote, entityId);
        var secondUrl = await service.GetOrCreateUrlAsync(DocumentShareType.Quote, entityId);

        Assert.Equal(firstUrl, secondUrl);
        Assert.Contains("/d/", firstUrl);
    }

    [Fact]
    public async Task GetOrCreateUrlAsync_Different_Entities_Get_Different_Tokens()
    {
        await EnsureDatabaseCreatedAsync();
        var service = GetRequiredService<DocumentShareLinkService>();

        var firstUrl = await service.GetOrCreateUrlAsync(DocumentShareType.Quote, Guid.NewGuid());
        var secondUrl = await service.GetOrCreateUrlAsync(DocumentShareType.Quote, Guid.NewGuid());

        Assert.NotEqual(firstUrl, secondUrl);
    }

    [Fact]
    public async Task GetOrCreateUrlAsync_Same_Entity_Different_DocumentType_Get_Different_Tokens()
    {
        await EnsureDatabaseCreatedAsync();
        var service = GetRequiredService<DocumentShareLinkService>();
        var entityId = Guid.NewGuid();

        var quoteUrl = await service.GetOrCreateUrlAsync(DocumentShareType.Quote, entityId);
        var orderUrl = await service.GetOrCreateUrlAsync(DocumentShareType.Order, entityId);

        Assert.NotEqual(quoteUrl, orderUrl);
    }

    [Fact]
    public async Task PublicDocumentResolver_Resolves_A_Real_Quote_To_A_Nonempty_Pdf()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var quoteAppService = GetRequiredService<QuoteAppService>();
        var quoteLineAppService = GetRequiredService<QuoteLineAppService>();
        var resolver = GetRequiredService<PublicDocumentResolver>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Densification Apartments" });
        var quote = await quoteAppService.CreateAsync(new CreateUpdateQuoteDto
        {
            CustomerId = customer.Id,
            Title = "CCTV Installation",
            IssueDate = DateTime.UtcNow,
            CurrencyCode = "KES"
        });
        await quoteLineAppService.CreateAsync(new CreateUpdateQuoteLineDto
        {
            QuoteId = quote.Id,
            Description = "4-camera CCTV kit",
            UnitPrice = 45000,
            Quantity = 1
        });

        var result = await resolver.ResolveAsync(DocumentShareType.Quote, quote.Id);

        Assert.NotNull(result);
        Assert.NotEmpty(result!.PdfBytes);
        Assert.Contains(quote.QuoteNumber, result.Title);
        Assert.Equal(customer.Name, result.Subtitle);
    }

    [Fact]
    public async Task PublicDocumentResolver_Returns_Null_For_Unknown_Token_Target()
    {
        await EnsureDatabaseCreatedAsync();
        var resolver = GetRequiredService<PublicDocumentResolver>();

        var result = await resolver.ResolveAsync(DocumentShareType.Quote, Guid.NewGuid());

        Assert.Null(result);
    }
}
