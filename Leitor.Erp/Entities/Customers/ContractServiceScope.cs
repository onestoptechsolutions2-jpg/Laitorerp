using System;

namespace Leitor.Erp.Entities.Customers;

// The 13 services in Laitor's managed-IT-services retainer pitch ("we manage your company's IT
// and cybersecurity") - lets a CustomerContract record which of them a given client's monthly fee
// actually covers, rather than every contract implicitly covering everything. Bit-flags so a
// contract can combine any subset.
[Flags]
public enum ContractServiceScope
{
    None = 0,
    NetworkInfrastructure = 1 << 0,
    Wifi = 1 << 1,
    Firewalls = 1 << 2,
    UserDeviceSupport = 1 << 3,
    Backups = 1 << 4,
    ProductivitySuiteAdmin = 1 << 5,
    EndpointSecurity = 1 << 6,
    PatchManagement = 1 << 7,
    CctvOversight = 1 << 8,
    SecurityMonitoring = 1 << 9,
    ItPolicies = 1 << 10,
    IncidentResponse = 1 << 11,
    VendorCoordination = 1 << 12
}

// Iteration order for checkbox lists (Create/Edit) and summary display (Customer Detail) - a
// single source of truth so the three call sites can't drift out of sync with each other.
public static class ContractServiceScopeOptions
{
    public static readonly ContractServiceScope[] All =
    {
        ContractServiceScope.NetworkInfrastructure,
        ContractServiceScope.Wifi,
        ContractServiceScope.Firewalls,
        ContractServiceScope.UserDeviceSupport,
        ContractServiceScope.Backups,
        ContractServiceScope.ProductivitySuiteAdmin,
        ContractServiceScope.EndpointSecurity,
        ContractServiceScope.PatchManagement,
        ContractServiceScope.CctvOversight,
        ContractServiceScope.SecurityMonitoring,
        ContractServiceScope.ItPolicies,
        ContractServiceScope.IncidentResponse,
        ContractServiceScope.VendorCoordination
    };
}
