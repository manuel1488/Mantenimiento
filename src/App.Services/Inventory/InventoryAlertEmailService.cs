using App.Core.Constants;
using App.Core.Interfaces;
using App.Core.Models.Email;
using App.Core.Options;
using App.Models.Data.Contexts;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Globalization;
using System.Security.Claims;

namespace App.Services.Inventory;

/// <summary>
/// Service for sending email alerts when inventory stock levels reach critical thresholds
/// </summary>
public class InventoryAlertEmailService : IInventoryAlertEmailService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly UserManager<Models.Identity.ApplicationUser> _userManager;
    private readonly ILogger<InventoryAlertEmailService> _logger;
    private readonly IStringLocalizer<InventoryAlertEmailService> _localizer;
    private readonly ApplicationOptions _applicationOptions;
    private readonly ICompanySettingsService _companySettingsService;

    public InventoryAlertEmailService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        UserManager<Models.Identity.ApplicationUser> userManager,
        ILogger<InventoryAlertEmailService> logger,
        IStringLocalizer<InventoryAlertEmailService> localizer,
        IOptions<ApplicationOptions> applicationOptions,
        ICompanySettingsService companySettingsService)
    {
        _contextFactory = contextFactory;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _userManager = userManager;
        _logger = logger;
        _localizer = localizer;
        _applicationOptions = applicationOptions.Value;
        _companySettingsService = companySettingsService;
    }

    // Prefers the store's own display name (Settings > General); falls back to the deployment's
    // brand profile name only if no CompanySettings row exists yet.
    private async Task<string> GetCompanyDisplayNameAsync()
    {
        var companySettings = await _companySettingsService.GetSettingsAsync();
        return string.IsNullOrWhiteSpace(companySettings?.CompanyName)
            ? _applicationOptions.Name
            : companySettings.CompanyName;
    }

    public async Task SendLowStockAlertAsync(InventoryAlertInfo alertInfo, CancellationToken cancellationToken = default)
    {
        if (alertInfo.AlertType != InventoryAlertType.LowStock)
        {
            _logger.LogWarning("Attempted to send low stock alert for non-low stock alert type: {AlertType}", alertInfo.AlertType);
            return;
        }

        await SendInventoryAlertAsync(alertInfo, cancellationToken);
    }

    public async Task SendOverStockAlertAsync(InventoryAlertInfo alertInfo, CancellationToken cancellationToken = default)
    {
        if (alertInfo.AlertType != InventoryAlertType.OverStock)
        {
            _logger.LogWarning("Attempted to send over stock alert for non-over stock alert type: {AlertType}", alertInfo.AlertType);
            return;
        }

        await SendInventoryAlertAsync(alertInfo, cancellationToken);
    }

    public async Task SendInventoryAlertAsync(InventoryAlertInfo alertInfo, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get recipients who should receive inventory alerts
            var recipients = await GetInventoryAlertRecipientsAsync(cancellationToken);

            if (!recipients.Any())
            {
                _logger.LogInformation("No recipients found for inventory alerts. Skipping email notification.");
                return;
            }

            var appName = await GetCompanyDisplayNameAsync();

            // Prepare email data with application URLs
            var emailData = new Dictionary<string, object>
            {
                { "culture", CultureInfo.CurrentUICulture.Name },
                { "alert_type", alertInfo.AlertType },
                { "product_name", alertInfo.ProductName },
                { "location_name", alertInfo.LocationName },
                { "current_stock", alertInfo.CurrentStock },
                { "threshold", alertInfo.Threshold ?? 0 },
                { "alert_date", DateTime.UtcNow },
                { "is_low_stock", alertInfo.AlertType == InventoryAlertType.LowStock },
                { "is_over_stock", alertInfo.AlertType == InventoryAlertType.OverStock },

                // Application URLs
                { "base_url", _applicationOptions.BaseUrl.TrimEnd('/') },
                { "inventory_url", $"{_applicationOptions.BaseUrl.TrimEnd('/')}/shop/inventory" },
                { "alerts_url", $"{_applicationOptions.BaseUrl.TrimEnd('/')}/shop/inventory#alerts" },
                { "app_name", appName },
                { "app_version", _applicationOptions.Version }
            };

            // Select appropriate template based on alert type
            var templateName = alertInfo.AlertType == InventoryAlertType.LowStock
                ? "inventory-low-stock-alert"
                : "inventory-over-stock-alert";

            // Generate email content
            var emailContent = await _emailTemplateService.GetTemplateAsync(templateName, emailData, cancellationToken);

            // Prepare email subject with application name
            var subject = alertInfo.AlertType == InventoryAlertType.LowStock
                ? _localizer["Low Stock Alert - {0} | {1}", alertInfo.ProductName, appName]
                : _localizer["Over Stock Alert - {0} | {1}", alertInfo.ProductName, appName];

            // Send email to each recipient
            var emailTasks = recipients.Select(async recipient =>
            {
                try
                {
                    var emailMessage = new EmailMessage
                    {
                        To = recipient,
                        Subject = subject,
                        Body = emailContent,
                        IsHtml = true,
                        Priority = EmailPriority.High,
                        Headers = new Dictionary<string, string>
                        {
                            { "X-App-Alert-Type", alertInfo.AlertType },
                            { "X-App-Product", alertInfo.ProductName },
                            { "X-App-Location", alertInfo.LocationName }
                        }
                    };

                    var result = await _emailService.SendAsync(emailMessage, cancellationToken);

                    if (result.Success)
                    {
                        _logger.LogInformation("Successfully sent inventory alert email to {Recipient} for product {ProductName}",
                            recipient, alertInfo.ProductName);
                    }
                    else
                    {
                        _logger.LogError("Failed to send inventory alert email to {Recipient}: {Error}",
                            recipient, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending inventory alert email to {Recipient}", recipient);
                }
            });

            await Task.WhenAll(emailTasks);

            _logger.LogInformation("Inventory alert processing completed for {AlertType} alert on product {ProductName} in location {LocationName}. Sent to {RecipientCount} recipients.",
                alertInfo.AlertType, alertInfo.ProductName, alertInfo.LocationName, recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending inventory alerts for product {ProductName} in location {LocationName}",
                alertInfo.ProductName, alertInfo.LocationName);
        }
    }

    public async Task<IList<string>> GetInventoryAlertRecipientsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all users who have the permission to receive inventory alert emails
            var usersWithClaim = await _userManager.GetUsersForClaimAsync(
                new Claim(ApplicationClaims.Shop.ReceiveInventoryAlertEmails, ApplicationClaims.Shop.ReceiveInventoryAlertEmails));

            // Filter active users and those with valid email addresses
            var recipients = usersWithClaim
                .Where(user => user.IsActive &&
                              !string.IsNullOrWhiteSpace(user.Email) &&
                              user.EmailConfirmed &&
                              user.IsDeleted == 0)
                .Select(user => user.Email!)
                .Distinct()
                .ToList();

            _logger.LogDebug("Found {Count} recipients for inventory alerts", recipients.Count);

            return recipients;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory alert recipients");
            return new List<string>();
        }
    }
}