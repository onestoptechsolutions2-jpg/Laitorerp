using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.FieldService;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Sales;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Leitor.Erp.BackgroundWorkers;

// Runs once daily: finds every non-Milestone Order that's Confirmed/Fulfilled, not yet invoiced,
// and whose linked FieldServiceJobs (if any) are all Completed - the exact condition
// MyWorkspaceAppService.LoadOrdersReadyToInvoiceAsync already surfaces as a read-only notice - and
// actually issues the invoice, closing the gap the 2026-08-18 consolidation review flagged: a
// completed job never triggered billing on its own, it only made a button appear next time someone
// happened to revisit the Order page. Same shape as ContractRecurringBillingWorker (see that file's
// own comment for why a background worker posts directly against repositories rather than going
// through OrderAppService.ConvertToInvoiceAsync - no authenticated CurrentUser to satisfy
// CheckCreatePolicyAsync); this duplicates that method's guts rather than sharing them, matching
// the same precedent.
//
// Deliberately scoped to non-Milestone orders only. Milestone billing decides *which percentage*
// to invoice next (CreateInvoiceForMilestoneAsync's own tax-blending rules) - a judgment call this
// worker doesn't make. Milestone orders keep working exactly as before: the existing "Orders Ready
// to Invoice" notice on My Workspace still flags them, and "Issue Final Invoice" is still a manual
// click for that path.
public class OrderReadyToInvoiceWorker : AsyncPeriodicBackgroundWorkerBase
{
    public OrderReadyToInvoiceWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromHours(24).TotalMilliseconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var orderRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Order, Guid>>();
        var orderLineRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<OrderLine, Guid>>();
        var jobRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<FieldServiceJob, Guid>>();
        var invoiceRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Invoice, Guid>>();
        var invoiceLineRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<InvoiceLine, Guid>>();
        var paymentRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Payment, Guid>>();
        var customerRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Customer, Guid>>();
        var currencyRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Currency, Guid>>();
        var exchangeRateRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<ExchangeRate, Guid>>();
        var accountRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Account, Guid>>();
        var journalEntryRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<JournalEntry, Guid>>();
        var journalEntryLineRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<JournalEntryLine, Guid>>();
        var fiscalPeriodRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<FiscalPeriod, Guid>>();
        var guidGenerator = workerContext.ServiceProvider.GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var dataFilter = workerContext.ServiceProvider.GetRequiredService<IDataFilter>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
        var logger = workerContext.ServiceProvider.GetRequiredService<ILogger<OrderReadyToInvoiceWorker>>();

        var candidateOrders = await orderRepository.GetListAsync(x =>
            x.PaymentTerms != PaymentTerms.Milestone &&
            (x.Status == OrderStatus.Confirmed || x.Status == OrderStatus.Fulfilled));

        if (candidateOrders.Count == 0)
        {
            return;
        }

        var orderIds = candidateOrders.Select(x => x.Id).ToList();

        var alreadyInvoicedOrderIds = (await invoiceRepository.GetListAsync(x => x.OrderId.HasValue && orderIds.Contains(x.OrderId.Value)))
            .Select(x => x.OrderId!.Value)
            .ToHashSet();

        var jobsByOrderId = (await jobRepository.GetListAsync(x => x.OrderId != null && orderIds.Contains(x.OrderId.Value)))
            .GroupBy(x => x.OrderId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var readyOrders = candidateOrders.Where(order =>
        {
            if (alreadyInvoicedOrderIds.Contains(order.Id))
            {
                return false;
            }

            var orderJobs = jobsByOrderId.GetValueOrDefault(order.Id);
            return orderJobs == null || orderJobs.All(x => x.Status == FieldServiceJobStatus.Completed);
        }).ToList();

        foreach (var order in readyOrders)
        {
            try
            {
                var orderLines = await orderLineRepository.GetListAsync(x => x.OrderId == order.Id);
                if (orderLines.Count == 0)
                {
                    continue;
                }

                var total = orderLines.Sum(x => x.Total());
                await CreditCheck.EnsureWithinLimitAsync(customerRepository, invoiceRepository, invoiceLineRepository, paymentRepository, order.CustomerId, total);

                var invoiceNumber = await DocumentNumbering.NextAsync(invoiceRepository, dataFilter, "INV-");
                var issueDate = clock.Now;

                var invoice = new Invoice(guidGenerator.Create(), order.CustomerId, invoiceNumber)
                {
                    OrderId = order.Id,
                    Status = InvoiceStatus.Issued,
                    IssueDate = issueDate,
                    DueDate = PaymentTermsCalculator.DueDate(issueDate, order.PaymentTerms),
                    Notes = order.Notes,
                    PaymentTerms = order.PaymentTerms,
                    CurrencyCode = order.CurrencyCode,
                    SalespersonUserId = order.SalespersonUserId,
                    ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(currencyRepository, exchangeRateRepository, order.CurrencyCode, issueDate, throwIfNotFound: false)
                };
                await invoiceRepository.InsertAsync(invoice, autoSave: true);

                foreach (var orderLine in orderLines)
                {
                    var invoiceLine = new InvoiceLine(guidGenerator.Create(), invoice.Id, orderLine.Description, orderLine.UnitPrice)
                    {
                        ProductId = orderLine.ProductId,
                        Quantity = orderLine.Quantity,
                        DiscountPercent = orderLine.DiscountPercent,
                        TaxRateId = orderLine.TaxRateId,
                        TaxRatePercent = orderLine.TaxRatePercent
                    };
                    await invoiceLineRepository.InsertAsync(invoiceLine, autoSave: true);
                }

                await JournalPostingService.PostAsync(
                    accountRepository, journalEntryRepository, journalEntryLineRepository, fiscalPeriodRepository, guidGenerator, dataFilter,
                    invoice.IssueDate, JournalPostingService.SourceDocumentTypes.Invoice, invoice.Id,
                    $"Invoice {invoice.InvoiceNumber}",
                    SystemAccountRole.AccountsReceivable, SystemAccountRole.Revenue,
                    total, invoice.CurrencyCode, invoice.ExchangeRateToBase, order.ProjectId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Order {OrderId} ({OrderNumber}) is ready to invoice but could not be auto-invoiced - will retry tomorrow.",
                    order.Id, order.OrderNumber);
            }
        }
    }
}
