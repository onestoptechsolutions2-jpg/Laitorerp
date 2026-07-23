using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Services.Accounting;

// Called from PaymentAppService/VendorPaymentAppService/OrderAppService/InvoiceAppService/
// SupplierInvoiceAppService to auto-post a balanced two-line JournalEntry the moment a document
// becomes final (issued/paid). Same static-method-with-injected-deps shape as
// WorkflowStageLog/DeletionGate, so callers don't need a dependency on a stateful service just to
// post an entry. Always Dr the first role / Cr the second - callers pick which role goes where.
public static class JournalPostingService
{
    public static class SourceDocumentTypes
    {
        public const string Invoice = "Invoice";
        public const string Payment = "Payment";
        public const string SupplierInvoice = "SupplierInvoice";
        public const string VendorPayment = "VendorPayment";
        public const string Order = "Order";
        public const string PosSale = "PosSale";
    }

    public static async Task<bool> IsAlreadyPostedAsync(
        IRepository<JournalEntry, Guid> journalEntryRepository,
        string sourceDocumentType,
        Guid sourceDocumentId)
    {
        return (await journalEntryRepository.GetListAsync(
            x => x.SourceDocumentType == sourceDocumentType && x.SourceDocumentId == sourceDocumentId)).Any();
    }

    public static async Task PostAsync(
        IRepository<Account, Guid> accountRepository,
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IGuidGenerator guidGenerator,
        IDataFilter dataFilter,
        DateTime entryDate,
        string sourceDocumentType,
        Guid sourceDocumentId,
        string description,
        SystemAccountRole debitRole,
        SystemAccountRole creditRole,
        decimal amount,
        string currencyCode,
        decimal exchangeRateToBase,
        Guid? projectId = null)
    {
        if (amount <= 0)
        {
            return;
        }

        var debitAccount = await ResolveSystemAccountAsync(accountRepository, debitRole);
        var creditAccount = await ResolveSystemAccountAsync(accountRepository, creditRole);

        await PostByAccountIdAsync(
            journalEntryRepository, journalEntryLineRepository, fiscalPeriodRepository, guidGenerator, dataFilter,
            entryDate, sourceDocumentType, sourceDocumentId, description,
            debitAccount.Id, creditAccount.Id, amount, currencyCode, exchangeRateToBase, projectId);
    }

    // Same two-line posting shape as PostAsync, but for callers that already know exactly which
    // Accounts to use (e.g. DepreciationAppService, where the debit/credit accounts are picked
    // per-FixedAsset, not a single global SystemAccountRole) rather than resolving a role.
    public static async Task PostByAccountIdAsync(
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IGuidGenerator guidGenerator,
        IDataFilter dataFilter,
        DateTime entryDate,
        string sourceDocumentType,
        Guid sourceDocumentId,
        string description,
        Guid debitAccountId,
        Guid creditAccountId,
        decimal amount,
        string currencyCode,
        decimal exchangeRateToBase,
        Guid? projectId = null)
    {
        if (amount <= 0)
        {
            return;
        }

        await FiscalPeriodGuard.EnsureNotLockedAsync(fiscalPeriodRepository, entryDate);

        var entryNumber = await DocumentNumbering.NextAsync(journalEntryRepository, dataFilter, "JE-");
        var entry = new JournalEntry(guidGenerator.Create(), entryNumber, entryDate, description)
        {
            SourceDocumentType = sourceDocumentType,
            SourceDocumentId = sourceDocumentId,
            IsSystemGenerated = true
        };
        await journalEntryRepository.InsertAsync(entry, autoSave: true);

        await journalEntryLineRepository.InsertAsync(
            new JournalEntryLine(guidGenerator.Create(), entry.Id, debitAccountId)
            {
                Debit = amount,
                CurrencyCode = currencyCode,
                ExchangeRateToBase = exchangeRateToBase,
                ProjectId = projectId
            },
            autoSave: true);

        await journalEntryLineRepository.InsertAsync(
            new JournalEntryLine(guidGenerator.Create(), entry.Id, creditAccountId)
            {
                Credit = amount,
                CurrencyCode = currencyCode,
                ExchangeRateToBase = exchangeRateToBase,
                ProjectId = projectId
            },
            autoSave: true);
    }

    // For callers with more than 2 lines (RecurringJournalWorker, CurrencyRevaluationAppService)
    // that also can't go through JournalEntryAppService.CreateAsync - a background worker has no
    // authenticated CurrentUser, so CheckPolicyAsync there would always throw. Validates the
    // entry balances in base-currency terms first, same check CreateAsync itself makes.
    public static async Task PostMultiLineAsync(
        IRepository<JournalEntry, Guid> journalEntryRepository,
        IRepository<JournalEntryLine, Guid> journalEntryLineRepository,
        IRepository<FiscalPeriod, Guid> fiscalPeriodRepository,
        IGuidGenerator guidGenerator,
        IDataFilter dataFilter,
        DateTime entryDate,
        string sourceDocumentType,
        Guid sourceDocumentId,
        string description,
        IReadOnlyList<MultiLineEntry> lines)
    {
        if (lines.Count < 2)
        {
            throw new UserFriendlyException("A journal entry needs at least two lines.");
        }

        var totalDebitBase = lines.Sum(x => x.Debit * x.ExchangeRateToBase);
        var totalCreditBase = lines.Sum(x => x.Credit * x.ExchangeRateToBase);
        if (Math.Round(totalDebitBase - totalCreditBase, 2) != 0)
        {
            throw new UserFriendlyException(
                $"This entry does not balance: debits total {totalDebitBase:N2} but credits total {totalCreditBase:N2} (in base currency).");
        }

        await FiscalPeriodGuard.EnsureNotLockedAsync(fiscalPeriodRepository, entryDate);

        var entryNumber = await DocumentNumbering.NextAsync(journalEntryRepository, dataFilter, "JE-");
        var entry = new JournalEntry(guidGenerator.Create(), entryNumber, entryDate, description)
        {
            SourceDocumentType = sourceDocumentType,
            SourceDocumentId = sourceDocumentId,
            IsSystemGenerated = true
        };
        await journalEntryRepository.InsertAsync(entry, autoSave: true);

        foreach (var line in lines)
        {
            await journalEntryLineRepository.InsertAsync(
                new JournalEntryLine(guidGenerator.Create(), entry.Id, line.AccountId)
                {
                    Debit = line.Debit,
                    Credit = line.Credit,
                    CurrencyCode = line.CurrencyCode,
                    ExchangeRateToBase = line.ExchangeRateToBase
                },
                autoSave: true);
        }
    }

    public readonly struct MultiLineEntry
    {
        public MultiLineEntry(Guid accountId, decimal debit, decimal credit, string currencyCode, decimal exchangeRateToBase)
        {
            AccountId = accountId;
            Debit = debit;
            Credit = credit;
            CurrencyCode = currencyCode;
            ExchangeRateToBase = exchangeRateToBase;
        }

        public Guid AccountId { get; }
        public decimal Debit { get; }
        public decimal Credit { get; }
        public string CurrencyCode { get; }
        public decimal ExchangeRateToBase { get; }
    }

    private static async Task<Account> ResolveSystemAccountAsync(IRepository<Account, Guid> accountRepository, SystemAccountRole role)
    {
        var account = (await accountRepository.GetListAsync(x => x.SystemRole == role)).FirstOrDefault();
        if (account == null)
        {
            throw new UserFriendlyException(
                $"No account is configured with the \"{role}\" role yet - set one on the Chart of Accounts page first.");
        }

        return account;
    }
}
