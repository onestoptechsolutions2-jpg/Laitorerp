using System;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

// Shared by QuoteLineAppService/OrderLineAppService/InvoiceLineAppService to snapshot a line's
// TaxRateId/TaxRatePercent at add/edit time: an explicit choice on the line wins, otherwise fall
// back to the chosen Product's own TaxRateId, otherwise the system default TaxRate (IsDefault).
// Mirrors DocumentNumbering.NextAsync's role as a small static helper shared across line services.
public static class TaxRateResolver
{
    public static async Task<(Guid? TaxRateId, decimal TaxRatePercent)> ResolveAsync(
        IRepository<TaxRate, Guid> taxRateRepository,
        IRepository<Product, Guid> productRepository,
        Guid? explicitTaxRateId,
        Guid? productId)
    {
        var taxRateId = explicitTaxRateId;

        if (taxRateId == null && productId.HasValue)
        {
            var product = await productRepository.FindAsync(productId.Value);
            taxRateId = product?.TaxRateId;
        }

        if (taxRateId == null)
        {
            // Vat-scoped: a default WithholdingTax rate (see Entities/Sales/TaxType.cs) must never
            // leak in here - it only ever applies to VendorPayment, not sales lines.
            var defaultRate = (await taxRateRepository.GetListAsync(x => x.IsDefault && x.TaxType == TaxType.Vat)).FirstOrDefault();
            return (defaultRate?.Id, defaultRate?.Percent ?? 0);
        }

        var taxRate = await taxRateRepository.FindAsync(taxRateId.Value);
        return (taxRate?.Id, taxRate?.Percent ?? 0);
    }
}
