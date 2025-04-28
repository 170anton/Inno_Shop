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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("NoReply", "noreply@example.com"));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;
            
            var bodyBuilder = new BodyBuilder { HtmlBody = htmlMessage };
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("mailhog", 1025, SecureSocketOptions.None);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
