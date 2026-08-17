using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.ServiceCatalog;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace Leitor.Erp.Data;

// Seeds the Service Catalog with Leitor's real named services on first run only - editable/
// extendable afterward via Pages/ServiceCatalog, same safe-to-rerun convention as
// ErpChartOfAccountsDataSeeder/ErpContractTemplateDataSeeder. The module ships with zero data
// otherwise (it's a toggleable optional module - see Features/ErpFeatures.ServiceCatalog),
// which meant turning it on showed an empty list with nothing to actually quote/request against.
//
// The 25 items map directly onto the 3-stream business model documented in MODULES.md: the 13
// Category="Managed IT & Cybersecurity Retainer" items mirror ContractServiceScope's 13 flags
// one-for-one (same list, different purpose - ContractServiceScope marks which services a given
// customer's contract covers; these are the catalog definitions a ServiceRequest can reference),
// the 7 Category="Projects" items are stream 2's one-off higher-margin work, and the 5
// Category="Cybersecurity Upsell" items mirror SecurityAssessmentType's 5 enum members - stream
// 3 made concrete as catalog entries rather than only existing as an assessment-type dropdown.
//
// TargetSlaHours is only set on the retainer items (a plausible standard-request response
// target under an active contract) - Project and Cybersecurity engagements are scoped/scheduled
// work, not an incident-style response-time commitment, so they're left null.
public class ErpServiceCatalogDataSeeder : IDataSeedContributor, ITransientDependency
{
    private const string RetainerCategory = "Managed IT & Cybersecurity Retainer";
    private const string ProjectCategory = "Projects";
    private const string CybersecurityCategory = "Cybersecurity Upsell";

    private readonly IRepository<ServiceCatalogItem, Guid> _serviceCatalogItemRepository;
    private readonly IGuidGenerator _guidGenerator;

    public ErpServiceCatalogDataSeeder(
        IRepository<ServiceCatalogItem, Guid> serviceCatalogItemRepository,
        IGuidGenerator guidGenerator)
    {
        _serviceCatalogItemRepository = serviceCatalogItemRepository;
        _guidGenerator = guidGenerator;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        if (await _serviceCatalogItemRepository.GetCountAsync() > 0)
        {
            return;
        }

        // Stream 1: the core recurring retainer (KES 15k-30k/month) - one item per
        // ContractServiceScope flag, same order and wording as that enum/MODULES.md.
        await SeedAsync("Network Infrastructure Management", RetainerCategory, "Routers, switches, and core network hardware - monitoring, configuration, and troubleshooting.", 8);
        await SeedAsync("Wi-Fi Management", RetainerCategory, "Wireless network setup, coverage, and access management.", 8);
        await SeedAsync("Firewall Management", RetainerCategory, "Firewall configuration, rule maintenance, and perimeter security.", 8);
        await SeedAsync("User & Device Support", RetainerCategory, "End-user helpdesk support and device troubleshooting.", 4);
        await SeedAsync("Backup Management", RetainerCategory, "Backup job configuration and monitoring.", 8);
        await SeedAsync("Microsoft 365 / Google Workspace Administration", RetainerCategory, "User provisioning, licensing, and admin-console management for the client's productivity suite.", 8);
        await SeedAsync("Endpoint Security", RetainerCategory, "Antivirus/EDR deployment and monitoring across client devices.", 8);
        await SeedAsync("Patch & Update Management", RetainerCategory, "Scheduled OS/application patching across servers and endpoints.", 24);
        await SeedAsync("CCTV Oversight", RetainerCategory, "Ongoing monitoring and maintenance of installed CCTV systems.", 24);
        await SeedAsync("Basic Security Monitoring", RetainerCategory, "Baseline monitoring for suspicious activity across managed infrastructure.", 8);
        await SeedAsync("IT Policy Management", RetainerCategory, "Maintaining the client's IT usage/security policy documents.", null);
        await SeedAsync("Incident Response", RetainerCategory, "First response and containment for reported security/IT incidents.", 2);
        await SeedAsync("Vendor Coordination", RetainerCategory, "Liaising with the client's other IT/telecom/hardware vendors on their behalf.", 24);

        // Stream 2: one-off, higher-margin projects.
        await SeedAsync("Network Deployment", ProjectCategory, "New-site or upgrade network builds - structured cabling, switches, routers.", null);
        await SeedAsync("CCTV Installation", ProjectCategory, "New CCTV system design and installation.", null);
        await SeedAsync("Server Deployment", ProjectCategory, "New server hardware/virtualization deployment.", null);
        await SeedAsync("Office Move / Relocation", ProjectCategory, "IT relocation planning and execution for an office move.", null);
        await SeedAsync("Security Upgrade", ProjectCategory, "One-off hardening projects - access control, camera coverage, network segmentation.", null);
        await SeedAsync("Cloud Migration", ProjectCategory, "Migrating on-prem workloads/data to cloud infrastructure.", null);
        await SeedAsync("Hardware & Software Resale", ProjectCategory, "Sourcing and reselling hardware/software as part of a project.", null);

        // Stream 3: the cybersecurity upsell tier - mirrors SecurityAssessmentType's 5 members.
        await SeedAsync("Vulnerability Assessment", CybersecurityCategory, "Scoped scan/review to identify exploitable weaknesses.", null);
        await SeedAsync("Cyber-Risk Assessment", CybersecurityCategory, "Broader risk-rated review of the client's security posture.", null);
        await SeedAsync("Security Policy Review", CybersecurityCategory, "Review and update of the client's security policy documents.", null);
        await SeedAsync("Cyber Awareness Training", CybersecurityCategory, "Staff training sessions on phishing/security best practices.", null);
        await SeedAsync("Backup & DR Review", CybersecurityCategory, "Assessment of backup/disaster-recovery readiness.", null);
    }

    private async Task SeedAsync(string name, string category, string description, int? targetSlaHours)
    {
        await _serviceCatalogItemRepository.InsertAsync(new ServiceCatalogItem(_guidGenerator.Create(), name)
        {
            Category = category,
            Description = description,
            TargetSlaHours = targetSlaHours
        });
    }
}
