using Shared.EWS.Enums;

namespace Shared.EWS.Entities
{
    public class Attendance : BaseEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
        public int? ReviewerId { get; set; }
        public string? ReviewerRemark { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public User User { get; set; } = null!;
        public User? Reviewer { get; set; }
    }
}