using System;
using System.Threading.Tasks;
using Leitor.Erp.Entities.Projects;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace Leitor.Erp.Services.Governance;

// Same static-method-with-injected-repository shape as FiscalPeriodGuard - a focused "throw if
// this transition isn't allowed yet" check, distinct from DependencyGuard (which blocks DELETION
// when a dependent exists, not a status transition). Called from ProjectAppService's update path
// when a Project's Status is moving into Active - a project can't start work until whatever it
// depends on (e.g. CCTV/video-intercom needing network infrastructure in place first) is done.
public static class ProjectDependencyGuard
{
    public static async Task EnsureDependencyCompleteAsync(IRepository<Project, Guid> projectRepository, Guid? dependsOnProjectId)
    {
        if (!dependsOnProjectId.HasValue)
        {
            return;
        }

        var dependency = await projectRepository.FindAsync(dependsOnProjectId.Value);
        if (dependency != null && dependency.Status != ProjectStatus.Completed)
        {
            throw new UserFriendlyException(
                $"This project depends on \"{dependency.Title}\", which isn't Completed yet ({dependency.Status}). Finish that project first.");
        }
    }
}
