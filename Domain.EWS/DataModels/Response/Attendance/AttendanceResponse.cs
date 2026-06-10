using Shared.EWS.Enums;

namespace Domain.EWS.DataModels.Response.Attendance
{
    public class AttendanceResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public string StatusDisplay => Status.ToString().Replace("_", " ");
        public ApprovalStatus ApprovalStatus { get; set; }
        public string ApprovalStatusDisplay => ApprovalStatus.ToString();
        public int? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public string? ReviewerRemark { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AttendanceDayResponse
    {
        public int Day { get; set; }
        public string DayName { get; set; } = string.Empty;
        public bool IsWeekend { get; set; }
        public bool IsToday { get; set; }
        public int? AttendanceId { get; set; }
        public AttendanceStatus? Status { get; set; }
        public string? StatusDisplay => Status?.ToString().Replace("_", " ");
        public ApprovalStatus? ApprovalStatus { get; set; }
        public string? ApprovalStatusDisplay => ApprovalStatus?.ToString();
        public bool IsAutoAbsent { get; set; }
    }

    public class AttendanceMonthResponse
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalDays { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public List<AttendanceDayResponse> Days { get; set; } = new();
    }
}