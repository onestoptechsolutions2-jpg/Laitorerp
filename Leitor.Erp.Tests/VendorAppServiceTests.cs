using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Services.Dtos.Procurement;
using Leitor.Erp.Services.Procurement;
using Xunit;

namespace Leitor.Erp.Tests;

public class VendorAppServiceTests : ErpTestBase
{
    [Fact]
    public async Task CreateAsync_Persists_Vendor_With_DefaultPaymentTerms_And_Currency()
    {
        await EnsureDatabaseCreatedAsync();
        var vendorAppService = GetRequiredService<VendorAppService>();

        var vendor = await vendorAppService.CreateAsync(new CreateUpdateVendorDto
        {
            Name = "Acme Supplies",
            DefaultPaymentTerms = PaymentTerms.Net30,
            DefaultCurrencyCode = "USD"
        });

        var fetched = await vendorAppService.GetAsync(vendor.Id);

        Assert.Equal("Acme Supplies", fetched.Name);
        Assert.Equal(PaymentTerms.Net30, fetched.DefaultPaymentTerms);
        Assert.Equal("USD", fetched.DefaultCurrencyCode);
    }

    [Fact]
    public async Task VendorContact_CreateAsync_Persists_And_GetListAsync_Filters_By_VendorId()
    {
        await EnsureDatabaseCreatedAsync();
        var vendorAppService = GetRequiredService<VendorAppService>();
        var vendorContactAppService = GetRequiredService<VendorContactAppService>();

        var vendorA = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Vendor A" });
        var vendorB = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Vendor B" });

        await vendorContactAppService.CreateAsync(new CreateUpdateVendorContactDto
        {
            VendorId = vendorA.Id,
            FullName = "Jane Procurement",
            Email = "jane@vendor-a.test",
            IsPrimary = true
        });
        await vendorContactAppService.CreateAsync(new CreateUpdateVendorContactDto
        {
            VendorId = vendorB.Id,
            FullName = "John Sales"
        });

        var result = await vendorContactAppService.GetListAsync(new GetVendorContactListInput { VendorId = vendorA.Id });

        Assert.Single(result.Items);
        Assert.Equal("Jane Procurement", result.Items[0].FullName);
        Assert.True(result.Items[0].IsPrimary);
    }

    [Fact]
    public async Task VendorContact_DeleteAsync_Removes_Contact_Without_Deleting_Vendor()
    {
        await EnsureDatabaseCreatedAsync();
        var vendorAppService = GetRequiredService<VendorAppService>();
        var vendorContactAppService = GetRequiredService<VendorContactAppService>();

        var vendor = await vendorAppService.CreateAsync(new CreateUpdateVendorDto { Name = "Vendor With Contact" });
        var contact = await vendorContactAppService.CreateAsync(new CreateUpdateVendorContactDto
        {
            VendorId = vendor.Id,
            FullName = "Temp Contact"
        });

        await vendorContactAppService.DeleteAsync(contact.Id);

        var result = await vendorContactAppService.GetListAsync(new GetVendorContactListInput { VendorId = vendor.Id });
        Assert.Empty(result.Items);

        var stillThere = await vendorAppService.GetAsync(vendor.Id);
        Assert.Equal("Vendor With Contact", stillThere.Name);
    }
}