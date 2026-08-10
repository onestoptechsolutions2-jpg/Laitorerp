using System.Threading.Tasks;
using Leitor.Erp.Documents;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Settings;

namespace Leitor.Erp.Settings;

// Resolves the company letterhead info (shown on every generated PDF) from admin-editable
// Settings rather than the old appsettings-only ErpCompanyOptions/IOptions binding - the one
// genuinely inaccessible-via-UI config gap found during the operations-hub review. Still returns
// the same ErpCompanyOptions shape so every PDF document class (Documents/*PdfDocument.cs) needed
// zero changes - only the 8 call sites that used to inject IOptions<ErpCompanyOptions> now call
// this instead. ITransientDependency so ABP's conventional registrar picks it up automatically -
// same as any AppService, just without the ApplicationService base class.
public class ErpCompanyProfileProvider : ITransientDependency
{
    private readonly ISettingProvider _settingProvider;

    public ErpCompanyProfileProvider(ISettingProvider settingProvider)
    {
        _settingProvider = settingProvider;
    }

    public async Task<ErpCompanyOptions> GetAsync()
    {
        return new ErpCompanyOptions
        {
            Name = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyName) ?? "Leitor Investment Company Ltd",
            AddressLine = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyAddressLine),
            City = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyCity),
            State = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyState),
            PostalCode = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyPostalCode),
            Country = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyCountry),
            Phone = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyPhone),
            Email = await _settingProvider.GetOrNullAsync(ErpSettings.CompanyEmail)
        };
    }
}
