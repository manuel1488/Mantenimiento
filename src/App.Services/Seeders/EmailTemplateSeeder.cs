using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Services.Resources.EmailTemplates;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class EmailTemplateSeeder : IEmailTemplateSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<EmailTemplateSeeder> _logger;
    private const string SystemUser = "System";

    public EmailTemplateSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<EmailTemplateSeeder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (await context.EmailTemplateSettings.AnyAsync())
                return;

            var now = DateTime.UtcNow;
            context.EmailTemplateSettings.Add(new EmailTemplateSettings
            {
                Name = "invoice-cfdi",
                HtmlContent = DefaultEmailTemplates.ClassicHtml,
                CssContent = DefaultEmailTemplates.ClassicCss,
                CreatedBy = SystemUser,
                CreatedAt = now,
                ModifiedBy = SystemUser,
                ModifiedAt = now
            });

            await context.SaveChangesAsync();
            _logger.LogInformation("Email template 'invoice-cfdi' seeded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding email templates");
            throw;
        }
    }
}
