using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Sales;
using Leitor.Erp.Permissions;
using Leitor.Erp.Services.Dtos.Customers;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Customers;

public class CustomerPriceListAppService : ApplicationService
{
    private readonly IRepository<CustomerPriceList, Guid> _repository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<PriceList, Guid> _priceListRepository;

    public CustomerPriceListAppService(
        IRepository<CustomerPriceList, Guid> repository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<PriceList, Guid> priceListRepository)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _priceListRepository = priceListRepository;
    }

    public async Task<List<CustomerPriceListDto>> GetListAsync(Guid customerId)
    {
        var priceListAssignments = await _repository.GetListAsync(x => x.CustomerId == customerId);
        var priceListMap = (await _priceListRepository.GetListAsync()).ToDictionary(x => x.Id, x => x.Name);

        return priceListAssignments.Select(x => new CustomerPriceListDto
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            PriceListId = x.PriceListId,
            IsPrimary = x.IsPrimary,
            PriceListName = priceListMap.GetValueOrDefault(x.PriceListId, "Unknown")
        }).ToList();
    }

    public async Task<CustomerPriceListDto> AddAsync(Guid customerId, CreateUpdateCustomerPriceListDto input)
    {
        await CheckCreatePolicyAsync();

        var customer = await _customerRepository.GetAsync(customerId);
        var priceList = await _priceListRepository.GetAsync(input.PriceListId);

        // Check if already assigned
        var existing = await _repository.FindAsync(x => x.CustomerId == customerId && x.PriceListId == input.PriceListId);
        if (existing != null)
        {
            throw new UserFriendlyException("This price list is already assigned to the customer.");
        }

        // If marking as primary, unset other primary
        if (input.IsPrimary)
        {
            var otherPrimary = await _repository.FindAsync(x => x.CustomerId == customerId && x.IsPrimary);
            if (otherPrimary != null)
            {
                otherPrimary.IsPrimary = false;
                await _repository.UpdateAsync(otherPrimary, autoSave: true);
            }

            // Also update customer's DefaultPriceListId
            customer.DefaultPriceListId = input.PriceListId;
            await _customerRepository.UpdateAsync(customer, autoSave: true);
        }

        var assignment = new CustomerPriceList(GuidGenerator.Create(), customerId, input.PriceListId, input.IsPrimary);
        var created = await _repository.InsertAsync(assignment, autoSave: true);

        return new CustomerPriceListDto
        {
            Id = created.Id,
            CustomerId = created.CustomerId,
            PriceListId = created.PriceListId,
            IsPrimary = created.IsPrimary,
            PriceListName = priceList.Name
        };
    }

    public async Task RemoveAsync(Guid id)
    {
        await CheckDeletePolicyAsync();
        await _repository.DeleteAsync(id, autoSave: true);
    }

    public async Task SetPrimaryAsync(Guid id)
    {
        await CheckUpdatePolicyAsync();

        var assignment = await _repository.GetAsync(id);

        // Unset other primary for same customer
        var otherPrimary = await _repository.FindAsync(x => x.CustomerId == assignment.CustomerId && x.IsPrimary && x.Id != id);
        if (otherPrimary != null)
        {
            otherPrimary.IsPrimary = false;
            await _repository.UpdateAsync(otherPrimary);
        }

        // Set this as primary
        assignment.IsPrimary = true;
        await _repository.UpdateAsync(assignment, autoSave: true);

        // Update customer's DefaultPriceListId
        var customer = await _customerRepository.GetAsync(assignment.CustomerId);
        customer.DefaultPriceListId = assignment.PriceListId;
        await _customerRepository.UpdateAsync(customer, autoSave: true);
    }

    protected async Task CheckCreatePolicyAsync()
    {
        await Task.CompletedTask;
        // Add permission check if needed
    }

    protected async Task CheckUpdatePolicyAsync()
    {
        await Task.CompletedTask;
        // Add permission check if needed
    }

    protected async Task CheckDeletePolicyAsync()
    {
        await Task.CompletedTask;
        // Add permission check if needed
    }
}
