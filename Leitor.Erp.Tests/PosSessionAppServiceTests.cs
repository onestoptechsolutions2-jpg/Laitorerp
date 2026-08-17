using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Pos;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Dtos.Inventory;
using Leitor.Erp.Services.Dtos.Pos;
using Leitor.Erp.Services.Inventory;
using Leitor.Erp.Services.Pos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Security.Claims;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the 2026-08-17 performance fix: PosSessionAppService.GetListAsync used to call the
// single-session ToDtoAsync/ComputeExpectedCashAsync helpers in a loop (up to 5 queries per
// session), now batches warehouse/user lookups and sale/payment aggregation across all sessions
// at once. These tests seed PosSale/PosPayment directly via repository (bypassing
// PosSaleAppService.CompleteSaleAsync's full stock/posting workflow, same "seed narrowly" pattern
// EscalationItemTests uses) since what's under test is GetListAsync's own aggregation math, not
// the sale-completion flow.
public class PosSessionAppServiceTests : ErpTestBase
{
    private IDisposable ImpersonateAsUser(Guid userId)
    {
        var principalAccessor = GetRequiredService<ICurrentPrincipalAccessor>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(AbpClaimTypes.UserId, userId.ToString())
        }));
        return principalAccessor.Change(principal);
    }

    [Fact]
    public async Task GetListAsync_Computes_ExpectedCashAmount_From_Opening_Plus_Cash_Sales_Only()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.PointOfSale, "true", "T", null);

        var warehouseAppService = GetRequiredService<WarehouseAppService>();
        var warehouse = await warehouseAppService.CreateAsync(new CreateUpdateWarehouseDto { Name = "Main Store" });

        var userId = Guid.NewGuid();
        PosSessionDto session;
        using (ImpersonateAsUser(userId))
        {
            var sessionAppService = GetRequiredService<PosSessionAppService>();
            session = await sessionAppService.OpenAsync(new OpenPosSessionDto { WarehouseId = warehouse.Id, OpeningCashAmount = 1000m });
        }

        var saleRepository = GetRequiredService<IRepository<PosSale, Guid>>();
        var completedSale = new PosSale(Guid.NewGuid(), "POS-0001", session.Id, warehouse.Id, userId, DateTime.UtcNow) { Status = PosSaleStatus.Completed };
        await saleRepository.InsertAsync(completedSale, autoSave: true);
        var voidedSale = new PosSale(Guid.NewGuid(), "POS-0002", session.Id, warehouse.Id, userId, DateTime.UtcNow) { Status = PosSaleStatus.Voided };
        await saleRepository.InsertAsync(voidedSale, autoSave: true);

        var paymentRepository = GetRequiredService<IRepository<PosPayment, Guid>>();
        await paymentRepository.InsertAsync(new PosPayment(Guid.NewGuid(), completedSale.Id, 500m, PaymentMethod.Cash), autoSave: true);
        await paymentRepository.InsertAsync(new PosPayment(Guid.NewGuid(), completedSale.Id, 200m, PaymentMethod.Card), autoSave: true);
        // Cash payment against the voided sale must not count toward expected cash.
        await paymentRepository.InsertAsync(new PosPayment(Guid.NewGuid(), voidedSale.Id, 9999m, PaymentMethod.Cash), autoSave: true);

        var sessionAppServiceForList = GetRequiredService<PosSessionAppService>();
        var sessions = await sessionAppServiceForList.GetListAsync();

        var dto = Assert.Single(sessions);
        Assert.Equal(warehouse.Id, dto.WarehouseId);
        Assert.Equal("Main Store", dto.WarehouseName);
        // 1000 opening + 500 cash from the completed sale only (card payment and the voided
        // sale's cash payment both excluded).
        Assert.Equal(1500m, dto.ExpectedCashAmount);
    }

    [Fact]
    public async Task GetListAsync_Returns_Sessions_With_No_Sales_At_Opening_Cash_Only()
    {
        await EnsureDatabaseCreatedAsync();
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.PointOfSale, "true", "T", null);

        var warehouseAppService = GetRequiredService<WarehouseAppService>();
        var warehouse = await warehouseAppService.CreateAsync(new CreateUpdateWarehouseDto { Name = "Kiosk" });

        using (ImpersonateAsUser(Guid.NewGuid()))
        {
            var sessionAppService = GetRequiredService<PosSessionAppService>();
            await sessionAppService.OpenAsync(new OpenPosSessionDto { WarehouseId = warehouse.Id, OpeningCashAmount = 250m });
        }

        var sessionAppServiceForList = GetRequiredService<PosSessionAppService>();
        var sessions = await sessionAppServiceForList.GetListAsync();

        var dto = Assert.Single(sessions);
        Assert.Equal(250m, dto.ExpectedCashAmount);
    }
}
