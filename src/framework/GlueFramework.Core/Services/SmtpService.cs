using GlueFramework.Core.Abstractions;
using GlueFramework.Core.ConfigurationOptions;
using Microsoft.Extensions.Options;
using System.Net.Mail;

namespace GlueFramework.Core.Services
{
    public class SmtpService : ISmtpService
    {
        private MailSettings _options;
        public SmtpService(IOptions<MailSettings> options)
        {
            _options = options.Value;
        }

        public void Send(System.Net.Mail.MailMessage message)
        {
            SmtpClient client = new SmtpClient(_options.smtp, _options.port);
            message.From = new MailAddress(_options.from);
            client.Credentials = new System.Net.NetworkCredential(_options.username, _options.password);
            client.EnableSsl = true;
            client.Send(message);
        }

        public async Task SendMailAsync(System.Net.Mail.MailMessage message)
        {
            SmtpClient client = new SmtpClient(_options.smtp, _options.port);
            if (message.To.Any() == false)
            {
                message.To.Add(_options.contact);
            }
            message.From = new MailAddress(_options.from);
            client.Credentials = new System.Net.NetworkCredential(_options.username, _options.password);
            client.EnableSsl = true;
            await client.SendMailAsync(message);
        }
    }
}
