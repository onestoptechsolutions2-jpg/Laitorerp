using System.Threading.Tasks;
using Leitor.Erp.Services.Dtos.Workspace;
using Leitor.Erp.Services.Workspace;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Leitor.Erp.Pages.Workspace;

[Authorize]
public class IndexModel : AbpPageModel
{
    private readonly MyWorkspaceAppService _myWorkspaceAppService;

    public IndexModel(MyWorkspaceAppService myWorkspaceAppService)
    {
        _myWorkspaceAppService = myWorkspaceAppService;
    }

    public MyWorkspaceDto Workspace { get; set; } = new();

    public async Task OnGetAsync()
    {
        Workspace = await _myWorkspaceAppService.GetAsync();
    }
}
