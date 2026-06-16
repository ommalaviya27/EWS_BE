using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Response.Leave
{
    public class LeaveResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public LeaveType LeaveType { get; set; }
        public string LeaveTypeDisplay => LeaveType.ToString();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ApprovalStatus LeaveStatus { get; set; }
        public string LeaveStatusDisplay => LeaveStatus.ToString();
        public int? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public string? ReviewerRemark { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public bool CanEdit { get; set; }
    }
}