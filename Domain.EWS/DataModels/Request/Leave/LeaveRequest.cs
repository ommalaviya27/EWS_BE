using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Request.Leave
{
    public class ApplyLeaveRequest
    {
        public LeaveType LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class EditLeaveRequest
    {
        public LeaveType LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class ReviewLeaveRequest
    {
        public ApprovalStatus LeaveStatus { get; set; }
        public string? ReviewerRemark { get; set; }
    }
}