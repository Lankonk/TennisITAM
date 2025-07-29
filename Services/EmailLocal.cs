using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using TennisITAM.Models;
using Azure.Core;
using TennisITAM.Areas.Identity.Pages.Account;
namespace TennisITAM.Services
{
    public class EmailLocal : IEmailSender<Usuario>
    {
        private readonly ILogger<EmailLocal> _logger;

        public EmailLocal(ILogger<EmailLocal> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation($"Email to: {email}");
            _logger.LogInformation($"Subject: {subject}");
            _logger.LogInformation($"Message: {htmlMessage}");
            return Task.CompletedTask;
        }
        //los metodos posteriores no se usan, pero si no estan truena el programa
        public Task SendConfirmationLinkAsync(Usuario user, string email, string confirmationLink)
        {
            throw new NotImplementedException();
        }

        public Task SendPasswordResetCodeAsync(Usuario user, string email, string resetCode)
        {
            throw new NotImplementedException();
        }

        public Task SendPasswordResetLinkAsync(Usuario user, string email, string resetLink)
        {
            throw new NotImplementedException();
        }
    }
}
