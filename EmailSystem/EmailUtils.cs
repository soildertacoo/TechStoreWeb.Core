using System;

using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

namespace TechStoreWeb.Core.Models.EmailSystem
{
    public class EmailUtils
    {
        private IConfiguration _configuration;
        
        public EmailUtils(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        // public async static Task<bool> SendMail(string _from, string _to, string _subject, string _body, SmtpClient _smtpClient) {
        //     // Tạo nội dung Email
        //     MailMessage message = new MailMessage (
        //         from: _from,
        //         to: _to,
        //         subject: _subject,
        //         body: _body
        //     );
        //     message.BodyEncoding = System.Text.Encoding.UTF8;
        //     message.SubjectEncoding = System.Text.Encoding.UTF8;
        //     message.IsBodyHtml = true;
        //     message.ReplyToList.Add (new MailAddress (_from));
        //     message.Sender = new MailAddress (_from);


        //     try {
        //         await _smtpClient.SendMailAsync(message);
        //         return true;
        //     } catch (Exception ex) {
        //         Console.WriteLine (ex.Message);
        //         return false;
        //     }
        // }
        // public static async Task<bool> SendMailGoogleSmtpNet (string _from, string _to, string _subject, 
        //                                                     string _body, string _smtpUsername, string _smtpPassword) {

        //     MailMessage message = new MailMessage (
        //         from: _from,
        //         to: _to,
        //         subject: _subject,
        //         body: _body
        //     );
        //     message.BodyEncoding = System.Text.Encoding.UTF8;
        //     message.SubjectEncoding = System.Text.Encoding.UTF8;
        //     message.IsBodyHtml = true;
        //     message.ReplyToList.Add (new MailAddress (_from));
        //     message.Sender = new MailAddress (_from);

        //     // Tạo SmtpClient kết nối đến smtp.gmail.com
        //     using (SmtpClient client = new SmtpClient ("smtp.gmail.com")) {
        //         client.Port = 587;
        //         client.Credentials = new NetworkCredential (_smtpUsername, _smtpPassword);
        //         client.EnableSsl = true;
        //         return await SendMail(_from, _to, _subject, _body, client);
        //     }

        // }
        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var message = new MimeMessage();
            string? emailAPP = _configuration["Authentication:Google:emailAPP"]; 
            string? passAPP =_configuration["Authentication:Google:passAPP"]; 
            // Thay email của bạn vào đây
            message.From.Add(new MailboxAddress("Hỗ trợ kỹ thuật", "soildertacoo@gmail.com"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                
                // Kết nối đến SMTP của Gmail
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

                // Đăng nhập (Dùng Email của bạn và MẬT KHẨU ỨNG DỤNG 16 KÝ TỰ)
                await client.AuthenticateAsync(emailAPP,  passAPP);

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}