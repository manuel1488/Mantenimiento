using System.Security.Claims;
using App.Core.Common;
using App.Core.Constants;
using App.Core.DTOs.Billing.Mexico;
using App.Core.Interfaces;
using App.Core.Interfaces.Billing;
using App.Core.Models.Email;
using App.Models.Billing;
using App.Models.Data.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Billing;

public class MexicoStampAlertService : IMexicoStampAlertService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ISwSapienService _swSapienService;
    private readonly IEmailService _emailService;
    private readonly UserManager<App.Models.Identity.ApplicationUser> _userManager;
    private readonly ILogger<MexicoStampAlertService> _logger;
    private readonly IStringLocalizer<MexicoStampAlertService> _localizer;

    public MexicoStampAlertService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ISwSapienService swSapienService,
        IEmailService emailService,
        UserManager<App.Models.Identity.ApplicationUser> userManager,
        ILogger<MexicoStampAlertService> logger,
        IStringLocalizer<MexicoStampAlertService> localizer)
    {
        _contextFactory = contextFactory;
        _swSapienService = swSapienService;
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<Result<MexicoStampBalanceDto>> GetBalanceAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var localCount = await context.MexicoInvoices
                .AsNoTracking()
                .CountAsync(i => i.IsStamped);

            var remoteResult = await _swSapienService.GetStampBalanceAsync();
            if (!remoteResult.IsSuccess)
            {
                _logger.LogWarning("Could not fetch stamp balance from PAC: {Error}", remoteResult.Error);
                return Result<MexicoStampBalanceDto>.Success(new MexicoStampBalanceDto
                {
                    LocalInvoicesStamped = localCount,
                    FetchedAt = DateTime.UtcNow,
                    IsConfigured = false
                });
            }

            var data = remoteResult.Value!;
            return Result<MexicoStampBalanceDto>.Success(new MexicoStampBalanceDto
            {
                Available = data.StampsBalance,
                UsedAtProvider = data.StampsUsed,
                TotalAssigned = data.StampsAssigned,
                IsUnlimited = data.IsUnlimited,
                ExpirationDate = data.ExpirationDate,
                LocalInvoicesStamped = localCount,
                FetchedAt = DateTime.UtcNow,
                IsConfigured = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stamp balance");
            return Result<MexicoStampBalanceDto>.Failure(_localizer["Error fetching stamp balance"]);
        }
    }

    public async Task<StampAlertSettingsDto> GetAlertSettingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.MexicoStampAlertSettings.FirstOrDefaultAsync();
        if (settings == null) return new StampAlertSettingsDto();

        return new StampAlertSettingsDto
        {
            LowStampThreshold = settings.LowStampThreshold,
            AlertEnabled = settings.AlertEnabled,
            AlertCooldownHours = settings.AlertCooldownHours,
            LastAlertSentAt = settings.LastAlertSentAt
        };
    }

    public async Task<Result<StampAlertSettingsDto>> SaveAlertSettingsAsync(UpdateStampAlertSettingsDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.MexicoStampAlertSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new MexicoStampAlertSettings
                {
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    ModifiedBy = "System",
                    ModifiedAt = DateTime.UtcNow
                };
                context.MexicoStampAlertSettings.Add(settings);
            }
            else
            {
                settings.ModifiedBy = "System";
                settings.ModifiedAt = DateTime.UtcNow;
            }

            settings.LowStampThreshold = dto.LowStampThreshold;
            settings.AlertEnabled = dto.AlertEnabled;
            settings.AlertCooldownHours = dto.AlertCooldownHours;

            await context.SaveChangesAsync();

            return Result<StampAlertSettingsDto>.Success(new StampAlertSettingsDto
            {
                LowStampThreshold = settings.LowStampThreshold,
                AlertEnabled = settings.AlertEnabled,
                AlertCooldownHours = settings.AlertCooldownHours,
                LastAlertSentAt = settings.LastAlertSentAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving stamp alert settings");
            return Result<StampAlertSettingsDto>.Failure(_localizer["Error saving alert settings"]);
        }
    }

    public async Task CheckAndAlertIfNeededAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var alertSettings = await context.MexicoStampAlertSettings.FirstOrDefaultAsync();

            if (alertSettings == null || !alertSettings.AlertEnabled)
                return;

            // Respect cooldown
            if (alertSettings.LastAlertSentAt.HasValue)
            {
                var elapsed = DateTime.UtcNow - alertSettings.LastAlertSentAt.Value;
                if (elapsed.TotalHours < alertSettings.AlertCooldownHours)
                    return;
            }

            var balanceResult = await _swSapienService.GetStampBalanceAsync();
            if (!balanceResult.IsSuccess) return;

            var balance = balanceResult.Value!;
            if (balance.IsUnlimited || balance.StampsBalance > alertSettings.LowStampThreshold)
                return;

            _logger.LogWarning(
                "Low stamp balance: {Available} available (threshold: {Threshold})",
                balance.StampsBalance, alertSettings.LowStampThreshold);

            await SendAlertEmailsAsync(balance.StampsBalance, alertSettings.LowStampThreshold);

            alertSettings.LastAlertSentAt = DateTime.UtcNow;
            alertSettings.ModifiedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in stamp balance alert check");
        }
    }

    private async Task SendAlertEmailsAsync(int available, int threshold)
    {
        try
        {
            var usersWithClaim = await _userManager.GetUsersForClaimAsync(
                new Claim(ApplicationClaims.Shop.ReceiveStampAlertEmails,
                          ApplicationClaims.Shop.ReceiveStampAlertEmails));

            var emails = usersWithClaim
                .Where(u => u.IsActive && u.EmailConfirmed && !string.IsNullOrEmpty(u.Email))
                .Select(u => u.Email!)
                .Distinct()
                .ToList();

            if (!emails.Any())
            {
                _logger.LogInformation("No recipients configured for stamp balance alerts");
                return;
            }

            var subject = _localizer["Low stamp balance alert: {0} available", available].Value;
            var body = BuildAlertEmailHtml(available, threshold);

            foreach (var email in emails)
            {
                try
                {
                    await _emailService.SendAsync(new EmailMessage
                    {
                        To = email,
                        Subject = subject,
                        Body = body,
                        IsHtml = true,
                        Priority = EmailPriority.High
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending stamp balance alert to {Email}", email);
                }
            }

            _logger.LogInformation("Stamp balance alerts sent to {Count} recipients", emails.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending stamp balance alert emails");
        }
    }

    private string BuildAlertEmailHtml(int available, int threshold) => $@"<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""UTF-8""/><title>{_localizer["Stamp Balance Alert"]}</title></head>
<body style=""font-family:Arial,sans-serif;font-size:14px;color:#333;margin:0;padding:0;"">
  <div style=""max-width:600px;margin:30px auto;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.12);"">
    <div style=""background:#E53935;color:#fff;padding:20px 24px;"">
      <h2 style=""margin:0;font-size:18px;"">&#9888; {_localizer["Low Stamp Balance"]}</h2>
    </div>
    <div style=""background:#fff;padding:24px;"">
      <p>{_localizer["The PAC stamp balance (SW Sapien) has reached the configured minimum level."]}</p>
      <table style=""width:100%;border-collapse:collapse;margin:16px 0;"">
        <tr style=""background:#fafafa;"">
          <td style=""padding:12px 16px;border:1px solid #eee;font-weight:bold;"">{_localizer["Available stamps"]}</td>
          <td style=""padding:12px 16px;border:1px solid #eee;color:#E53935;font-weight:bold;font-size:22px;"">{available}</td>
        </tr>
        <tr>
          <td style=""padding:12px 16px;border:1px solid #eee;font-weight:bold;"">{_localizer["Alert threshold"]}</td>
          <td style=""padding:12px 16px;border:1px solid #eee;"">{threshold}</td>
        </tr>
      </table>
      <p>{_localizer["Please purchase additional stamps to continue issuing electronic invoices (CFDI)."]}</p>
      <p style=""color:#999;font-size:11px;margin-top:24px;"">
        {_localizer["This message was automatically generated. Users with the 'Receive stamp alerts' permission receive these notifications."]}
      </p>
    </div>
  </div>
</body>
</html>";
}
