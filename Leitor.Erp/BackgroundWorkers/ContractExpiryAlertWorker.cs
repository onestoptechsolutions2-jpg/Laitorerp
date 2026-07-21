using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;
using Volo.Abp.Identity;
using Volo.Abp.Settings;
using Volo.Abp.Threading;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace Leitor.Erp.BackgroundWorkers;

// Runs once daily: emails a customer's account owner when an Active CustomerContract is within
// ErpSettings.ContractExpiryAlertLeadDays (admin-editable, default 30) of its EndDate, then stamps
// LastExpiryAlertSentDate so the same crossing isn't re-alerted tomorrow. A contract with no
// account owner (or an owner with no email) is skipped, not stamped, so it's picked up again once
// an owner is assigned.
public class ContractExpiryAlertWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ContractExpiryAlertWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromHours(24).TotalMilliseconds;
    }

    [UnitOfWork]
    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var contractRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<CustomerContract, Guid>>();
        var customerRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Customer, Guid>>();
        var identityUserRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<IdentityUser, Guid>>();
        var emailSender = workerContext.ServiceProvider.GetRequiredService<IEmailSender>();
        var clock = workerContext.ServiceProvider.GetRequiredService<IClock>();
        var companyOptions = workerContext.ServiceProvider.GetRequiredService<IOptions<ErpCompanyOptions>>().Value;
        var settingProvider = workerContext.ServiceProvider.GetRequiredService<ISettingProvider>();

        var leadDays = double.Parse((await settingProvider.GetOrNullAsync(ErpSettings.ContractExpiryAlertLeadDays))!);

        var now = clock.Now;
        var cutoff = now.AddDays(leadDays);

        var expiringContracts = await contractRepository.GetListAsync(x =>
            x.Status == CustomerContractStatus.Active &&
            x.EndDate.HasValue &&
            x.EndDate.Value >= now &&
            x.EndDate.Value <= cutoff &&
            x.LastExpiryAlertSentDate == null);

        if (expiringContracts.Count == 0)
        {
            return;
        }

        var customerIds = expiringContracts.Select(x => x.CustomerId).Distinct().ToList();
        var customersById = (await customerRepository.GetListAsync(x => customerIds.Contains(x.Id)))
            .ToDictionary(x => x.Id);

        var ownerIds = customersById.Values
            .Where(x => x.AccountOwnerUserId.HasValue)
            .Select(x => x.AccountOwnerUserId!.Value)
            .Distinct()
            .ToList();
        var ownersById = ownerIds.Count > 0
            ? (await identityUserRepository.GetListAsync(x => ownerIds.Contains(x.Id))).ToDictionary(x => x.Id)
            : new Dictionary<Guid, IdentityUser>();

        var alertedContracts = new List<CustomerContract>();

        foreach (var contract in expiringContracts)
        {
            if (!customersById.TryGetValue(contract.CustomerId, out var customer) ||
                !customer.AccountOwnerUserId.HasValue ||
                !ownersById.TryGetValue(customer.AccountOwnerUserId.Value, out var owner) ||
                string.IsNullOrWhiteSpace(owner.Email))
            {
                continue;
            }

            await emailSender.SendAsync(
                owner.Email,
                $"Contract {contract.ContractNumber} expiring soon",
                $"Dear {owner.UserName},\n\n" +
                $"{customer.Name}'s contract \"{contract.Title}\" ({contract.ContractNumber}) expires on {contract.EndDate:d}.\n\n" +
                "Please follow up on renewal.\n\n" +
                $"Regards,\n{companyOptions.Name}",
                isBodyHtml: false
            );

            contract.LastExpiryAlertSentDate = now;
            alertedContracts.Add(contract);
        }

        if (alertedContracts.Count > 0)
        {
            await contractRepository.UpdateManyAsync(alertedContracts);
        }
    }
}
