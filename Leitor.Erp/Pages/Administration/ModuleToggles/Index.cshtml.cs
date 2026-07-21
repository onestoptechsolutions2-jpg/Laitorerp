using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Localization;
using Leitor.Erp.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
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
    // "H" is ABP's well-known Host feature-value-provider name - the correct scope for a global
    // toggle in a non-multi-tenant app like this one (ErpModule.IsMultiTenant is false).
    private const string HostProviderName = "H";

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

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostToggleAsync(string name, bool enabled)
    {
        await _featureManager.SetAsync(name, enabled ? "true" : "false", HostProviderName, null);
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
            await ToRowAsync(ErpFeatures.KnowledgeManagement, _l["Feature:KnowledgeManagement"])
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
