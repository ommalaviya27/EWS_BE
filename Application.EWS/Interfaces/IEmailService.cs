namespace Application.EWS.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken);
        Task SendAttendanceRejectedEmailAsync(string toEmail, string toName, DateTime attendanceDate, string? reviewerRemark);
        Task SendLeaveApprovedEmailAsync(string toEmail, string toName, DateTime startDate, DateTime endDate, string leaveType, string? reviewerRemark);
        Task SendLeaveRejectedEmailAsync(string toEmail, string toName, DateTime startDate, DateTime endDate, string leaveType, string? reviewerRemark);
    }
}