namespace Leitor.Erp.Entities.Sales;

// Only meaningful on a PriceListItem where ServiceCatalogItemId is set (Product rows are
// implicitly Fixed, no behavior change there) - distinguishes a standard flat service price from
// an hourly/callout billing rate. A "per scope" custom job doesn't need either: that's just a
// manual Quote/Order line with no Product/ServiceCatalogItem reference at all, already supported
// with zero changes. Not every client is on a retainer - some want one-off callouts billed
// hourly, which this exists to support.
public enum RateType
{
    Fixed = 0,
    Hourly = 1
}
