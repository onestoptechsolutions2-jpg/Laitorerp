using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Localization;
using Leitor.Erp.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Features;

namespace Leitor.Erp.Pages.Administration.ModuleToggles;

// A small, purpose-built screen rather than ABP's own generic Feature Management UI - this shows
// only the 6 Erp.* module toggles (see Features/ErpFeatures.cs), not every feature every
// installed ABP module happens to define. Reads through IFeatureChecker (same interface every
// AppService's [RequiresFeature] check goes through) and writes through IFeatureManager - the
// standard read/write pair ABP's own admin tooling uses internally.
[Authorize(Policy = ErpPermissions.ModuleToggles.Manage)]
public class IndexModel : AbpPageModel
{
    // There is no "Host" feature-value provider in this ABP version (10.5.0) - only Default("D"),
    // Configuration("C"), Edition("E"), and Tenant("T") are registered (confirmed by reflecting
    // Volo.Abp.FeatureManagement.Domain.dll; "H" was never valid and threw "Unknown feature value
    // provider: H" on every toggle click). TenantFeatureManagementProvider always resolves against
    // CurrentTenant.Id regardless of the key passed in, which is always null since
    // ErpModule.IsMultiTenant is false - so setting via "T" with a null key is the correct
    // ABP-idiomatic equivalent of "the one global value" in a single-tenant app like this one.
    private const string HostProviderName = "T";

    private readonly IFeatureChecker _featureChecker;
    private readonly IFeatureManager _featureManager;
    private readonly IHtmlLocalizer<ErpResource> _l;

    public IndexModel(IFeatureChecker featureChecker, IFeatureManager featureManager, IHtmlLocalizer<ErpResource> l)
    {
        _featureChecker = featureChecker;
        _featureManager = featureManager;
        _l = l;
    }

    public List<ModuleToggleRow> Modules { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(string name, bool enabled)
    {
        try
        {
            await _featureManager.SetAsync(name, enabled ? "true" : "false", HostProviderName, null);
        }
        catch (AbpException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Modules = new List<ModuleToggleRow>
        {
            await ToRowAsync(ErpFeatures.ProjectManagement, _l["Feature:ProjectManagement"]),
            await ToRowAsync(ErpFeatures.TaxCompliance, _l["Feature:TaxCompliance"]),
            await ToRowAsync(ErpFeatures.ServiceCatalog, _l["Feature:ServiceCatalog"]),
            await ToRowAsync(ErpFeatures.ServiceRequestManagement, _l["Feature:ServiceRequestManagement"]),
            await ToRowAsync(ErpFeatures.AssetManagement, _l["Feature:AssetManagement"]),
            await ToRowAsync(ErpFeatures.KnowledgeManagement, _l["Feature:KnowledgeManagement"]),
            await ToRowAsync(ErpFeatures.PointOfSale, _l["Feature:PointOfSale"])
        };
    }

    private async Task<ModuleToggleRow> ToRowAsync(string name, LocalizedHtmlString displayName)
    {
        return new ModuleToggleRow
        {
            Name = name,
            DisplayName = displayName.Value,
            IsEnabled = await _featureChecker.IsEnabledAsync(name)
        };
    }

    public class ModuleToggleRow
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
