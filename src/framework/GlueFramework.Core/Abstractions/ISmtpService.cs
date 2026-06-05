namespace GlueFramework.Core.Abstractions
{
    public interface ISmtpService
    {
        void Send(System.Net.Mail.MailMessage message);
        Task SendMailAsync(System.Net.Mail.MailMessage message);
    }
}
