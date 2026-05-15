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
            var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";

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
    }
}