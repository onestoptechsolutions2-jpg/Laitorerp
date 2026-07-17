using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Sales;
using Xunit;

namespace Leitor.Erp.Tests;

public class InvoiceAppServiceTests : ErpTestBase
{
    private async Task<Guid> CreateInvoiceWithLineAsync(decimal unitPrice, DateTime dueDate)
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var invoiceLineAppService = GetRequiredService<InvoiceLineAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Umbrella Corp" });

        var invoice = await invoiceAppService.CreateAsync(new CreateUpdateInvoiceDto
        {
            CustomerId = customer.Id,
            Status = InvoiceStatus.Issued,
            IssueDate = DateTime.UtcNow.AddDays(-10),
            DueDate = dueDate
        });

        await invoiceLineAppService.CreateAsync(new CreateUpdateInvoiceLineDto
        {
            InvoiceId = invoice.Id,
            Description = "Annual maintenance contract",
            UnitPrice = unitPrice,
            Quantity = 1
        });

        return invoice.Id;
    }

    [Fact]
    public async Task Invoice_With_No_Payments_And_Future_Due_Date_Is_Unpaid()
    {
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var invoiceId = await CreateInvoiceWithLineAsync(1000m, DateTime.UtcNow.AddDays(30));

        var invoice = await invoiceAppService.GetAsync(invoiceId);

        Assert.Equal(InvoicePaymentStatus.Unpaid, invoice.PaymentStatus);
    }

    [Fact]
    public async Task Invoice_With_No_Payments_Past_Due_Date_Is_Overdue()
    {
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var invoiceId = await CreateInvoiceWithLineAsync(1000m, DateTime.UtcNow.AddDays(-1));

        var invoice = await invoiceAppService.GetAsync(invoiceId);

        Assert.Equal(InvoicePaymentStatus.Overdue, invoice.PaymentStatus);
    }

    [Fact]
    public async Task Partial_Payment_Yields_PartiallyPaid_Status()
    {
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var paymentAppService = GetRequiredService<PaymentAppService>();
        var invoiceId = await CreateInvoiceWithLineAsync(1000m, DateTime.UtcNow.AddDays(30));

        await paymentAppService.CreateAsync(new CreateUpdatePaymentDto
        {
            InvoiceId = invoiceId,
            Amount = 400m,
            PaymentDate = DateTime.UtcNow
        });

        var invoice = await invoiceAppService.GetAsync(invoiceId);

        Assert.Equal(InvoicePaymentStatus.PartiallyPaid, invoice.PaymentStatus);
        Assert.Equal(400m, invoice.AmountPaid);
    }

    [Fact]
    public async Task Full_Payment_Yields_PaidInFull_Status()
    {
        var invoiceAppService = GetRequiredService<InvoiceAppService>();
        var paymentAppService = GetRequiredService<PaymentAppService>();
        var invoiceId = await CreateInvoiceWithLineAsync(1000m, DateTime.UtcNow.AddDays(30));

        await paymentAppService.CreateAsync(new CreateUpdatePaymentDto
        {
            InvoiceId = invoiceId,
            Amount = 1000m,
            PaymentDate = DateTime.UtcNow
        });

        var invoice = await invoiceAppService.GetAsync(invoiceId);

        Assert.Equal(InvoicePaymentStatus.PaidInFull, invoice.PaymentStatus);
    }
}
