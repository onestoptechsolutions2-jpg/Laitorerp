using System;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Dtos.Hr;
using Leitor.Erp.Services.Hr;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 HR module's foundation (Phase 4a): Employee directory CRUD, toggleable
// via ErpFeatures.HumanResources.
public class EmployeeAppServiceTests : ErpTestBase
{
    private async Task EnableHumanResourcesAsync()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.HumanResources, "true", "T", null);
    }

    [Fact]
    public async Task CreateAsync_Then_UpdateAsync_Round_Trips_An_Employee()
    {
        await EnableHumanResourcesAsync();
        var employeeAppService = GetRequiredService<EmployeeAppService>();

        var created = await employeeAppService.CreateAsync(new CreateUpdateEmployeeDto
        {
            FullName = "Jane Wambui",
            HireDate = new DateTime(2024, 1, 15),
            JobTitle = "Field Technician",
            BasicSalary = 45000m
        });

        Assert.Equal("Jane Wambui", created.FullName);
        Assert.True(created.IsActive);

        var updated = await employeeAppService.UpdateAsync(created.Id, new CreateUpdateEmployeeDto
        {
            FullName = "Jane Wambui",
            HireDate = new DateTime(2024, 1, 15),
            JobTitle = "Senior Field Technician",
            BasicSalary = 55000m,
            KraPin = "A012345678Z"
        });

        Assert.Equal("Senior Field Technician", updated.JobTitle);
        Assert.Equal(55000m, updated.BasicSalary);
        Assert.Equal("A012345678Z", updated.KraPin);
    }

    [Fact]
    public async Task GetListAsync_Filters_By_IsActive_And_Name()
    {
        await EnableHumanResourcesAsync();
        var employeeAppService = GetRequiredService<EmployeeAppService>();

        var active = await employeeAppService.CreateAsync(new CreateUpdateEmployeeDto
        {
            FullName = "Active Employee",
            HireDate = DateTime.UtcNow,
            IsActive = true
        });

        var inactive = await employeeAppService.CreateAsync(new CreateUpdateEmployeeDto
        {
            FullName = "Former Employee",
            HireDate = DateTime.UtcNow.AddYears(-2),
            TerminationDate = DateTime.UtcNow.AddMonths(-1),
            IsActive = false
        });

        var activeOnly = await employeeAppService.GetListAsync(new GetEmployeeListInput { IsActive = true });
        var activeItem = Assert.Single(activeOnly.Items);
        Assert.Equal(active.Id, activeItem.Id);

        var byName = await employeeAppService.GetListAsync(new GetEmployeeListInput { Filter = "Former" });
        var namedItem = Assert.Single(byName.Items);
        Assert.Equal(inactive.Id, namedItem.Id);
    }

    [Fact]
    public async Task Optional_UserId_Self_Service_Link_Round_Trips()
    {
        await EnableHumanResourcesAsync();
        var employeeAppService = GetRequiredService<EmployeeAppService>();
        var userId = Guid.NewGuid();

        var created = await employeeAppService.CreateAsync(new CreateUpdateEmployeeDto
        {
            FullName = "Self-Service Employee",
            HireDate = DateTime.UtcNow,
            UserId = userId
        });

        var reloaded = await employeeAppService.GetAsync(created.Id);
        Assert.Equal(userId, reloaded.UserId);
    }
}
