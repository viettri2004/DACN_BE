using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using AccountService.Application.Interfaces;
using AccountService.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Localization;
using src.Shared.Domain.Entities;
using src.Shared.Resources;

namespace AccountService.Infrastructure.Email
{
    public class EmailService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IConfiguration _config;
        private readonly IOtpService _otpService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        public EmailService(IAccountRepository accountRepository, IOtpService otpService, IConfiguration config, IStringLocalizer<SharedResources> localizer)
        {
            _localizer = localizer;
            _config = config;
            _accountRepository = accountRepository;
            _otpService = otpService;
        }
        public async Task<ApiResponse> SendEmailAsync(string toEmail)
        {
            var user = await _accountRepository.FindUserByEmail(toEmail);
            if (user == null)
                return new ApiResponse("Error", "User not found", null, false);

            var fromEmail = _config["Email:From"];
            var fromPassword = _config["Email:Password"];

            string otp = await _otpService.GenerateOtpAsync($"ResetPassword:{toEmail}", TimeSpan.FromMinutes(5));

            string subject = "3T - OTP for Password Reset";
            string body = $@"
    <!DOCTYPE html>
    <html lang='en' xmlns='http://www.w3.org/1999/xhtml'>
    <head>
        <meta http-equiv='Content-Type' content='text/html; charset=UTF-8'/>
        <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
        <title>3T - OTP for Password Reset</title>
    </head>
    <body style='margin:0; padding:0; background-color:#f4f4f7;'>
        <table role='presentation' width='100%' cellspacing='0' cellpadding='0' border='0' style='background-color:#f4f4f7;'>
            <tr>
                <td align='center' style='padding:24px;'>
                    <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0' style='width:100%; max-width:600px; background-color:#ffffff; border-radius:8px; overflow:hidden;'>
                        <tr>
                            <td style='background-color:#007BFF; padding:20px 24px; color:#ffffff; font-family:Arial, Helvetica, sans-serif; font-size:20px; font-weight:bold;'>
                                3T
                            </td>
                        </tr>
                        <tr>
                            <td style='padding:24px; font-family:Arial, Helvetica, sans-serif; color:#333333; font-size:15px; line-height:1.6;'>
                                <h2 style='margin:0 0 8px 0; font-size:20px; color:#111827; font-weight:700;'>Password Reset Request</h2>
                                <p style='margin:0 0 12px 0;'>Dear user,</p>
                                <p style='margin:0 0 12px 0;'>We received a request to reset your password. Please use the following One-Time Password (OTP) to proceed:</p>
                                <div style='margin:16px 0; text-align:center;'>
                                    <div style='display:inline-block; padding:12px 18px; border:1px solid #e5e7eb; border-radius:8px; background-color:#f9fafb; font-size:24px; letter-spacing:4px; font-weight:700; color:#d90c0c;'>
                                        {otp}
                                    </div>
                                    <div style='font-size:12px; color:#6b7280; margin-top:8px;'>Valid for 5 minutes</div>
                                </div>
                                <p style='margin:0 0 12px 0;'>Please do not share this code with anyone. If you did not request a password reset, please ignore this email or contact support immediately.</p>
                                <p style='margin:16px 0 0 0;'>Best regards,<br /><strong>3T Team</strong></p>
                            </td>
                        </tr>
                        <tr>
                            <td style='padding:16px 24px; border-top:1px solid #e5e7eb; background-color:#fafafa; font-family:Arial, Helvetica, sans-serif; font-size:12px; color:#6b7280;'>
                                This is an automated message. Please do not reply to this email.
                            </td>
                        </tr>
                    </table>
                    <div style='height:24px; line-height:24px;'>&#160;</div>
                </td>
            </tr>
        </table>
    </body>
    </html>";


            var message = new MailMessage
            {
                From = new MailAddress(fromEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toEmail));

            using var smtp = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true
            };

            await smtp.SendMailAsync(message);

            return new ApiResponse("Success", _localizer["OtpSentSuccessfully"].Value, null, true);

        }

    }

}
