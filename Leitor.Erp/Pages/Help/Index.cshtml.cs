using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Help;

// No permission policy - every logged-in staff account can read this, same as Workspace. Purely
// static reference content (no AppService calls), so there's nothing here to gate.
[Authorize]
public class IndexModel : AbpPageModel
{
}
