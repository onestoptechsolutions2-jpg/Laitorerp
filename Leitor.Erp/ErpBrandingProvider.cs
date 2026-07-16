using Microsoft.Extensions.Localization;
using Leitor.Erp.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace Leitor.Erp;

[Dependency(ReplaceServices = true)]
public class ErpBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<ErpResource> _localizer;

    public ErpBrandingProvider(IStringLocalizer<ErpResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
