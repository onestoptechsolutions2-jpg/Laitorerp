using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Sales;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class PaymentAppService :
    CrudAppService<Payment, PaymentDto, Guid, GetPaymentListInput, CreateUpdatePaymentDto>
{
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;

    public PaymentAppService(
        IRepository<Payment, Guid> repository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository)
        : base(repository)
    {
        _invoiceRepository = invoiceRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        GetPolicyName = ErpPermissions.Sales.Default;
        GetListPolicyName = ErpPermissions.Sales.Default;
        CreatePolicyName = ErpPermissions.Sales.Edit;
        UpdatePolicyName = ErpPermissions.Sales.Edit;
        DeletePolicyName = ErpPermissions.Sales.Edit;
    }

    protected override async Task<IQueryable<Payment>> CreateFilteredQueryAsync(GetPaymentListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.InvoiceId.HasValue, x => x.InvoiceId == input.InvoiceId!.Value);
    }

    protected override async Task<Payment> MapToEntityAsync(CreateUpdatePaymentDto createInput)
    {
        var entity = new Payment(GuidGenerator.Create(), createInput.InvoiceId, createInput.Amount, createInput.PaymentDate);
        CopyToEntity(createInput, entity);

        // CurrencyCode is optional on the DTO - defaults from the parent Invoice when the caller
        // doesn't specify one (the common case: paid in the same currency it was billed in).
        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            var invoice = await _invoiceRepository.GetAsync(createInput.InvoiceId);
            entity.CurrencyCode = invoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdatePaymentDto updateInput, Payment entity)
    {
        CopyToEntity(updateInput, entity);

        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            var invoice = await _invoiceRepository.GetAsync(updateInput.InvoiceId);
            entity.CurrencyCode = invoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate);
    }

    private static void CopyToEntity(CreateUpdatePaymentDto input, Payment entity)
    {
        entity.InvoiceId = input.InvoiceId;
        entity.Amount = input.Amount;
        entity.PaymentDate = input.PaymentDate;
        entity.Method = input.Method;
        entity.Reference = input.Reference;
        entity.Notes = input.Notes;
        entity.CurrencyCode = input.CurrencyCode ?? string.Empty;
    }
}
