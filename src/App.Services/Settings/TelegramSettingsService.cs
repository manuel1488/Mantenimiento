using AutoMapper;
using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Interfaces;
using App.Core.Interfaces.Notifications;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Settings;

public class TelegramSettingsService : ITelegramSettingsService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<TelegramSettingsService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ITelegramApiClient _telegramApiClient;

    public TelegramSettingsService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<TelegramSettingsService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        ITelegramApiClient telegramApiClient)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _telegramApiClient = telegramApiClient;
    }

    public async Task<TelegramSettingsDto?> GetSettingsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.TelegramSettings
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            return settings != null ? _mapper.Map<TelegramSettingsDto>(settings) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Telegram settings");
            throw;
        }
    }

    public async Task<TelegramSettingsDto> UpdateSettingsAsync(UpdateTelegramSettingsDto updateDto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var settings = await context.TelegramSettings.OrderBy(x => x.Id).FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new TelegramSettings
                {
                    CreatedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown",
                    CreatedAt = _dateTime.Now
                };
                context.TelegramSettings.Add(settings);
            }

            _mapper.Map(updateDto, settings);

            // Defend against stray whitespace from copy/paste — an untrimmed value here (e.g. a
            // leading space before "https://") makes Telegram reject setWebhook with a generic
            // "invalid webhook URL specified" that gives no hint the URL itself was the problem.
            settings.BotToken = settings.BotToken?.Trim();
            settings.WebhookBaseUrl = settings.WebhookBaseUrl?.Trim();

            if (string.IsNullOrWhiteSpace(settings.WebhookSecretToken))
                settings.WebhookSecretToken = Guid.NewGuid().ToString("N");

            settings.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
            settings.ModifiedAt = _dateTime.Now;

            string? webhookRegistrationError = null;

            if (settings.Enabled && !string.IsNullOrWhiteSpace(settings.BotToken) && !string.IsNullOrWhiteSpace(settings.WebhookBaseUrl))
            {
                var webhookUrl = $"{settings.WebhookBaseUrl.TrimEnd('/')}/api/telegram/webhook";
                var webhookResult = await _telegramApiClient.SetWebhookAsync(settings.BotToken, webhookUrl, settings.WebhookSecretToken);
                if (!webhookResult.IsSuccess)
                {
                    webhookRegistrationError = webhookResult.Error;
                    _logger.LogWarning("Failed to register Telegram webhook: {Error}", webhookResult.Error);
                }

                var usernameResult = await _telegramApiClient.GetBotUsernameAsync(settings.BotToken);
                if (usernameResult.IsSuccess)
                {
                    settings.BotUsername = usernameResult.Value;
                }
                else
                {
                    webhookRegistrationError ??= usernameResult.Error;
                    _logger.LogWarning("Failed to fetch Telegram bot username: {Error}", usernameResult.Error);
                }
            }

            await context.SaveChangesAsync();

            var dto = _mapper.Map<TelegramSettingsDto>(settings);
            dto.WebhookRegistrationError = webhookRegistrationError;
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Telegram settings");
            throw;
        }
    }

    public async Task<Result<string>> TestBotTokenAsync(string botToken, CancellationToken cancellationToken = default)
    {
        botToken = botToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(botToken))
            return Result<string>.Failure("Bot token is required");

        var result = await _telegramApiClient.GetBotUsernameAsync(botToken, cancellationToken);
        if (!result.IsSuccess)
            return result;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            var settings = await context.TelegramSettings.OrderBy(x => x.Id).FirstOrDefaultAsync(cancellationToken);

            // Only persist against an existing row — a bare test shouldn't create settings on its own,
            // that stays Save's job. Also skip if the token being tested isn't the one already saved,
            // so we never attach a username to a token the admin hasn't committed yet.
            if (settings != null && settings.BotToken == botToken)
            {
                settings.BotUsername = result.Value;
                settings.ModifiedBy = await _currentUserService.GetFullNameAsync() ?? "Unknown";
                settings.ModifiedAt = _dateTime.Now;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting bot username after test");
        }

        return result;
    }
}
