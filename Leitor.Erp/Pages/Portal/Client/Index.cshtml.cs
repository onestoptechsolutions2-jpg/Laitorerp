using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Pages.Portal.Client;

// Client Portal: a single consolidated page, not a mirrored set of staff CRUD pages - matches the
// actual need ("where is my order right now," at a glance). Deliberately talks to repositories
// directly rather than the staff AppServices: those enforce module-wide Default/Create/Edit
// permissions that a portal login must never hold (holding Sales.Default, for example, would let
// a client browse /Sales/Orders and see every other customer's orders too). The only
// authorization here is the PortalUserId linkage itself, checked once in OnGetAsync and re-checked
// before every write.
[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<OrderLine, Guid> _orderLineRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<InvoiceLine, Guid> _invoiceLineRepository;
    private readonly IRepository<Payment, Guid> _paymentRepository;
    private readonly IRepository<CustomerContract, Guid> _contractRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<TicketMessage, Guid> _ticketMessageRepository;
    private readonly IDataFilter _dataFilter;
    private readonly IGuidGenerator _guidGenerator;

    public IndexModel(
        IRepository<Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository,
        IRepository<OrderLine, Guid> orderLineRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<InvoiceLine, Guid> invoiceLineRepository,
        IRepository<Payment, Guid> paymentRepository,
        IRepository<CustomerContract, Guid> contractRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<TicketMessage, Guid> ticketMessageRepository,
        IDataFilter dataFilter,
        IGuidGenerator guidGenerator)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _orderLineRepository = orderLineRepository;
        _invoiceRepository = invoiceRepository;
        _invoiceLineRepository = invoiceLineRepository;
        _paymentRepository = paymentRepository;
        _contractRepository = contractRepository;
        _ticketRepository = ticketRepository;
        _ticketMessageRepository = ticketMessageRepository;
        _dataFilter = dataFilter;
        _guidGenerator = guidGenerator;
    }

    public bool IsLinked { get; set; }
    public Customer Customer { get; set; } = null!;
    public IReadOnlyList<Order> Orders { get; set; } = Array.Empty<Order>();
    public Dictionary<Guid, decimal> OrderTotals { get; set; } = new();
    public IReadOnlyList<Invoice> Invoices { get; set; } = Array.Empty<Invoice>();
    public Dictionary<Guid, decimal> InvoiceTotals { get; set; } = new();
    public Dictionary<Guid, decimal> InvoiceAmountsPaid { get; set; } = new();
    public IReadOnlyList<CustomerContract> Contracts { get; set; } = Array.Empty<CustomerContract>();
    public IReadOnlyList<Ticket> Tickets { get; set; } = Array.Empty<Ticket>();

    [BindProperty]
    public string NewTicketSubject { get; set; } = string.Empty;

    [BindProperty]
    public string? NewTicketMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    private async Task<Customer?> ResolveLinkedCustomerAsync()
    {
        if (!CurrentUser.Id.HasValue)
        {
            return null;
        }

        return (await _customerRepository.GetListAsync(x => x.PortalUserId == CurrentUser.Id.Value)).FirstOrDefault();
    }

    private async Task LoadAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null)
        {
            IsLinked = false;
            return;
        }

        IsLinked = true;
        Customer = customer;

        Orders = (await _orderRepository.GetListAsync(x => x.CustomerId == customer.Id))
            .OrderByDescending(x => x.OrderDate)
            .ToList();
        var orderIds = Orders.Select(x => x.Id).ToList();
        var orderLines = orderIds.Count > 0 ? await _orderLineRepository.GetListAsync(x => orderIds.Contains(x.OrderId)) : new List<OrderLine>();
        OrderTotals = orderLines
            .GroupBy(x => x.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m)));

        Invoices = (await _invoiceRepository.GetListAsync(x => x.CustomerId == customer.Id))
            .OrderByDescending(x => x.IssueDate)
            .ToList();
        var invoiceIds = Invoices.Select(x => x.Id).ToList();
        var invoiceLines = invoiceIds.Count > 0 ? await _invoiceLineRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId)) : new List<InvoiceLine>();
        InvoiceTotals = invoiceLines
            .GroupBy(x => x.InvoiceId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.UnitPrice * x.Quantity * (1 - x.DiscountPercent / 100m)));
        var payments = invoiceIds.Count > 0 ? await _paymentRepository.GetListAsync(x => invoiceIds.Contains(x.InvoiceId)) : new List<Payment>();
        InvoiceAmountsPaid = payments
            .GroupBy(x => x.InvoiceId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        Contracts = (await _contractRepository.GetListAsync(x => x.CustomerId == customer.Id))
            .OrderByDescending(x => x.EndDate)
            .ToList();

        Tickets = (await _ticketRepository.GetListAsync(x => x.CustomerId == customer.Id))
            .OrderByDescending(x => x.CreationTime)
            .ToList();
    }

    public async Task<IActionResult> OnPostRaiseTicketAsync()
    {
        var customer = await ResolveLinkedCustomerAsync();
        if (customer == null || string.IsNullOrWhiteSpace(NewTicketSubject))
        {
            return RedirectToPage();
        }

        var ticketNumber = await DocumentNumbering.NextAsync(_ticketRepository, _dataFilter, "TKT-");
        var ticket = new Ticket(_guidGenerator.Create(), customer.Id, ticketNumber, NewTicketSubject);
        await _ticketRepository.InsertAsync(ticket, autoSave: true);

        if (!string.IsNullOrWhiteSpace(NewTicketMessage))
        {
            var message = new TicketMessage(_guidGenerator.Create(), ticket.Id, isCustomerMessage: true, NewTicketMessage);
            await _ticketMessageRepository.InsertAsync(message, autoSave: true);
        }

        return RedirectToPage();
    }
}
