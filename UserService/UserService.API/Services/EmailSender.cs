using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace UserService.API.Services
{
    public class EmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Создаем MIME-сообщение
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("NoReply", "noreply@example.com"));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;
            
            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            // Подключение к MailDev (SMTP на localhost:1025)
            using (var client = new SmtpClient())
            {
                // Не используем TLS, так как MailDev не требует его
                await client.ConnectAsync("maildev", 1025, SecureSocketOptions.None);
                
                // MailDev обычно не требует аутентификации, поэтому вызов AuthenticateAsync не нужен
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
