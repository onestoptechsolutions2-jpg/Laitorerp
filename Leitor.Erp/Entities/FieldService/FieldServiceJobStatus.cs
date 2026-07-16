namespace Leitor.Erp.Entities.FieldService;

// ServiceNow-informed: Completed and Incomplete are both terminal "visit concluded" outcomes
// (Incomplete = visited but couldn't finish, e.g. parts missing/site inaccessible), distinct from
// Cancelled (never attempted). Matches ServiceNow FSM's "Closed Complete" vs "Closed Incomplete".
public enum FieldServiceJobStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Incomplete = 3,
    Cancelled = 4
}
