using MailKit.Net.Smtp;
using MimeKit;

namespace RegisterMicroservice.Api.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration config;
        private readonly ILogger<EmailSender> logger;

        public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
        {
            this.config = config;
            this.logger = logger;
        }
        public async Task SendEmailAsync(MailboxAddress to, string subject, string content)
        {
            logger.LogInformation("Sending email message method start working");
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(config["Email:CompanyName"], config["Email:From"]));
            emailMessage.To.Add(to);
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = content };

            using var client = new SmtpClient();
            logger.LogInformation("Smtp client has been created");
            await client.ConnectAsync(config["Email:Smtp"], int.Parse(config["Email:Port"]), true);
            logger.LogInformation("Smtp has been connected");
            await client.AuthenticateAsync(config["Email:UserName"], config["Email:Password"]);
            logger.LogInformation("Smtp user has been authenticated");
            await client.SendAsync(emailMessage);
            logger.LogInformation("Message has been successfully sent");

            await client.DisconnectAsync(true);
        }
    }
}
