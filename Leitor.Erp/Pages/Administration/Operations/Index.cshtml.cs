using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Leitor.Erp.Features;
using Leitor.Erp.Localization;
using Leitor.Erp.Permissions;
using Leitor.Erp.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Features;
using Volo.Abp.Settings;

namespace Leitor.Erp.Pages.Administration.Operations;

// Consolidates what used to be three separate screens (Pages/Administration/AppSettings,
// Pages/Administration/ModuleToggles, and ABP's own buried generic Setting Management "Emailing"
// tab) plus a genuinely new one (Company Profile, previously appsettings-only with zero UI) into
// one "single pane of glass" operations page. Company Profile is the one section with no prior
// UI at all - see Settings/ErpCompanyProfileProvider.cs for why that mattered (every generated
// PDF's letterhead needed a code redeploy to change before this).
[Authorize]
public class IndexModel : AbpPageModel
{
    // Same "no Host provider in this ABP version" story as ModuleToggles/AppSettings - see those
    // pages' own comments for the full ABP 10.5.0 investigation.
    private const string GlobalProviderName = "T";

    private readonly ISettingProvider _settingProvider;
    private readonly ISettingManager _settingManager;
    private readonly IFeatureChecker _featureChecker;
    private readonly IFeatureManager _featureManager;
    private readonly IHtmlLocalizer<ErpResource> _l;

    public IndexModel(
        ISettingProvider settingProvider,
        ISettingManager settingManager,
        IFeatureChecker featureChecker,
        IFeatureManager featureManager,
        IHtmlLocalizer<ErpResource> l)
    {
        _settingProvider = settingProvider;
        _settingManager = settingManager;
        _featureChecker = featureChecker;
        _featureManager = featureManager;
        _l = l;
    }

    public bool CanManageAppSettings { get; set; }
    public bool CanManageModules { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public CompanyProfileInput Company { get; set; } = new();

    [BindProperty]
    public EmailSettingsInput Email { get; set; } = new();

    public List<SettingRow> BusinessSettings { get; set; } = new();
    public List<ModuleToggleRow> Modules { get; set; } = new();

    public async Task OnGetAsync()
    {
        CanManageAppSettings = await AuthorizationService.IsGrantedAsync(ErpPermissions.AppSettings.Manage);
        CanManageModules = await AuthorizationService.IsGrantedAsync(ErpPermissions.ModuleToggles.Manage);

        if (CanManageAppSettings)
        {
            await LoadCompanyAsync();
            await LoadEmailAsync();
            await LoadBusinessSettingsAsync();
        }

        if (CanManageModules)
        {
            await LoadModulesAsync();
        }
    }

    public async Task<IActionResult> OnPostSaveCompanyAsync()
    {
        await CheckAppSettingsPermissionAsync();

        await _settingManager.SetGlobalAsync(ErpSettings.CompanyName, Company.Name);
        await _settingManager.SetGlobalAsync(ErpSettings.CompanyAddressLine, Company.AddressLine ?? "");
        await _settingManager.SetGlobalAsync(ErpSettings.CompanyCity, Company.City ?? "");
        await _settingManager.SetGlobalAsync(ErpSettings.CompanyState, Company.State ?? "");
        await _settingManager.SetGlobalAsync(ErpSettings.CompanyPostalCode, Company.PostalCode ?? "");
        await _settingManager.SetGlobalAsync(ErpSettings.CompanyCountry, Company.Country ?? "");
        await _settingManager.SetGlobalAsync(ErpSettings.CompanyPhone, Company.Phone ?? "");
        await _settingManager.SetGlobalAsync(ErpSettings.CompanyEmail, Company.Email ?? "");

        return RedirectToPage(new { tab = "company" });
    }

    public async Task<IActionResult> OnPostSaveEmailAsync()
    {
        await CheckAppSettingsPermissionAsync();

        await _settingManager.SetGlobalAsync(EmailSettingNames.Smtp.Host, Email.SmtpHost ?? "");
        await _settingManager.SetGlobalAsync(EmailSettingNames.Smtp.Port, Email.SmtpPort.ToString());
        await _settingManager.SetGlobalAsync(EmailSettingNames.Smtp.UserName, Email.SmtpUserName ?? "");
        // Blank means "leave the stored password unchanged" - the form never round-trips the
        // real value back to the browser (see LoadEmailAsync), so an empty submit shouldn't wipe it.
        if (!string.IsNullOrEmpty(Email.SmtpPassword))
        {
            await _settingManager.SetGlobalAsync(EmailSettingNames.Smtp.Password, Email.SmtpPassword);
        }
        await _settingManager.SetGlobalAsync(EmailSettingNames.Smtp.EnableSsl, Email.EnableSsl.ToString());
        await _settingManager.SetGlobalAsync(EmailSettingNames.Smtp.UseDefaultCredentials, Email.UseDefaultCredentials.ToString());
        await _settingManager.SetGlobalAsync(EmailSettingNames.DefaultFromAddress, Email.DefaultFromAddress ?? "");
        await _settingManager.SetGlobalAsync(EmailSettingNames.DefaultFromDisplayName, Email.DefaultFromDisplayName ?? "");

        return RedirectToPage(new { tab = "email" });
    }

    public async Task<IActionResult> OnPostSaveBusinessAsync(string name, string value)
    {
        await CheckAppSettingsPermissionAsync();

        if (double.TryParse(value, out var parsed) && parsed > 0)
        {
            await _settingManager.SetGlobalAsync(name, parsed.ToString("0.##"));
        }

        return RedirectToPage(new { tab = "business" });
    }

    public async Task<IActionResult> OnPostToggleModuleAsync(string name)
    {
        if (!CanManageModules)
        {
            throw new AbpAuthorizationException();
        }

        try
        {
            var currentlyEnabled = await _featureChecker.IsEnabledAsync(name);
            await _featureManager.SetAsync(name, currentlyEnabled ? "false" : "true", GlobalProviderName, null);
        }
        catch (AbpException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { tab = "modules" });
    }

    private async Task CheckAppSettingsPermissionAsync()
    {
        if (!await AuthorizationService.IsGrantedAsync(ErpPermissions.AppSettings.Manage))
        {
            throw new AbpAuthorizationException();
        }
    }

    private async Task LoadCompanyAsync()
    {
        Company = new CompanyProfileInput
        {
            Name = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyName) ?? "",
            AddressLine = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyAddressLine),
            City = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyCity),
            State = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyState),
            PostalCode = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyPostalCode),
            Country = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyCountry),
            Phone = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyPhone),
            Email = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyEmail)
        };
    }

    private async Task LoadEmailAsync()
    {
        var host = await _settingProvider.GetOrNullAsync(EmailSettingNames.Smtp.Host);
        var port = await _settingProvider.GetOrNullAsync(EmailSettingNames.Smtp.Port);
        var userName = await _settingProvider.GetOrNullAsync(EmailSettingNames.Smtp.UserName);
        var storedPassword = await _settingProvider.GetOrNullAsync(EmailSettingNames.Smtp.Password);
        var enableSsl = await _settingProvider.GetOrNullAsync(EmailSettingNames.Smtp.EnableSsl);
        var useDefaultCredentials = await _settingProvider.GetOrNullAsync(EmailSettingNames.Smtp.UseDefaultCredentials);
        var fromAddress = await _settingProvider.GetOrNullAsync(EmailSettingNames.DefaultFromAddress);
        var fromDisplayName = await _settingProvider.GetOrNullAsync(EmailSettingNames.DefaultFromDisplayName);

        Email = new EmailSettingsInput
        {
            SmtpHost = host,
            SmtpPort = int.TryParse(port, out var parsedPort) ? parsedPort : 587,
            SmtpUserName = userName,
            // Never round-tripped to the browser - see the OnPostSaveEmailAsync comment.
            SmtpPassword = null,
            HasStoredPassword = !string.IsNullOrEmpty(storedPassword),
            EnableSsl = !bool.TryParse(enableSsl, out var parsedSsl) || parsedSsl,
            UseDefaultCredentials = bool.TryParse(useDefaultCredentials, out var parsedCreds) && parsedCreds,
            DefaultFromAddress = fromAddress,
            DefaultFromDisplayName = fromDisplayName
        };
    }

    private async Task LoadBusinessSettingsAsync()
    {
        BusinessSettings = new List<SettingRow>
        {
            await ToSettingRowAsync(ErpSettings.SlaHoursUrgent, _l["Setting:SlaHoursUrgent"]),
            await ToSettingRowAsync(ErpSettings.SlaHoursHigh, _l["Setting:SlaHoursHigh"]),
            await ToSettingRowAsync(ErpSettings.SlaHoursMedium, _l["Setting:SlaHoursMedium"]),
            await ToSettingRowAsync(ErpSettings.SlaHoursLow, _l["Setting:SlaHoursLow"]),
            await ToSettingRowAsync(ErpSettings.ContractExpiryAlertLeadDays, _l["Setting:ContractExpiryAlertLeadDays"])
        };
    }

    private async Task<SettingRow> ToSettingRowAsync(string name, LocalizedHtmlString displayName)
    {
        return new SettingRow
        {
            Name = name,
            DisplayName = displayName.Value,
            Value = await _settingProvider.GetOrNullAsync(name) ?? string.Empty
        };
    }

    private async Task LoadModulesAsync()
    {
        Modules = new List<ModuleToggleRow>
        {
            await ToModuleRowAsync(ErpFeatures.ProjectManagement, _l["Feature:ProjectManagement"]),
            await ToModuleRowAsync(ErpFeatures.TaxCompliance, _l["Feature:TaxCompliance"]),
            await ToModuleRowAsync(ErpFeatures.ServiceCatalog, _l["Feature:ServiceCatalog"]),
            await ToModuleRowAsync(ErpFeatures.ServiceRequestManagement, _l["Feature:ServiceRequestManagement"]),
            await ToModuleRowAsync(ErpFeatures.AssetManagement, _l["Feature:AssetManagement"]),
            await ToModuleRowAsync(ErpFeatures.KnowledgeManagement, _l["Feature:KnowledgeManagement"]),
            await ToModuleRowAsync(ErpFeatures.PointOfSale, _l["Feature:PointOfSale"]),
            await ToModuleRowAsync(ErpFeatures.PartnerCommission, _l["Feature:PartnerCommission"]),
            await ToModuleRowAsync(ErpFeatures.Cybersecurity, _l["Feature:Cybersecurity"])
        };
    }

    private async Task<ModuleToggleRow> ToModuleRowAsync(string name, LocalizedHtmlString displayName)
    {
        return new ModuleToggleRow
        {
            Name = name,
            DisplayName = displayName.Value,
            IsEnabled = await _featureChecker.IsEnabledAsync(name)
        };
    }

    public class CompanyProfileInput
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; } = string.Empty;

        [StringLength(512)]
        public string? AddressLine { get; set; }

        [StringLength(128)]
        public string? City { get; set; }

        [StringLength(128)]
        public string? State { get; set; }

        [StringLength(32)]
        public string? PostalCode { get; set; }

        [StringLength(128)]
        public string? Country { get; set; }

        [StringLength(64)]
        public string? Phone { get; set; }

        [StringLength(256)]
        public string? Email { get; set; }
    }

    public class EmailSettingsInput
    {
        [StringLength(256)]
        public string? SmtpHost { get; set; }

        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;

        [StringLength(256)]
        public string? SmtpUserName { get; set; }

        [DataType(DataType.Password)]
        public string? SmtpPassword { get; set; }

        public bool HasStoredPassword { get; set; }

        public bool EnableSsl { get; set; } = true;

        public bool UseDefaultCredentials { get; set; }

        [StringLength(256)]
        public string? DefaultFromAddress { get; set; }

        [StringLength(256)]
        public string? DefaultFromDisplayName { get; set; }
    }

    public class SettingRow
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ModuleToggleRow
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
