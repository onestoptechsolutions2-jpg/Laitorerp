using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Customers;
using Leitor.Erp.Entities.Cybersecurity;
using Leitor.Erp.Entities.Support;
using Leitor.Erp.Features;
using Leitor.Erp.Services.Assets;
using Leitor.Erp.Services.Customers;
using Leitor.Erp.Services.Cybersecurity;
using Leitor.Erp.Services.Dtos.Assets;
using Leitor.Erp.Services.Dtos.Customers;
using Leitor.Erp.Services.Dtos.Cybersecurity;
using Leitor.Erp.Services.Dtos.Support;
using Leitor.Erp.Services.Support;
using Volo.Abp.FeatureManagement;
using Xunit;

namespace Leitor.Erp.Tests;

// Covers the managed-IT-and-cybersecurity retainer additions built 2026-08-09: the
// SecurityAssessment module's status/CompletedDate auto-tracking (same pattern as
// ProblemAppService), and that the new fields on CustomerContract/Ticket/ConfigurationItem
// actually persist round-trip.
public class CybersecurityLayerTests : ErpTestBase
{
    private async Task EnableCybersecurityFeatureAsync()
    {
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.Cybersecurity, "true", "T", null);
    }

    private async Task EnableAssetManagementFeatureAsync()
    {
        var featureManager = GetRequiredService<IFeatureManager>();
        await featureManager.SetAsync(ErpFeatures.AssetManagement, "true", "T", null);
    }

    private async Task<Guid> CreateCustomerAsync(string name)
    {
        var customerAppService = GetRequiredService<CustomerAppService>();
        var customer = await customerAppService.CreateAsync(new CreateUpdateCustomerDto { Name = name });
        return customer.Id;
    }

    [Fact]
    public async Task CreateAsync_Assigns_SEC_Prefixed_AssessmentNumber()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableCybersecurityFeatureAsync();

        var securityAssessmentAppService = GetRequiredService<SecurityAssessmentAppService>();
        var customerId = await CreateCustomerAsync("Zikis");

        var assessment = await securityAssessmentAppService.CreateAsync(new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = customerId,
            Title = "Q3 Vulnerability Scan",
            Type = SecurityAssessmentType.VulnerabilityAssessment,
            ScheduledDate = DateTime.Today
        });

        Assert.StartsWith("SEC-", assessment.AssessmentNumber);
    }

    [Fact]
    public async Task UpdateAsync_Sets_CompletedDate_When_Status_Moves_To_Completed()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableCybersecurityFeatureAsync();

        var securityAssessmentAppService = GetRequiredService<SecurityAssessmentAppService>();
        var customerId = await CreateCustomerAsync("Mnyanga");

        var assessment = await securityAssessmentAppService.CreateAsync(new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = customerId,
            Title = "Cyber-Risk Review",
            Type = SecurityAssessmentType.CyberRiskAssessment,
            Status = SecurityAssessmentStatus.InProgress,
            ScheduledDate = DateTime.Today
        });
        Assert.Null(assessment.CompletedDate);

        var completed = await securityAssessmentAppService.UpdateAsync(assessment.Id, new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = customerId,
            Title = assessment.Title,
            Type = assessment.Type,
            Status = SecurityAssessmentStatus.Completed,
            RiskRating = SecurityRiskRating.High,
            ScheduledDate = assessment.ScheduledDate
        });

        Assert.NotNull(completed.CompletedDate);
        Assert.Equal(SecurityRiskRating.High, completed.RiskRating);
    }

    [Fact]
    public async Task UpdateAsync_Clears_CompletedDate_When_Reopened()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableCybersecurityFeatureAsync();

        var securityAssessmentAppService = GetRequiredService<SecurityAssessmentAppService>();
        var customerId = await CreateCustomerAsync("Riffat Consulting");

        var assessment = await securityAssessmentAppService.CreateAsync(new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = customerId,
            Title = "Backup/DR Review",
            Type = SecurityAssessmentType.BackupDrReview,
            Status = SecurityAssessmentStatus.Completed,
            ScheduledDate = DateTime.Today
        });
        Assert.NotNull(assessment.CompletedDate);

        var reopened = await securityAssessmentAppService.UpdateAsync(assessment.Id, new CreateUpdateSecurityAssessmentDto
        {
            CustomerId = customerId,
            Title = assessment.Title,
            Type = assessment.Type,
            Status = SecurityAssessmentStatus.InProgress,
            ScheduledDate = assessment.ScheduledDate
        });

        Assert.Null(reopened.CompletedDate);
    }

    [Fact]
    public async Task CustomerContract_ServicesIncluded_Flags_Persist_Round_Trip()
    {
        await EnsureDatabaseCreatedAsync();

        var contractAppService = GetRequiredService<CustomerContractAppService>();
        var customerId = await CreateCustomerAsync("Jipos");

        var scope = ContractServiceScope.NetworkInfrastructure | ContractServiceScope.EndpointSecurity | ContractServiceScope.IncidentResponse;

        var contract = await contractAppService.CreateAsync(new CreateUpdateCustomerContractDto
        {
            CustomerId = customerId,
            ContractNumber = "CTR-0001",
            Title = "Managed IT & Cybersecurity Retainer",
            StartDate = DateTime.Today,
            ServicesIncluded = scope
        });

        Assert.Equal(scope, contract.ServicesIncluded);
        Assert.True(contract.ServicesIncluded.HasFlag(ContractServiceScope.EndpointSecurity));
        Assert.False(contract.ServicesIncluded.HasFlag(ContractServiceScope.CctvOversight));
    }

    [Fact]
    public async Task Ticket_SecurityIncident_Breach_And_ContainedDate_Persist()
    {
        await EnsureDatabaseCreatedAsync();

        var ticketAppService = GetRequiredService<TicketAppService>();
        var customerId = await CreateCustomerAsync("Zikis Client");

        var containedDate = DateTime.UtcNow;
        var ticket = await ticketAppService.CreateAsync(new CreateUpdateTicketDto
        {
            CustomerId = customerId,
            Subject = "Phishing email compromised a mailbox",
            Type = TicketType.SecurityIncident,
            IsSecurityBreach = true,
            ContainedDate = containedDate
        });

        Assert.Equal(TicketType.SecurityIncident, ticket.Type);
        Assert.True(ticket.IsSecurityBreach);
        Assert.Equal(containedDate, ticket.ContainedDate);
    }

    [Fact]
    public async Task ConfigurationItem_Security_Monitoring_Fields_Persist()
    {
        await EnsureDatabaseCreatedAsync();
        await EnableAssetManagementFeatureAsync();

        var configurationItemAppService = GetRequiredService<ConfigurationItemAppService>();

        var patchedDate = DateTime.Today.AddDays(-3);
        var backupDate = DateTime.Today.AddDays(-1);

        var ci = await configurationItemAppService.CreateAsync(new CreateUpdateConfigurationItemDto
        {
            Name = "File Server",
            HasEndpointProtection = true,
            IsBackedUp = true,
            LastBackupVerifiedDate = backupDate,
            LastPatchedDate = patchedDate,
            SecurityMonitoringEnabled = true
        });

        Assert.True(ci.HasEndpointProtection);
        Assert.True(ci.IsBackedUp);
        Assert.True(ci.SecurityMonitoringEnabled);
        Assert.Equal(patchedDate, ci.LastPatchedDate);
        Assert.Equal(backupDate, ci.LastBackupVerifiedDate);
    }
}
