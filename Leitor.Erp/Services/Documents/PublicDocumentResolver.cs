using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Documents;
using Leitor.Erp.Entities.Opportunities;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services;
using Leitor.Erp.Services.Dtos.Opportunities;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Settings;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Documents;

public class PublicDocumentResult
{
    public byte[] PdfBytes { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public string CompanyName { get; init; } = string.Empty;
}

// Regenerates a document's PDF live from current data, given the (DocumentType, EntityId) pair a
// DocumentShareLink token resolves to. Deliberately talks to repositories directly rather than the
// staff AppServices (QuoteAppService etc.) - those enforce Sales.Default/Support.Default/etc.
// permissions that a public, unauthenticated link must never require, same "query repositories
// directly" convention Pages/Portal/Client/Index.cshtml.cs already established. Only the handful
// of DTO fields each *PdfDocument.Generate actually reads are filled in here - see each
// Documents/*PdfDocument.cs for the exact list - not a full AppService-style mapping.
public class PublicDocumentResolver : ITransientDependency
{
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<QuoteLine, Guid> _quoteLineRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderLine, Guid> _orderLineRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<PurchaseOrder, Guid> _purchaseOrderRepository;
    private readonly IRepository<PurchaseOrderLine, Guid> _purchaseOrderLineRepository;
    private readonly IRepository<Proposal, Guid> _proposalRepository;
    private readonly IRepository<Opportunity, Guid> _opportunityRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Vendor, Guid> _vendorRepository;
    private readonly ErpCompanyProfileProvider _companyProfileProvider;

    public PublicDocumentResolver(
        IRepository<Quote, Guid> quoteRepository,
        IRepository<QuoteLine, Guid> quoteLineRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderLine, Guid> orderLineRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<PurchaseOrder, Guid> purchaseOrderRepository,
        IRepository<PurchaseOrderLine, Guid> purchaseOrderLineRepository,
        IRepository<Proposal, Guid> proposalRepository,
        IRepository<Opportunity, Guid> opportunityRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Vendor, Guid> vendorRepository,
        ErpCompanyProfileProvider companyProfileProvider)
    {
        _quoteRepository = quoteRepository;
        _quoteLineRepository = quoteLineRepository;
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _paymentRepository = paymentRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseOrderLineRepository = purchaseOrderLineRepository;
        _proposalRepository = proposalRepository;
        _opportunityRepository = opportunityRepository;
        _customerRepository = customerRepository;
        _vendorRepository = vendorRepository;
        _companyProfileProvider = companyProfileProvider;
    }

    public async Task<PublicDocumentResult?> ResolveAsync(string documentType, Guid entityId)
    {
        var company = await _companyProfileProvider.GetAsync();

        return documentType switch
        {
            DocumentShareType.Quote => await ResolveQuoteAsync(entityId, company),
            DocumentShareType.Order => await ResolveOrderAsync(entityId, company),
            DocumentShareType.Invoice => await ResolveInvoiceAsync(entityId, company),
            DocumentShareType.PurchaseOrder => await ResolvePurchaseOrderAsync(entityId, company),
            DocumentShareType.Proposal => await ResolveProposalAsync(entityId, company),
            _ => null
        };
    }

    private async Task<PublicDocumentResult?> ResolveQuoteAsync(Guid id, ErpCompanyOptions company)
    {
        var quote = await _quoteRepository.FindAsync(id);
        if (quote == null)
        {
            return null;
        }

        var customer = await _customerRepository.GetAsync(quote.CustomerId);
        var lines = await _quoteLineRepository.GetListAsync(x => x.QuoteId == id);

        var dto = new QuoteDto
        {
            QuoteNumber = quote.QuoteNumber,
            Title = quote.Title,
            Status = quote.Status,
            IssueDate = quote.IssueDate,
            ExpiryDate = quote.ExpiryDate,
            Notes = quote.Notes,
            Subtotal = lines.Sum(x => x.Subtotal()),
            TaxAmount = lines.Sum(x => x.TaxAmount()),
            Total = lines.Sum(x => x.Total())
        };
        var lineDtos = lines.Select(x => new QuoteLineDto
        {
            Description = x.Description,
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity,
            DiscountPercent = x.DiscountPercent,
            LineTotal = x.Total()
        }).ToList();

        var pdfBytes = QuotePdfDocument.Generate(dto, lineDtos, customer, company);
        return new PublicDocumentResult
        {
            PdfBytes = pdfBytes,
            FileName = $"{quote.QuoteNumber}.pdf",
            Title = $"Quotation {quote.QuoteNumber}",
            Subtitle = customer.Name,
            CompanyName = company.Name
        };
    }

    private async Task<PublicDocumentResult?> ResolveOrderAsync(Guid id, ErpCompanyOptions company)
    {
        var order = await _orderRepository.FindAsync(id);
        if (order == null)
        {
            return null;
        }

        var customer = await _customerRepository.GetAsync(order.CustomerId);
        var lines = await _orderLineRepository.GetListAsync(x => x.OrderId == id);

        var dto = new OrderDto
        {
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            OrderDate = order.OrderDate,
            Notes = order.Notes,
            Subtotal = lines.Sum(x => x.Subtotal()),
            TaxAmount = lines.Sum(x => x.TaxAmount()),
            Total = lines.Sum(x => x.Total())
        };
        var lineDtos = lines.Select(x => new OrderLineDto
        {
            Description = x.Description,
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity,
            DiscountPercent = x.DiscountPercent,
            LineTotal = x.Total()
        }).ToList();

        var pdfBytes = OrderPdfDocument.Generate(dto, lineDtos, customer, company);
        return new PublicDocumentResult
        {
            PdfBytes = pdfBytes,
            FileName = $"{order.OrderNumber}.pdf",
            Title = $"Order {order.OrderNumber}",
            Subtitle = customer.Name,
            CompanyName = company.Name
        };
    }

    private async Task<PublicDocumentResult?> ResolveInvoiceAsync(Guid id, ErpCompanyOptions company)
    {
        var invoice = await _invoiceRepository.FindAsync(id);
        if (invoice == null)
        {
            return null;
        }

        var customer = await _customerRepository.GetAsync(invoice.CustomerId);
        var lines = await _invoiceLineRepository.GetListAsync(x => x.InvoiceId == id);
        var payments = await _paymentRepository.GetListAsync(x => x.InvoiceId == id);
        var amountPaid = payments.Sum(x => x.Amount);
        var total = lines.Sum(x => x.Total());

        var dto = new InvoiceDto
        {
            InvoiceNumber = invoice.InvoiceNumber,
            IssueDate = invoice.IssueDate,
            DueDate = invoice.DueDate,
            Notes = invoice.Notes,
            Subtotal = lines.Sum(x => x.Subtotal()),
            TaxAmount = lines.Sum(x => x.TaxAmount()),
            Total = total,
            AmountPaid = amountPaid,
            // Same 4-branch rule as PaymentStatusCalculator.Compute - inlined rather than
            // implementing IPayableDocument on a throwaway DTO just for this one call.
            PaymentStatus = amountPaid <= 0
                ? (invoice.DueDate < DateTime.Now ? InvoicePaymentStatus.Overdue : InvoicePaymentStatus.Unpaid)
                : amountPaid < total
                    ? InvoicePaymentStatus.PartiallyPaid
                    : (amountPaid > total ? InvoicePaymentStatus.Overpaid : InvoicePaymentStatus.PaidInFull)
        };
        var lineDtos = lines.Select(x => new InvoiceLineDto
        {
            Description = x.Description,
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity,
            DiscountPercent = x.DiscountPercent,
            LineTotal = x.Total()
        }).ToList();
        var paymentDtos = payments.Select(x => new PaymentDto
        {
            PaymentDate = x.PaymentDate,
            Amount = x.Amount,
            Method = x.Method,
            Reference = x.Reference
        }).ToList();

        var pdfBytes = InvoicePdfDocument.Generate(dto, lineDtos, paymentDtos, customer, company);
        return new PublicDocumentResult
        {
            PdfBytes = pdfBytes,
            FileName = $"{invoice.InvoiceNumber}.pdf",
            Title = $"Invoice {invoice.InvoiceNumber}",
            Subtitle = customer.Name,
            CompanyName = company.Name
        };
    }

    private async Task<PublicDocumentResult?> ResolvePurchaseOrderAsync(Guid id, ErpCompanyOptions company)
    {
        var purchaseOrder = await _purchaseOrderRepository.FindAsync(id);
        if (purchaseOrder == null)
        {
            return null;
        }

        var vendor = await _vendorRepository.GetAsync(purchaseOrder.VendorId);
        var lines = await _purchaseOrderLineRepository.GetListAsync(x => x.PurchaseOrderId == id);

        Customer? shipToCustomer = null;
        if (purchaseOrder.ShipToCustomer && purchaseOrder.SourceOrderId.HasValue)
        {
            var sourceOrder = await _orderRepository.FindAsync(purchaseOrder.SourceOrderId.Value);
            if (sourceOrder != null)
            {
                shipToCustomer = await _customerRepository.FindAsync(sourceOrder.CustomerId);
            }
        }

        var dto = new PurchaseOrderDto
        {
            PONumber = purchaseOrder.PONumber,
            Status = purchaseOrder.Status,
            OrderDate = purchaseOrder.OrderDate,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            Notes = purchaseOrder.Notes,
            ShipToCustomer = purchaseOrder.ShipToCustomer,
            Total = lines.Sum(x => x.Total())
        };
        var lineDtos = lines.Select(x => new PurchaseOrderLineDto
        {
            Description = x.Description,
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity,
            DiscountPercent = x.DiscountPercent,
            LineTotal = x.Total()
        }).ToList();

        var pdfBytes = PurchaseOrderPdfDocument.Generate(dto, lineDtos, vendor, company, shipToCustomer);
        return new PublicDocumentResult
        {
            PdfBytes = pdfBytes,
            FileName = $"{purchaseOrder.PONumber}.pdf",
            Title = $"Purchase Order {purchaseOrder.PONumber}",
            Subtitle = vendor.Name,
            CompanyName = company.Name
        };
    }

    private async Task<PublicDocumentResult?> ResolveProposalAsync(Guid id, ErpCompanyOptions company)
    {
        var proposal = await _proposalRepository.FindAsync(id);
        if (proposal == null)
        {
            return null;
        }

        var opportunity = await _opportunityRepository.GetAsync(proposal.OpportunityId);
        var customer = await _customerRepository.GetAsync(opportunity.CustomerId);

        var dto = new ProposalDto
        {
            ProposalNumber = proposal.ProposalNumber,
            Status = proposal.Status,
            Summary = proposal.Summary,
            ProposedSolution = proposal.ProposedSolution,
            Scope = proposal.Scope,
            Timeline = proposal.Timeline,
            Assumptions = proposal.Assumptions,
            Exclusions = proposal.Exclusions,
            WarrantyAndSupport = proposal.WarrantyAndSupport,
            Terms = proposal.Terms
        };

        var pdfBytes = ProposalPdfDocument.Generate(dto, customer, company);
        return new PublicDocumentResult
        {
            PdfBytes = pdfBytes,
            FileName = $"{proposal.ProposalNumber}.pdf",
            Title = $"Proposal {proposal.ProposalNumber}",
            Subtitle = customer.Name,
            CompanyName = company.Name
        };
    }
}
