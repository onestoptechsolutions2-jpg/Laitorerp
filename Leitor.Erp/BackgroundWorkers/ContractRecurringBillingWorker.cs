using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Customers;
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

// Runs once daily: for each CustomerContract with BillingFrequency set and NextBillingDate due,
// issues a real Invoice for the contract's Value and advances NextBillingDate. Same shape as
// RecurringJournalWorker (see that file's own comment for why a background worker posts directly
// rather than going through an AppService - no authenticated CurrentUser to satisfy
// CheckPolicyAsync) but produces a customer-facing Invoice instead of a GL-only journal entry.
// A contract whose Customer/Value has gone missing since is skipped and logged, not crashed on -
// picked up again tomorrow once fixed, same resilience RecurringJournalWorker already has.
public class ContractRecurringBillingWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ContractRecurringBillingWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromHours(24).TotalMilliseconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var contractRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<CustomerContract, Guid>>();
        var customerRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Customer, Guid>>();
        var invoiceRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Invoice, Guid>>();
        var invoiceLineRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<InvoiceLine, Guid>>();
        var accountRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Account, Guid>>();
        var journalEntryRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<JournalEntry, Guid>>();
        var journalEntryLineRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<JournalEntryLine, Guid>>();
        var fiscalPeriodRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<FiscalPeriod, Guid>>();
        var currencyRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Currency, Guid>>();
        var exchangeRateRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<ExchangeRate, Guid>>();
        var guidGenerator = workerContext.ServiceProvider.GetRequiredService<Volo.Abp.Guids.IGuidGenerator>();
        var dataFilter = workerContext.ServiceProvider.GetRequiredService<IDataFilter>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
        var logger = workerContext.ServiceProvider.GetRequiredService<ILogger<ContractRecurringBillingWorker>>();

        var now = clock.Now;
        var dueContracts = await contractRepository.GetListAsync(x =>
            x.BillingFrequency != RecurringBillingFrequency.None &&
            x.Status == CustomerContractStatus.Active &&
            x.NextBillingDate != null && x.NextBillingDate <= now);

        foreach (var contract in dueContracts)
        {
            if (!contract.Value.HasValue || contract.Value.Value <= 0)
            {
                logger.LogWarning("Contract {ContractId} ({ContractNumber}) is due for recurring billing but has no Value set - skipping, will retry tomorrow.",
                    contract.Id, contract.ContractNumber);
                continue;
            }

            try
            {
                var customer = await customerRepository.GetAsync(contract.CustomerId);
                var currencyCode = customer.DefaultCurrencyCode;
                if (string.IsNullOrWhiteSpace(currencyCode))
                {
                    var baseCurrency = (await currencyRepository.GetListAsync(x => x.IsBaseCurrency)).FirstOrDefault();
                    currencyCode = baseCurrency?.Code ?? "KES";
                }

                var invoiceNumber = await DocumentNumbering.NextAsync(invoiceRepository, dataFilter, "INV-");
                var issueDate = contract.NextBillingDate!.Value;

                var invoice = new Invoice(guidGenerator.Create(), contract.CustomerId, invoiceNumber)
                {
                    Status = InvoiceStatus.Issued,
                    IssueDate = issueDate,
                    DueDate = PaymentTermsCalculator.DueDate(issueDate, customer.DefaultPaymentTerms),
                    Notes = $"Recurring billing - {contract.Title} ({contract.ContractNumber})",
                    PaymentTerms = customer.DefaultPaymentTerms,
                    CurrencyCode = currencyCode,
                    ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(currencyRepository, exchangeRateRepository, currencyCode, issueDate, throwIfNotFound: false)
                };
                await invoiceRepository.InsertAsync(invoice, autoSave: true);

                var invoiceLine = new InvoiceLine(guidGenerator.Create(), invoice.Id, $"{contract.Title} - {FrequencyLabel(contract.BillingFrequency)} billing", contract.Value.Value);
                await invoiceLineRepository.InsertAsync(invoiceLine, autoSave: true);

                await JournalPostingService.PostAsync(
                    accountRepository, journalEntryRepository, journalEntryLineRepository, fiscalPeriodRepository, guidGenerator, dataFilter,
                    invoice.IssueDate, JournalPostingService.SourceDocumentTypes.Invoice, invoice.Id,
                    $"Invoice {invoice.InvoiceNumber}",
                    SystemAccountRole.AccountsReceivable, SystemAccountRole.Revenue,
                    contract.Value.Value, invoice.CurrencyCode, invoice.ExchangeRateToBase);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Contract {ContractId} ({ContractNumber}) could not be billed for {NextBillingDate} - will retry tomorrow.",
                    contract.Id, contract.ContractNumber, contract.NextBillingDate);
                continue;
            }

            contract.LastBilledDate = contract.NextBillingDate;
            contract.NextBillingDate = contract.BillingFrequency switch
            {
                RecurringBillingFrequency.Monthly => contract.NextBillingDate!.Value.AddMonths(1),
                RecurringBillingFrequency.Quarterly => contract.NextBillingDate!.Value.AddMonths(3),
                RecurringBillingFrequency.Annually => contract.NextBillingDate!.Value.AddYears(1),
                _ => contract.NextBillingDate!.Value.AddMonths(1)
            };
            await contractRepository.UpdateAsync(contract, autoSave: true);
        }
    }

    private static string FrequencyLabel(RecurringBillingFrequency frequency) => frequency switch
    {
        RecurringBillingFrequency.Monthly => "Monthly",
        RecurringBillingFrequency.Quarterly => "Quarterly",
        RecurringBillingFrequency.Annually => "Annual",
        _ => "Recurring"
    };
}
