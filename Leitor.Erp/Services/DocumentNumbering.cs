using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services;

// Every numbered document (Quote/Order/Invoice/PurchaseOrder/Ticket) mints its number from
// Repository.GetCountAsync() + 1. That count excludes soft-deleted rows (ABP's default ISoftDelete
// query filter), but a deleted row's number still physically occupies the unique index forever -
// so the very next document recomputes the same number and the insert fails with a duplicate-key
// error the moment anything gets deleted. Disabling the ISoftDelete filter just for the count
// fixes this without changing the numbering scheme itself.
public static class DocumentNumbering
{
    public static async Task<string> NextAsync<TEntity>(IRepository<TEntity, Guid> repository, IDataFilter dataFilter, string prefix)
        where TEntity : class, IEntity<Guid>
    {
        long count;
        using (dataFilter.Disable<ISoftDelete>())
        {
            count = await repository.GetCountAsync();
        }

        return $"{prefix}{count + 1:D6}";
    }
}
