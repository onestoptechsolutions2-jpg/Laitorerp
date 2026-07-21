using System.Collections.Generic;
using System.Threading.Tasks;
using Leitor.Erp.Localization;
using Leitor.Erp.Permissions;
using Leitor.Erp.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.SettingManagement;
using Volo.Abp.Settings;

namespace Leitor.Erp.Pages.Administration.AppSettings;

// A small, purpose-built screen rather than ABP's own generic Setting Management UI - this shows
// only the Erp.* settings defined in Settings/ErpSettingDefinitionProvider.cs, not every setting
// every installed ABP module happens to define. Reads through ISettingProvider (the same interface
// TicketAppService/ContractExpiryAlertWorker read through) and writes through
// ISettingManager.SetGlobalAsync - the correct scope for a non-multi-tenant app like this one
// (same "H"/Global-provider precedent as ModuleToggles' use of the Host feature provider).
[Authorize(Policy = ErpPermissions.AppSettings.Manage)]
public class IndexModel : AbpPageModel
{
    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;
    private readonly IHtmlLocalizer<ErpResource> _l;

    public IndexModel(ISettingProvider settingProvider, ISettingManager settingManager, IHtmlLocalizer<ErpResource> l)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
        _l = l;
    }

    public List<SettingRow> Rows { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync(string name, string value)
    {
        if (double.TryParse(value, out var parsed) && parsed > 0)
        {
            await _settingManager.SetGlobalAsync(name, parsed.ToString("0.##"));
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rows = new List<SettingRow>
        {
            await ToRowAsync(ErpSettings.SlaHoursUrgent, _l["Setting:SlaHoursUrgent"]),
            await ToRowAsync(ErpSettings.SlaHoursHigh, _l["Setting:SlaHoursHigh"]),
            await ToRowAsync(ErpSettings.SlaHoursMedium, _l["Setting:SlaHoursMedium"]),
            await ToRowAsync(ErpSettings.SlaHoursLow, _l["Setting:SlaHoursLow"]),
            await ToRowAsync(ErpSettings.ContractExpiryAlertLeadDays, _l["Setting:ContractExpiryAlertLeadDays"])
        };
    }

    private async Task<SettingRow> ToRowAsync(string name, LocalizedHtmlString displayName)
    {
        return new SettingRow
        {
            Name = name,
            DisplayName = displayName.Value,
            Value = await _settingProvider.GetOrNullAsync(name) ?? string.Empty
        };
    }

    public class SettingRow
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
