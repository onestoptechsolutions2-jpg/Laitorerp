using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Sales;
using Leitor.Erp.Services.Governance;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Sales;

public class PriceListAppService :
    CrudAppService<PriceList, PriceListDto, Guid, PagedAndSortedResultRequestDto, CreateUpdatePriceListDto>
{
    private readonly IRepository<PriceListItem, Guid> _itemRepository;
    private readonly IRepository<Quote, Guid> _quoteRepository;
    private readonly IRepository<CustomerPriceList, Guid> _customerPriceListRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;

    public PriceListAppService(
        IRepository<PriceList, Guid> repository,
        IRepository<PriceListItem, Guid> itemRepository,
        IRepository<Quote, Guid> quoteRepository,
        IRepository<CustomerPriceList, Guid> customerPriceListRepository,
        IRepository<Customer, Guid> customerRepository)
        : base(repository)
    {
        _itemRepository = itemRepository;
        _quoteRepository = quoteRepository;
        _customerPriceListRepository = customerPriceListRepository;
        _customerRepository = customerRepository;

        GetPolicyName = ErpPermissions.Catalog.Default;
        GetListPolicyName = ErpPermissions.Catalog.Default;
        CreatePolicyName = ErpPermissions.Catalog.Edit;
        UpdatePolicyName = ErpPermissions.Catalog.Edit;
        DeletePolicyName = ErpPermissions.Catalog.Edit;
    }

    // PriceListItems have no independent identity of their own, so they're still cascaded.
    // Quote/CustomerPriceList/Customer.DefaultPriceListId are independent records that can
    // reference this PriceList - blocked instead (system-wide "block deletion if dependents
    // exist" policy, see DependencyGuard).
    public override async Task DeleteAsync(Guid id)
    {
        await CheckDeletePolicyAsync();

        await DependencyGuard.EnsureDeletableAsync(
            (async () => (await _quoteRepository.GetListAsync(x => x.PriceListId == id)).Count, "Quote"),
            (async () => (await _customerPriceListRepository.GetListAsync(x => x.PriceListId == id)).Count, "Customer Price List Override"),
            (async () => (await _customerRepository.GetListAsync(x => x.DefaultPriceListId == id)).Count, "Customer")
        );

        var items = await _itemRepository.GetListAsync(x => x.PriceListId == id);
        await _itemRepository.DeleteManyAsync(items);

        await Repository.DeleteAsync(id);
    }

    // CreateUpdatePriceListDto -> PriceList is mapped manually rather than via Mapperly - same
    // reason as every other entity in this app (protected Id setter).
    protected override Task<PriceList> MapToEntityAsync(CreateUpdatePriceListDto createInput)
    {
        var entity = new PriceList(GuidGenerator.Create(), createInput.Name);
        return Task.FromResult(entity);
    }

    protected override Task MapToEntityAsync(CreateUpdatePriceListDto updateInput, PriceList entity)
    {
        entity.Name = updateInput.Name;
        return Task.CompletedTask;
    }
}
