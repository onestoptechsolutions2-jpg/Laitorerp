using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;

namespace Leitor.Erp.Services.Governance;

// System-wide "block deletion if dependents exist" - a record that's still referenced by another
// independent top-level aggregate (a loose Guid reference, since this app deliberately has no real
// database foreign keys - see project stack notes) can't be deleted until those references are
// cleared, rather than silently orphaning them. This is deliberately separate from cascade-delete:
// a record's own *owned* children (e.g. TicketMessage under Ticket, ProjectTask under Project)
// should still be deleted along with their parent - only cross-aggregate references get blocked.
// Each check is a deferred count so unrelated repositories aren't queried once a blocker is
// already found for a preceding check that matters more to report first.
public static class DependencyGuard
{
    public static async Task EnsureDeletableAsync(params (Func<Task<int>> CountAsync, string Label)[] checks)
    {
        var blockers = new List<string>();

        foreach (var (countAsync, label) in checks)
        {
            var count = await countAsync();
            if (count > 0)
            {
                blockers.Add(count == 1 ? $"1 {label}" : $"{count} {label}s");
            }
        }

        if (blockers.Count > 0)
        {
            throw new UserFriendlyException(
                $"Cannot delete - still referenced by {string.Join(", ", blockers)}. Remove or reassign these first.");
        }
    }
}
