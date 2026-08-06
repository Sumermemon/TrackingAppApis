using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using RunningCompetition.Contracts.Services;
using RunningCompetition.Shared.Settings;

namespace RunningCompetition.Infrastructure.Email;

/// <summary>
/// Email service using MailKit/SMTP for transactional emails.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    /// <summary>Initializes a new instance of <see cref="EmailService"/>.</summary>
    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendEmailVerificationAsync(string toEmail, string toName, string token, CancellationToken cancellationToken = default)
    {
        var verifyUrl = $"https://api.runningapp.com/api/v1/auth/verify-email?token={Uri.EscapeDataString(token)}";
        var html = $"""
            <h2>Verify Your Email</h2>
            <p>Hi {toName}, please verify your email address by clicking the button below:</p>
            <a href="{verifyUrl}" style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block;">
                Verify Email
            </a>
            <p>This link expires in 24 hours.</p>
            """;

        await SendAsync(toEmail, toName, "Verify Your Email — Running Competition", html, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetAsync(string toEmail, string toName, string token, CancellationToken cancellationToken = default)
    {
        var resetUrl = $"https://app.runningapp.com/reset-password?token={Uri.EscapeDataString(token)}";
        var html = $"""
            <h2>Reset Your Password</h2>
            <p>Hi {toName}, click the button below to reset your password:</p>
            <a href="{resetUrl}" style="background:#dc2626;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block;">
                Reset Password
            </a>
            <p>This link expires in 2 hours. If you didn't request this, you can safely ignore this email.</p>
            """;

        await SendAsync(toEmail, toName, "Reset Your Password — Running Competition", html, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendWelcomeEmailAsync(string toEmail, string toName, CancellationToken cancellationToken = default)
    {
        var html = $"""
            <h2>Welcome to Running Competition! 🏃</h2>
            <p>Hi {toName}, you've successfully joined. Lace up your shoes and start your first run!</p>
            <p>Track your pace, compete on leaderboards, and earn achievements along the way.</p>
            """;

        await SendAsync(toEmail, toName, "Welcome to Running Competition!", html, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Host, _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls,
                cancellationToken);
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent to {Email} with subject '{Subject}'", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            throw;
        }
    }
}
