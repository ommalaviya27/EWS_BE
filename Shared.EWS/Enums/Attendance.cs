namespace Shared.EWS.Enums
{
    public enum AttendanceStatus
    {
        Present_WFO = 1,
        Present_WFH = 2,
        Absent = 3,
        HalfDay_WFO = 4,
        HalfDay_WFH = 5
    }

    public enum ApprovalStatus
    {
        Pending  = 1,
        Approved = 2,
        Rejected = 3
    }

    public enum LeaveType
    {
        FullDay  = 1,
        HalfDay  = 2,
    }
}