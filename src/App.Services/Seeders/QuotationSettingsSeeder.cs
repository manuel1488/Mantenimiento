using App.Core.Interfaces;
using App.Models.Data.Contexts;
using App.Models.Settings;
using App.Services.Resources.PdfTemplates;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Seeders;

public class QuotationSettingsSeeder : IQuotationSettingsSeeder
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<QuotationSettingsSeeder> _logger;
    private const string SystemUser = "System";

    public QuotationSettingsSeeder(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<QuotationSettingsSeeder> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (await context.QuotationSettings.AnyAsync())
                return;

            var now = DateTime.UtcNow;
            context.QuotationSettings.Add(new QuotationSettings
            {
                HtmlBody = DefaultQuotationTemplate.Html,
                CustomCss = DefaultQuotationTemplate.Css,
                CreatedBy = SystemUser,
                CreatedAt = now,
                ModifiedBy = SystemUser,
                ModifiedAt = now
            });

            await context.SaveChangesAsync();
            _logger.LogInformation("Quotation settings seeded with default template");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding quotation settings");
            throw;
        }
    }
}
