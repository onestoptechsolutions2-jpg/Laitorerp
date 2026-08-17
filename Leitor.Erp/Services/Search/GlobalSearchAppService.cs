using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Search;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Search;

// The app has no way to find a record without already knowing which of ~30 modules owns it and
// navigating to that module's own list/filter - flagged in a 2026-08-17 usability audit as the
// single highest-friction gap for a user who doesn't have the nav memorized. Deliberately scoped
// to the 4 entity types a user is most likely to be hunting for by name/number day-to-day
// (Customer, Lead, Ticket, Invoice) rather than every entity in the app - can grow later, this is
// meant to close the biggest gap cheaply, not be an exhaustive search index.
//
// Each entity type's results are gated on that module's own view permission, same as the module's
// own Index page would be - a user never sees a search result they couldn't already reach by
// navigating there directly.
public class GlobalSearchAppService : ApplicationService
{
    private const int MaxResultsPerType = 5;

    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IRepository<Ticket, Guid> _ticketRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;

    public GlobalSearchAppService(
        IRepository<Customer, Guid> customerRepository,
        IRepository<Lead, Guid> leadRepository,
        IRepository<Ticket, Guid> ticketRepository,
        IRepository<Invoice, Guid> invoiceRepository)
    {
        _customerRepository = customerRepository;
        _leadRepository = leadRepository;
        _ticketRepository = ticketRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<List<GlobalSearchResultDto>> SearchAsync(string term)
    {
        var results = new List<GlobalSearchResultDto>();

        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
        {
            return results;
        }

        term = term.Trim();

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Customers.Default))
        {
            var customers = (await _customerRepository.GetListAsync(x =>
                    x.Name.Contains(term) || (x.PhoneNumber != null && x.PhoneNumber.Contains(term))))
                .Take(MaxResultsPerType);
            results.AddRange(customers.Select(x => new GlobalSearchResultDto
            {
                EntityType = "Customer",
                Title = x.Name,
                Subtitle = x.PhoneNumber ?? string.Empty,
                Url = $"/Customers/Detail?id={x.Id}"
            }));
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Leads.Default))
        {
            var leads = (await _leadRepository.GetListAsync(x =>
                    x.Name.Contains(term) ||
                    (x.CompanyName != null && x.CompanyName.Contains(term)) ||
                    (x.Phone != null && x.Phone.Contains(term))))
                .Take(MaxResultsPerType);
            results.AddRange(leads.Select(x => new GlobalSearchResultDto
            {
                EntityType = "Lead",
                Title = x.Name,
                Subtitle = x.CompanyName ?? x.Phone ?? string.Empty,
                Url = $"/Leads/Detail?id={x.Id}"
            }));
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Support.Default))
        {
            var tickets = (await _ticketRepository.GetListAsync(x =>
                    x.TicketNumber.Contains(term) || x.Subject.Contains(term)))
                .Take(MaxResultsPerType)
                .ToList();
            var ticketCustomerNames = await GetCustomerNamesAsync(tickets.Select(x => x.CustomerId));
            results.AddRange(tickets.Select(x => new GlobalSearchResultDto
            {
                EntityType = "Ticket",
                Title = $"{x.TicketNumber} - {x.Subject}",
                Subtitle = ticketCustomerNames.GetValueOrDefault(x.CustomerId, string.Empty),
                Url = $"/Support/Tickets/Detail?id={x.Id}"
            }));
        }

        if (await AuthorizationService.IsGrantedAsync(ErpPermissions.Sales.Default))
        {
            var invoices = (await _invoiceRepository.GetListAsync(x => x.InvoiceNumber.Contains(term)))
                .Take(MaxResultsPerType)
                .ToList();
            var invoiceCustomerNames = await GetCustomerNamesAsync(invoices.Select(x => x.CustomerId));
            results.AddRange(invoices.Select(x => new GlobalSearchResultDto
            {
                EntityType = "Invoice",
                Title = x.InvoiceNumber,
                Subtitle = invoiceCustomerNames.GetValueOrDefault(x.CustomerId, string.Empty),
                Url = $"/Sales/Invoices/Detail?id={x.Id}"
            }));
        }

        return results;
    }

    private async Task<Dictionary<Guid, string>> GetCustomerNamesAsync(IEnumerable<Guid> customerIds)
    {
        var ids = customerIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return (await _customerRepository.GetListAsync(x => ids.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => x.Name);
    }
}
