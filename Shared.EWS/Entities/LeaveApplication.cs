using Shared.EWS.Enums;

namespace Shared.EWS.Entities
{
    public class LeaveApplication : BaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? ReviewerId { get; set; }
        public LeaveType LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ApprovalStatus LeaveStatus { get; set; } = ApprovalStatus.Pending;
        public string? ReviewerRemark { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public User User { get; set; } = null!;
        public User? Reviewer { get; set; }
    }
}