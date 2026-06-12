using Application.EWS.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace Application.EWS.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SmtpClient BuildClient(out string username, out string senderName)
        {
            var smtp = _configuration.GetSection("Smtp");

            var host = smtp["Host"]!;
            var port = int.Parse(smtp["Port"] ?? "587");
            username = smtp["Username"]!;
            var password = smtp["Password"]!;
            senderName = smtp["SenderName"] ?? "EWS Support";

            return new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };
        }

        private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            using var client = BuildClient(out var username, out var senderName);

            var message = new MailMessage
            {
                From = new MailAddress(username, senderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail, toName));

            await client.SendMailAsync(message);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string toName, string resetToken)
        {
            var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:4200";
            var resetLink = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}";

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>EWS - Reset Password</title>
  <style>
    body {{ margin:0; padding:0; background-color:#f3f4f6; font-family:Arial,sans-serif; }}
    .email-wrapper {{ width:100%; background-color:#f3f4f6; padding:20px 0; }}
    .email-container {{ max-width:600px; width:100%; margin:0 auto; background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.08); }}
    .email-header {{ background-color:#2563eb; padding:20px 30px; text-align:center; }}
    .email-header h2 {{ color:#ffffff; margin:0; font-size:20px; }}
    .email-body {{ padding:30px 24px; }}
    .email-footer {{ background-color:#f9fafb; padding:12px; text-align:center; border-top:1px solid #e5e7eb; }}
    .email-footer p {{ font-size:12px; color:#9ca3af; margin:0; }}
    p {{ color:#374151; line-height:1.6; margin:0 0 12px 0; font-size:14px; }}
    .btn-wrap {{ text-align:center; margin:28px 0; }}
    .btn {{ background-color:#2563eb; color:#ffffff; padding:12px 28px; text-decoration:none; border-radius:6px; font-weight:600; font-size:14px; display:inline-block; }}
    .text-gray {{ color:#6b7280; font-size:13px; }}
  </style>
</head>
<body>
  <div class='email-wrapper'>
    <div class='email-container'>
      <div class='email-header'>
        <h2>EWS Support</h2>
      </div>
      <div class='email-body'>
        <p>Hello <strong>{toName}</strong>,</p>
        <p>We received a request to reset your EWS account password.</p>
        <div class='btn-wrap'>
          <a href='{resetLink}' class='btn'>Reset Password</a>
        </div>
        <p>This link expires in <strong>15 minutes</strong>.</p>
        <p>Or copy this link into your browser:<br/>
           <a href='{resetLink}' style='color:#2563eb;word-break:break-all;font-size:13px;'>{resetLink}</a>
        </p>
        <p class='text-gray'>If you did not request this, you can safely ignore this email.</p>
        <p style='margin-top:24px;'>Regards,<br><strong>EWS Team</strong></p>
      </div>
      <div class='email-footer'>
        <p>&copy; {DateTime.UtcNow.Year} EWS. All rights reserved.</p>
      </div>
    </div>
  </div>
</body>
</html>";

            await SendAsync(toEmail, toName, "Reset Your EWS Password", html);
        }

        public async Task SendAttendanceRejectedEmailAsync( string toEmail, string toName, DateTime attendanceDate, string? reviewerRemark)
        {
            var dateDisplay = attendanceDate.ToString("dddd, dd MMMM yyyy");

            var remarkRow = string.IsNullOrWhiteSpace(reviewerRemark)
                ? string.Empty
                : $@"
        <tr>
          <td style='padding:8px 12px; background-color:#fef2f2; border-radius:4px;'>
            <strong style='color:#991b1b;'>Reviewer's Remark:</strong>
            <span style='color:#374151;'> {reviewerRemark}</span>
          </td>
        </tr>";

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>EWS - Attendance Rejected</title>
  <style>
    body {{ margin:0; padding:0; background-color:#f3f4f6; font-family:Arial,sans-serif; }}
    .email-wrapper {{ width:100%; background-color:#f3f4f6; padding:20px 0; }}
    .email-container {{ max-width:600px; width:100%; margin:0 auto; background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.08); }}
    .email-header {{ background-color:#dc2626; padding:20px 30px; text-align:center; }}
    .email-header h2 {{ color:#ffffff; margin:0; font-size:20px; }}
    .email-body {{ padding:30px 24px; }}
    .email-footer {{ background-color:#f9fafb; padding:12px; text-align:center; border-top:1px solid #e5e7eb; }}
    .email-footer p {{ font-size:12px; color:#9ca3af; margin:0; }}
    p {{ color:#374151; line-height:1.6; margin:0 0 12px 0; font-size:14px; }}
    .info-box {{ background-color:#fef2f2; border:1px solid #fecaca; border-radius:6px; padding:16px 20px; margin:20px 0; }}
    .info-box table {{ width:100%; border-collapse:collapse; }}
    .info-box td {{ padding:6px 0; font-size:14px; color:#374151; vertical-align:top; }}
    .label {{ font-weight:600; color:#991b1b; width:160px; }}
    .badge-rejected {{ display:inline-block; background-color:#fee2e2; color:#991b1b; border:1px solid #fca5a5; border-radius:4px; padding:2px 10px; font-size:13px; font-weight:600; }}
    .note-box {{ background-color:#fffbeb; border:1px solid #fcd34d; border-radius:6px; padding:14px 18px; margin:20px 0; font-size:13px; color:#92400e; }}
  </style>
</head>
<body>
  <div class='email-wrapper'>
    <div class='email-container'>

      <div class='email-header'>
        <h2>&#10008; Attendance Rejected</h2>
      </div>

      <div class='email-body'>
        <p>Hello <strong>{toName}</strong>,</p>
        <p>
          Your attendance submission has been <strong style='color:#dc2626;'>rejected</strong>
          by your reporting person. Please review the details below and resubmit.
        </p>

        <div class='info-box'>
          <table>
            <tr>
              <td class='label'>Date:</td>
              <td>{dateDisplay}</td>
            </tr>
            <tr>
              <td class='label'>Status:</td>
              <td><span class='badge-rejected'>Rejected</span></td>
            </tr>
            {remarkRow}
          </table>
        </div>

        <div class='note-box'>
          &#9432;&nbsp; You can now log in to the EWS portal and resubmit your attendance
          for <strong>{dateDisplay}</strong>.
        </div>

        <p style='margin-top:24px;'>Regards,<br><strong>EWS Team</strong></p>
      </div>

      <div class='email-footer'>
        <p>&copy; {DateTime.UtcNow.Year} EWS. All rights reserved.</p>
      </div>

    </div>
  </div>
</body>
</html>";

            await SendAsync(toEmail, toName, $"Attendance Rejected – {dateDisplay}", html);
        }

        public async Task SendLeaveApprovedEmailAsync(string toEmail, string toName, DateTime startDate, DateTime endDate, string leaveType, string? reviewerRemark)
        {
            var startDisplay = startDate.ToString("dddd, dd MMMM yyyy");
            var endDisplay = endDate.ToString("dddd, dd MMMM yyyy");
            var dateRange = startDate.Date == endDate.Date ? startDisplay : $"{startDisplay} – {endDisplay}";

            var remarkRow = string.IsNullOrWhiteSpace(reviewerRemark)
                ? string.Empty
                : $@"
        <tr>
          <td class='label'>Remark:</td>
          <td>{reviewerRemark}</td>
        </tr>";

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>EWS - Leave Approved</title>
  <style>
    body {{ margin:0; padding:0; background-color:#f3f4f6; font-family:Arial,sans-serif; }}
    .email-wrapper {{ width:100%; background-color:#f3f4f6; padding:20px 0; }}
    .email-container {{ max-width:600px; width:100%; margin:0 auto; background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.08); }}
    .email-header {{ background-color:#16a34a; padding:20px 30px; text-align:center; }}
    .email-header h2 {{ color:#ffffff; margin:0; font-size:20px; }}
    .email-body {{ padding:30px 24px; }}
    .email-footer {{ background-color:#f9fafb; padding:12px; text-align:center; border-top:1px solid #e5e7eb; }}
    .email-footer p {{ font-size:12px; color:#9ca3af; margin:0; }}
    p {{ color:#374151; line-height:1.6; margin:0 0 12px 0; font-size:14px; }}
    .info-box {{ background-color:#f0fdf4; border:1px solid #bbf7d0; border-radius:6px; padding:16px 20px; margin:20px 0; }}
    .info-box table {{ width:100%; border-collapse:collapse; }}
    .info-box td {{ padding:6px 0; font-size:14px; color:#374151; vertical-align:top; }}
    .label {{ font-weight:600; color:#15803d; width:130px; }}
    .badge-approved {{ display:inline-block; background-color:#dcfce7; color:#15803d; border:1px solid #86efac; border-radius:4px; padding:2px 10px; font-size:13px; font-weight:600; }}
  </style>
</head>
<body>
  <div class='email-wrapper'>
    <div class='email-container'>
      <div class='email-header'>
        <h2>&#10004; Leave Approved</h2>
      </div>
      <div class='email-body'>
        <p>Hello <strong>{toName}</strong>,</p>
        <p>Your leave application has been <strong style='color:#16a34a;'>approved</strong>.</p>
        <div class='info-box'>
          <table>
            <tr><td class='label'>Leave Type:</td><td>{leaveType}</td></tr>
            <tr><td class='label'>Date(s):</td><td>{dateRange}</td></tr>
            <tr><td class='label'>Status:</td><td><span class='badge-approved'>Approved</span></td></tr>
            {remarkRow}
          </table>
        </div>
        <p style='margin-top:24px;'>Regards,<br><strong>EWS Team</strong></p>
      </div>
      <div class='email-footer'>
        <p>&copy; {DateTime.UtcNow.Year} EWS. All rights reserved.</p>
      </div>
    </div>
  </div>
</body>
</html>";

            await SendAsync(toEmail, toName, $"Leave Approved – {dateRange}", html);
        }

        public async Task SendLeaveRejectedEmailAsync(string toEmail, string toName, DateTime startDate, DateTime endDate, string leaveType, string? reviewerRemark)
        {
            var startDisplay = startDate.ToString("dddd, dd MMMM yyyy");
            var endDisplay = endDate.ToString("dddd, dd MMMM yyyy");
            var dateRange = startDate.Date == endDate.Date ? startDisplay : $"{startDisplay} – {endDisplay}";

            var remarkRow = string.IsNullOrWhiteSpace(reviewerRemark)
                ? string.Empty
                : $@"
        <tr>
          <td style='padding:8px 12px; background-color:#fef2f2; border-radius:4px;'>
            <strong style='color:#991b1b;'>Reviewer's Remark:</strong>
            <span style='color:#374151;'> {reviewerRemark}</span>
          </td>
        </tr>";

            var html = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='UTF-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>EWS - Leave Rejected</title>
  <style>
    body {{ margin:0; padding:0; background-color:#f3f4f6; font-family:Arial,sans-serif; }}
    .email-wrapper {{ width:100%; background-color:#f3f4f6; padding:20px 0; }}
    .email-container {{ max-width:600px; width:100%; margin:0 auto; background-color:#ffffff; border-radius:8px; overflow:hidden; box-shadow:0 2px 8px rgba(0,0,0,0.08); }}
    .email-header {{ background-color:#dc2626; padding:20px 30px; text-align:center; }}
    .email-header h2 {{ color:#ffffff; margin:0; font-size:20px; }}
    .email-body {{ padding:30px 24px; }}
    .email-footer {{ background-color:#f9fafb; padding:12px; text-align:center; border-top:1px solid #e5e7eb; }}
    .email-footer p {{ font-size:12px; color:#9ca3af; margin:0; }}
    p {{ color:#374151; line-height:1.6; margin:0 0 12px 0; font-size:14px; }}
    .info-box {{ background-color:#fef2f2; border:1px solid #fecaca; border-radius:6px; padding:16px 20px; margin:20px 0; }}
    .info-box table {{ width:100%; border-collapse:collapse; }}
    .info-box td {{ padding:6px 0; font-size:14px; color:#374151; vertical-align:top; }}
    .label {{ font-weight:600; color:#991b1b; width:130px; }}
    .badge-rejected {{ display:inline-block; background-color:#fee2e2; color:#991b1b; border:1px solid #fca5a5; border-radius:4px; padding:2px 10px; font-size:13px; font-weight:600; }}
    .note-box {{ background-color:#fffbeb; border:1px solid #fcd34d; border-radius:6px; padding:14px 18px; margin:20px 0; font-size:13px; color:#92400e; }}
  </style>
</head>
<body>
  <div class='email-wrapper'>
    <div class='email-container'>
      <div class='email-header'>
        <h2>&#10008; Leave Rejected</h2>
      </div>
      <div class='email-body'>
        <p>Hello <strong>{toName}</strong>,</p>
        <p>Your leave application has been <strong style='color:#dc2626;'>rejected</strong>.</p>
        <div class='info-box'>
          <table>
            <tr><td class='label'>Leave Type:</td><td>{leaveType}</td></tr>
            <tr><td class='label'>Date(s):</td><td>{dateRange}</td></tr>
            <tr><td class='label'>Status:</td><td><span class='badge-rejected'>Rejected</span></td></tr>
            {remarkRow}
          </table>
        </div>
        <div class='note-box'>
          &#9432;&nbsp; You may submit a new leave application if needed.
        </div>
        <p style='margin-top:24px;'>Regards,<br><strong>EWS Team</strong></p>
      </div>
      <div class='email-footer'>
        <p>&copy; {DateTime.UtcNow.Year} EWS. All rights reserved.</p>
      </div>
    </div>
  </div>
</body>
</html>";

            await SendAsync(toEmail, toName, $"Leave Rejected – {dateRange}", html);
        }    }
}