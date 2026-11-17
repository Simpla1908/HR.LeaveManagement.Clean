using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Models.Email;
using Microsoft.Extensions.Options;

namespace HR.LeaveManagement.Infrastructure.Services;

// Simulation locale, comme le backend console de Django. Aucun appel réseau.
public sealed class ConsoleEmailSender(IOptions<EmailSettings> options) : IEmailSender
{
    public async Task<bool> SendEmail(EmailMessage email)
    {
        var settings = options.Value;
        // Le contenu reste dans la console de développement, hors des fichiers Serilog.
        await Console.Out.WriteLineAsync($"""
            ---------- EMAIL LOCAL (aucun envoi réel) ----------
            From: {settings.FromName} <{settings.FromAddress}>
            To: {email.To}
            Subject: {email.Subject}

            {email.Body}
            ---------------------------------------------------
            """);
        return true;
    }
}
