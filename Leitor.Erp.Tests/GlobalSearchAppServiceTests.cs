using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Sales;
using Leitor.Erp.Services.Search;
using Leitor.Erp.Services.Support;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the cross-module search AppService built 2026-08-17 after a usability audit flagged "no
// way to find a record without already knowing which module owns it" as the app's highest-
// friction gap. AlwaysAllowAuthorizationService means every permission check here always passes
// (same limitation as every other governance-adjacent test in this suite), so these tests cover
// the query/matching/DTO-mapping logic itself, not the per-entity-type permission gating.
//
// The URL assertions below caught a real live bug the first time this file was written: all 4
// target pages use `@page "{id}"` (a route SEGMENT, e.g. /Customers/Detail/{guid}), but the
// original code generated query-string URLs (/Customers/Detail?id={guid}) instead - which don't
// match that route at all, so every search-result click threw. The original assertions here
// mirrored that same bug (both sides agreed, so the test passed despite being wrong) - a live
// user clicking an actual result is what caught it, not this suite. Worth remembering: a URL
// string assertion only catches a routing mismatch if it's checked against the real page's
// @page directive, not just re-derived from the same code being tested.
public class GlobalSearchAppServiceTests : ErpTestBase
{
    [Fact]
    public async Task Term_Shorter_Than_Two_Characters_Returns_No_Results()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Acme Ltd" });

        var searchAppService = GetRequiredService<GlobalSearchAppService>();
        var results = await searchAppService.SearchAsync("a");

        Assert.Empty(results);
    }

    [Fact]
    public async Task Matches_Customer_By_Name()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Globex Corporation" });

        var searchAppService = GetRequiredService<GlobalSearchAppService>();
        var results = await searchAppService.SearchAsync("Globex");

        var result = Assert.Single(results);
        Assert.Equal("Customer", result.EntityType);
        Assert.Equal("Globex Corporation", result.Title);
        Assert.Equal($"/Customers/Detail/{customer.Id}", result.Url);
    }

    [Fact]
    public async Task Matches_Lead_By_CompanyName()
    {
        await EnsureDatabaseCreatedAsync();
        var leadAppService = GetRequiredService<LeadAppService>();
        var lead = await leadAppService.CreateAsync(new CreateUpdateLeadDto { Name = "Jane Doe", CompanyName = "Initech Solutions" });

        var searchAppService = GetRequiredService<GlobalSearchAppService>();
        var results = await searchAppService.SearchAsync("Initech");

        var result = Assert.Single(results);
        Assert.Equal("Lead", result.EntityType);
        Assert.Equal($"/Leads/Detail/{lead.Id}", result.Url);
    }

    [Fact]
    public async Task Matches_Ticket_By_Subject_And_Includes_Customer_Name_As_Subtitle()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Umbrella Corp" });

        var ticketAppService = GetRequiredService<TicketAppService>();
        var ticket = await ticketAppService.CreateAsync(new CreateUpdateTicketDto
        {
            CustomerId = customer.Id,
            Subject = "VPN keeps dropping overnight",
            Status = TicketStatus.Open
        });

        var searchAppService = GetRequiredService<GlobalSearchAppService>();
        var results = await searchAppService.SearchAsync("keeps dropping");

        var result = Assert.Single(results);
        Assert.Equal("Ticket", result.EntityType);
        Assert.Equal("Umbrella Corp", result.Subtitle);
        Assert.Equal($"/Support/Tickets/Detail/{ticket.Id}", result.Url);
    }

    [Fact]
    public async Task Matches_Invoice_By_InvoiceNumber()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Wayne Enterprises" });

        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var invoice = await invoiceAppService.CreateAsync(new CreateUpdateInvoiceDto
        {
            CustomerId = customer.Id,
            Status = InvoiceStatus.Issued,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            CurrencyCode = "KES"
        });

        var searchAppService = GetRequiredService<GlobalSearchAppService>();
        var results = await searchAppService.SearchAsync(invoice.InvoiceNumber);

        var result = Assert.Single(results);
        Assert.Equal("Invoice", result.EntityType);
        Assert.Equal("Wayne Enterprises", result.Subtitle);
        Assert.Equal($"/Sales/Invoices/Detail/{invoice.Id}", result.Url);
    }

    [Fact]
    public async Task NonMatching_Term_Returns_No_Results()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Acme Ltd" });

        var searchAppService = GetRequiredService<GlobalSearchAppService>();
        var results = await searchAppService.SearchAsync("Nonexistent Company XYZ");

        Assert.Empty(results);
    }
}
