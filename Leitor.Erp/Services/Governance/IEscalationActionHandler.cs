using System.Threading.Tasks;
using Leitor.Erp.Entities.Governance;

namespace Leitor.Erp.Services.Governance;

// Register each implementation explicitly in ErpModule.ConfigureServices (see that registration's
// own comment - the ITransientDependency convention was tried first and empirically didn't expose
// implementations under this interface for IEnumerable<IEscalationActionHandler> resolution).
// Replaces the hardcoded entity-type switch DeletionRequestAppService.DispatchDeleteAsync uses -
// see EscalationItem.cs's own comment.
public interface IEscalationActionHandler
{
    string ActionType { get; }

    Task ExecuteAsync(EscalationItem item);
}
