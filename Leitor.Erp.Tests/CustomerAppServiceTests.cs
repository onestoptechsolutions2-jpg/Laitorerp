using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace Leitor.Erp.Tests;

public class CustomerAppServiceTests : ErpTestBase
{
    [Fact]
    public async Task CreateAsync_Persists_Customer_With_Given_Name()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto
        {
            Name = "Acme Networks",
            Email = "ops@acme.test"
        });

        var fetched = await customerAppService.GetAsync(customer.Id);

        Assert.Equal("Acme Networks", fetched.Name);
        Assert.Equal("ops@acme.test", fetched.Email);
        Assert.Equal(CustomerStatus.Lead, fetched.Status);
    }

    [Fact]
    public async Task GetListAsync_Filters_By_Name()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();

        await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Contoso Ltd" });
        await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Fabrikam Inc" });

        var result = await customerAppService.GetListAsync(new GetCustomerListInput { Filter = "Contoso" });

        Assert.Single(result.Items);
        Assert.Equal("Contoso Ltd", result.Items[0].Name);
    }

    [Fact]
    public async Task DeleteAsync_Cascades_To_Notes()
    {
        await EnsureDatabaseCreatedAsync();
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customerNoteAppService = GetRequiredService<CustomerNoteAppService>();
        var noteRepository = GetRequiredService<IRepository<CustomerNote, Guid>>();

        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = "Northwind Traders" });
        await customerNoteAppService.CreateAsync(new CreateCustomerNoteDto
        {
            CustomerId = customer.Id,
            Text = "Initial discovery call."
        });

        await customerAppService.DeleteAsync(customer.Id);

        var remainingNotes = await noteRepository.GetListAsync(x => x.CustomerId == customer.Id);
        Assert.Empty(remainingNotes);
    }
}
