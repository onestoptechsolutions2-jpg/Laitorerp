namespace Leitor.Erp.Entities.Hr;

// Kenya Employment Act leave categories. Balance/entitlement tracking (how many Annual days an
// employee has left) is a separate, later feature - out of scope here, this only categorizes a
// request.
public enum LeaveType
{
    Annual = 0,
    Sick = 1,
    Maternity = 2,
    Paternity = 3,
    Compassionate = 4,
    Unpaid = 5,
    Other = 6
}
