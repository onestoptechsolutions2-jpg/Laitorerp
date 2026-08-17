using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Inventory;
using Leitor.Erp.Entities.Pos;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Features;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Pos;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;
using Volo.Abp.Identity;

namespace Leitor.Erp.Services.Pos;

// Opening/closing a till - PosSaleAppService.CompleteSaleAsync requires an Open session to post a
// sale against. At most one Open session per Warehouse at a time, enforced here.
[RequiresFeature(ErpFeatures.PointOfSale)]
public class PosSessionAppService : ApplicationService
{
    private readonly IRepository<PosSession, Guid> _sessionRepository;
    private readonly IRepository<PosSale, Guid> _saleRepository;
    private readonly IRepository<PosPayment, Guid> _paymentRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;

    public PosSessionAppService(
        IRepository<PosSession, Guid> sessionRepository,
        IRepository<PosSale, Guid> saleRepository,
        IRepository<PosPayment, Guid> paymentRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<IdentityUser, Guid> identityUserRepository)
    {
        _sessionRepository = sessionRepository;
        _saleRepository = saleRepository;
        _paymentRepository = paymentRepository;
        _warehouseRepository = warehouseRepository;
        _identityUserRepository = identityUserRepository;
    }

    public async Task<PosSessionDto?> GetCurrentOpenAsync(Guid warehouseId)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Default);

        var session = (await _sessionRepository.GetListAsync(x => x.WarehouseId == warehouseId && x.Status == PosSessionStatus.Open))
            .FirstOrDefault();
        return session == null ? null : await ToDtoAsync(session);
    }

    // Batched rather than looping ToDtoAsync per session (each iteration was up to 5 queries -
    // warehouse, user, plus ComputeExpectedCashAsync's own 2 - so up to 5xN queries on a single
    // page load) - flagged in a 2026-08-17 performance audit. GetCurrentOpenAsync/OpenAsync/
    // CloseAsync keep using ToDtoAsync/ComputeExpectedCashAsync as-is; they only ever handle one
    // session, so there's no N+1 there to fix.
    public async Task<List<PosSessionDto>> GetListAsync()
    {
        await CheckPolicyAsync(ErpPermissions.Pos.Default);

        var sessions = (await _sessionRepository.GetListAsync()).OrderByDescending(x => x.OpenedAt).ToList();
        if (sessions.Count == 0)
        {
            return new List<PosSessionDto>();
        }

        var warehouseIds = sessions.Select(x => x.WarehouseId).Distinct().ToList();
        var warehouseNamesById = (await _warehouseRepository.GetListAsync(x => warehouseIds.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => x.Name);

        var userIds = sessions.Select(x => x.OpenedByUserId).Distinct().ToList();
        var userNamesById = (await _identityUserRepository.GetListAsync(x => userIds.Contains(x.Id)))
            .ToDictionary(x => x.Id, x => x.UserName);

        var sessionIds = sessions.Select(x => x.Id).ToList();
        var sales = await _saleRepository.GetListAsync(x => sessionIds.Contains(x.PosSessionId) && x.Status == PosSaleStatus.Completed);
        var saleIdsBySessionId = sales.ToLookup(x => x.PosSessionId, x => x.Id);

        var saleIds = sales.Select(x => x.Id).ToList();
        var cashBySaleId = saleIds.Count > 0
            ? (await _paymentRepository.GetListAsync(x => saleIds.Contains(x.PosSaleId) && x.Method == PaymentMethod.Cash))
                .GroupBy(x => x.PosSaleId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount))
            : new Dictionary<Guid, decimal>();

        return sessions.Select(session =>
        {
            var cashTendered = saleIdsBySessionId[session.Id].Sum(saleId => cashBySaleId.GetValueOrDefault(saleId, 0m));

            return new PosSessionDto
            {
                Id = session.Id,
                WarehouseId = session.WarehouseId,
                OpenedByUserId = session.OpenedByUserId,
                OpenedAt = session.OpenedAt,
                OpeningCashAmount = session.OpeningCashAmount,
                Status = session.Status,
                ClosedByUserId = session.ClosedByUserId,
                ClosedAt = session.ClosedAt,
                ClosingCashAmount = session.ClosingCashAmount,
                WarehouseName = warehouseNamesById.GetValueOrDefault(session.WarehouseId),
                OpenedByUserName = userNamesById.GetValueOrDefault(session.OpenedByUserId),
                ExpectedCashAmount = session.OpeningCashAmount + cashTendered
            };
        }).ToList();
    }

    public async Task<PosSessionDto> OpenAsync(OpenPosSessionDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.ManageSessions);

        var alreadyOpen = (await _sessionRepository.GetListAsync(x => x.WarehouseId == input.WarehouseId && x.Status == PosSessionStatus.Open)).Any();
        if (alreadyOpen)
        {
            throw new UserFriendlyException("A session is already open for this warehouse.");
        }

        var session = new PosSession(GuidGenerator.Create(), input.WarehouseId, CurrentUser.Id!.Value, Clock.Now, input.OpeningCashAmount);
        await _sessionRepository.InsertAsync(session, autoSave: true);

        return await ToDtoAsync(session);
    }

    public async Task<PosSessionDto> CloseAsync(Guid id, ClosePosSessionDto input)
    {
        await CheckPolicyAsync(ErpPermissions.Pos.ManageSessions);

        var session = await _sessionRepository.GetAsync(id);
        if (session.Status != PosSessionStatus.Open)
        {
            throw new UserFriendlyException("This session is already closed.");
        }

        session.Status = PosSessionStatus.Closed;
        session.ClosedByUserId = CurrentUser.Id!.Value;
        session.ClosedAt = Clock.Now;
        session.ClosingCashAmount = input.ClosingCashAmount;
        await _sessionRepository.UpdateAsync(session, autoSave: true);

        return await ToDtoAsync(session);
    }

    private async Task<decimal> ComputeExpectedCashAsync(PosSession session)
    {
        var saleIds = (await _saleRepository.GetListAsync(x => x.PosSessionId == session.Id && x.Status == PosSaleStatus.Completed))
            .Select(x => x.Id)
            .ToList();

        if (saleIds.Count == 0)
        {
            return session.OpeningCashAmount;
        }

        var cashTendered = (await _paymentRepository.GetListAsync(x => saleIds.Contains(x.PosSaleId) && x.Method == PaymentMethod.Cash))
            .Sum(x => x.Amount);

        return session.OpeningCashAmount + cashTendered;
    }

    private async Task<PosSessionDto> ToDtoAsync(PosSession session)
    {
        var warehouse = await _warehouseRepository.FindAsync(session.WarehouseId);
        var openedByUser = await _identityUserRepository.FindAsync(session.OpenedByUserId);

        return new PosSessionDto
        {
            Id = session.Id,
            WarehouseId = session.WarehouseId,
            OpenedByUserId = session.OpenedByUserId,
            OpenedAt = session.OpenedAt,
            OpeningCashAmount = session.OpeningCashAmount,
            Status = session.Status,
            ClosedByUserId = session.ClosedByUserId,
            ClosedAt = session.ClosedAt,
            ClosingCashAmount = session.ClosingCashAmount,
            WarehouseName = warehouse?.Name,
            OpenedByUserName = openedByUser?.UserName,
            ExpectedCashAmount = await ComputeExpectedCashAsync(session)
        };
    }
}
