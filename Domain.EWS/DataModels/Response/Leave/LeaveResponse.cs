using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Response.Leave
{
    public class LeaveResponse
    {
        public int Id { get; set; }
        public LeaveType LeaveType { get; set; }
        public string LeaveTypeDisplay => LeaveType.ToString();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ApprovalStatus LeaveStatus { get; set; }
        public string LeaveStatusDisplay => LeaveStatus.ToString();
        public string? ReviewerRemark { get; set; }
        public bool CanEdit { get; set; }
    }
}