using System;

namespace Leitor.Erp.Pages.Shared;

// Shared overdue/due-soon computation for CustomerTask/ProjectTask, used by both the My Workspace
// reminders rollup (Services/Workspace/MyWorkspaceAppService.cs) and the plain task tables on
// Customer/Project Detail - one place for the DueDate check instead of copy-pasting it per page.
public static class TaskDueStatus
{
    public static bool IsOverdue(DateTime? dueDate, bool isCompleted, DateTime now)
        => dueDate.HasValue && !isCompleted && dueDate.Value < now;

    public static bool IsDueSoon(DateTime? dueDate, bool isCompleted, DateTime now, int dueSoonLeadDays)
        => dueDate.HasValue && !isCompleted && dueDate.Value >= now && dueDate.Value <= now.AddDays(dueSoonLeadDays);

    public static string PillClass(DateTime? dueDate, bool isCompleted, DateTime now, int dueSoonLeadDays)
    {
        if (IsOverdue(dueDate, isCompleted, now))
        {
            return "pill-danger";
        }

        if (IsDueSoon(dueDate, isCompleted, now, dueSoonLeadDays))
        {
            return "pill-warning";
        }

        return string.Empty;
    }
}
