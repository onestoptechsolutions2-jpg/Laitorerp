using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;

namespace Leitor.Erp.Services.Governance;

// Implement alongside ITransientDependency - ABP's convention-based registrar picks up every
// implementation automatically (same pattern as every IDataSeedContributor in this codebase), so
// EscalationItemAppService's injected IEnumerable<IEscalationActionHandler> resolves all of them
// with zero central-file changes. Replaces the hardcoded entity-type switch
// DeletionRequestAppService.DispatchDeleteAsync uses - see EscalationItem.cs's own comment.
public interface IEscalationActionHandler
{
    string ActionType { get; }

    Task ExecuteAsync(EscalationItem item);
}
