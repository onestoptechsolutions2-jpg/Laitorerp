using Leitor.Erp.Localization;
using Volo.Abp.Application.Services;

namespace Leitor.Erp.Services;

/* Inherit your application services from this class. */
public abstract class ErpAppService : ApplicationService
{
    protected ErpAppService()
    {
        LocalizationResource = typeof(ErpResource);
    }
}