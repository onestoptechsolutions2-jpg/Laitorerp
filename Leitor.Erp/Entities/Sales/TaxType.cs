namespace Leitor.Erp.Entities.Sales;

// VAT rates apply to Quote/Order/Invoice lines (and, approximately, to Procurement input VAT -
// see Services/Tax/VatReturnAppService.cs); WithholdingTax rates apply only to VendorPayment (see
// Entities/Procurement/Vendor.WithholdingTaxRateId). Kept as one TaxRate table with a type flag
// rather than two separate tables since both are just "a named percentage" - TaxRateAppService
// scopes its IsDefault-per-type enforcement off this.
public enum TaxType
{
    Vat = 0,
    WithholdingTax = 1
}
