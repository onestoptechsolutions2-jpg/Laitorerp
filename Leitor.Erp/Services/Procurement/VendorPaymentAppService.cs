using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Accounting;
using Leitor.Erp.Entities.Procurement;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Accounting;
using Leitor.Erp.Services.Dtos.Procurement;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Procurement;

public class VendorPaymentAppService :
    CrudAppService<VendorPayment, VendorPaymentDto, Guid, GetVendorPaymentListInput, CreateUpdateVendorPaymentDto>
{
    private readonly IRepository<SupplierInvoice, Guid> _supplierInvoiceRepository;
    private readonly IRepository<Currency, Guid> _currencyRepository;
    private readonly IRepository<ExchangeRate, Guid> _exchangeRateRepository;

    public VendorPaymentAppService(
        IRepository<VendorPayment, Guid> repository,
        IRepository<SupplierInvoice, Guid> supplierInvoiceRepository,
        IRepository<Currency, Guid> currencyRepository,
        IRepository<ExchangeRate, Guid> exchangeRateRepository)
        : base(repository)
    {
        _supplierInvoiceRepository = supplierInvoiceRepository;
        _currencyRepository = currencyRepository;
        _exchangeRateRepository = exchangeRateRepository;
        GetPolicyName = ErpPermissions.Procurement.Default;
        GetListPolicyName = ErpPermissions.Procurement.Default;
        CreatePolicyName = ErpPermissions.Procurement.Edit;
        UpdatePolicyName = ErpPermissions.Procurement.Edit;
        DeletePolicyName = ErpPermissions.Procurement.Edit;
    }

    protected override async Task<IQueryable<VendorPayment>> CreateFilteredQueryAsync(GetVendorPaymentListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);
        return query.WhereIf(input.SupplierInvoiceId.HasValue, x => x.SupplierInvoiceId == input.SupplierInvoiceId!.Value);
    }

    protected override async Task<VendorPayment> MapToEntityAsync(CreateUpdateVendorPaymentDto createInput)
    {
        var entity = new VendorPayment(GuidGenerator.Create(), createInput.SupplierInvoiceId, createInput.Amount, createInput.PaymentDate);
        CopyToEntity(createInput, entity);

        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            var supplierInvoice = await _supplierInvoiceRepository.GetAsync(createInput.SupplierInvoiceId);
            entity.CurrencyCode = supplierInvoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate);
        return entity;
    }

    protected override async Task MapToEntityAsync(CreateUpdateVendorPaymentDto updateInput, VendorPayment entity)
    {
        CopyToEntity(updateInput, entity);

        if (string.IsNullOrWhiteSpace(entity.CurrencyCode))
        {
            var supplierInvoice = await _supplierInvoiceRepository.GetAsync(updateInput.SupplierInvoiceId);
            entity.CurrencyCode = supplierInvoice.CurrencyCode;
        }

        entity.ExchangeRateToBase = await CurrencyRateResolver.ResolveAsync(
            _currencyRepository, _exchangeRateRepository, entity.CurrencyCode, entity.PaymentDate);
    }

    private static void CopyToEntity(CreateUpdateVendorPaymentDto input, VendorPayment entity)
    {
        entity.SupplierInvoiceId = input.SupplierInvoiceId;
        entity.Amount = input.Amount;
        entity.PaymentDate = input.PaymentDate;
        entity.Method = input.Method;
        entity.Reference = input.Reference;
        entity.Notes = input.Notes;
        entity.CurrencyCode = input.CurrencyCode ?? string.Empty;
    }
}
