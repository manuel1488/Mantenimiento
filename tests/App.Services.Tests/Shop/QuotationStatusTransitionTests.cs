using AutoMapper;
using Moq;
using NUnit.Framework;

using App.Core.Common;
using App.Core.DTOs.Settings;
using App.Core.Enums.Shop;
using App.Core.Interfaces;
using App.Core.Interfaces.Settings;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Services.Settings;
using App.Services.Shop;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace App.Services.Tests.Shop;

/// <summary>
/// Validates quotation status transition business rules:
///   - Only allowed forward transitions succeed.
///   - Backward transitions are rejected.
///   - Terminal statuses (Rejected, Expired, ConvertedToSale, ConvertedToRemission,
///     and Accepted when reached by the user) cannot be changed via UpdateStatusAsync.
/// </summary>
[TestFixture]
public class QuotationStatusTransitionTests
{
    private static readonly IServiceProvider _efServiceProvider =
        new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

    private QuotationService _service = null!;
    private DbContextOptions<ApplicationDbContext> _dbOptions = null!;

    [SetUp]
    public void Setup()
    {
        _dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseInternalServiceProvider(_efServiceProvider)
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var contextFactory = new TestDbContextFactory(_dbOptions);

        var localizerMock = new Mock<IStringLocalizer<QuotationService>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerMock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] args) =>
                new LocalizedString(key, string.Format(key, args)));

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.UserId).Returns((string?)"test-user");
        currentUserMock.Setup(u => u.FullName).Returns((string?)"Test User");

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);

        // Remaining dependencies are not used by UpdateStatusAsync — mock minimally
        var taxRateMock = new Mock<ITaxRateService>();
        var companySettingsMock = new Mock<ICompanySettingsService>();
        companySettingsMock.Setup(c => c.GetSettingsAsync())
            .ReturnsAsync(new CompanySettingsDto { CountryCode = "MX" });
        var roundingMock = new Mock<IRoundingSettingsService>();
        roundingMock
            .Setup(r => r.ApplyRoundingAsync(It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, CancellationToken _) =>
                Result<(decimal, decimal)>.Success((amount, 0m)));

        _service = new QuotationService(
            contextFactory,
            new Mock<IMapper>().Object,
            NullLogger<QuotationService>.Instance,
            localizerMock.Object,
            currentUserMock.Object,
            dateTimeMock.Object,
            taxRateMock.Object,
            companySettingsMock.Object,
            new Mock<IEmailService>().Object,
            new Mock<IEmailTemplateService>().Object,
            new Mock<IPdfService>().Object,
            new PricingCalculationService(
                taxRateMock.Object,
                companySettingsMock.Object,
                roundingMock.Object,
                NullLogger<PricingCalculationService>.Instance),
            new Mock<IDocumentSequenceService>().Object,
            new Mock<IQuotationSettingsService>().Object,
            roundingMock.Object,
            new Mock<ITaxSettingsService>().Object);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<long> CreateQuotationAsync(QuotationStatus status)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var quotation = new Quotation
        {
            QuotationNumber = "COT-TEST-0001",
            CustomerId = 1,
            QuoteDate = DateTime.UtcNow,
            ValidUntil = DateTime.UtcNow.AddDays(30),
            Status = status,
            IsDeleted = 0,
            CreatedBy = "seed",
            CreatedAt = DateTime.UtcNow,
            ModifiedBy = "seed",
            ModifiedAt = DateTime.UtcNow
        };
        ctx.Quotations.Add(quotation);
        await ctx.SaveChangesAsync();
        return quotation.Id;
    }

    private async Task<QuotationStatus> GetCurrentStatusAsync(long id)
    {
        await using var ctx = new ApplicationDbContext(_dbOptions);
        var q = await ctx.Quotations.FindAsync(id);
        return q!.Status;
    }

    // =========================================================================
    // Valid transitions
    // =========================================================================

    [Test]
    public async Task Draft_To_Pending_Succeeds()
    {
        var id = await CreateQuotationAsync(QuotationStatus.Draft);

        var result = await _service.UpdateStatusAsync(id, QuotationStatus.Pending);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(QuotationStatus.Pending));
    }

    [Test]
    public async Task Pending_To_Accepted_Succeeds()
    {
        var id = await CreateQuotationAsync(QuotationStatus.Pending);

        var result = await _service.UpdateStatusAsync(id, QuotationStatus.Accepted);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(QuotationStatus.Accepted));
    }

    [Test]
    public async Task Pending_To_Rejected_Succeeds()
    {
        var id = await CreateQuotationAsync(QuotationStatus.Pending);

        var result = await _service.UpdateStatusAsync(id, QuotationStatus.Rejected);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(QuotationStatus.Rejected));
    }

    [Test]
    public async Task Pending_To_Expired_Succeeds()
    {
        var id = await CreateQuotationAsync(QuotationStatus.Pending);

        var result = await _service.UpdateStatusAsync(id, QuotationStatus.Expired);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(QuotationStatus.Expired));
    }

    // =========================================================================
    // Skipped / invalid forward transitions
    // =========================================================================

    [TestCase(QuotationStatus.Draft, QuotationStatus.Accepted,          TestName = "Draft_To_Accepted_Fails")]
    [TestCase(QuotationStatus.Draft, QuotationStatus.Rejected,          TestName = "Draft_To_Rejected_Fails")]
    [TestCase(QuotationStatus.Draft, QuotationStatus.Expired,           TestName = "Draft_To_Expired_Fails")]
    [TestCase(QuotationStatus.Draft, QuotationStatus.ConvertedToSale,   TestName = "Draft_To_ConvertedToSale_Fails")]
    [TestCase(QuotationStatus.Draft, QuotationStatus.ConvertedToRemission, TestName = "Draft_To_ConvertedToRemission_Fails")]
    public async Task InvalidForwardTransition_Fails(QuotationStatus from, QuotationStatus to)
    {
        var id = await CreateQuotationAsync(from);

        var result = await _service.UpdateStatusAsync(id, to);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(from), "Status must not change on failure");
    }

    // =========================================================================
    // Backward transitions (forbidden)
    // =========================================================================

    [TestCase(QuotationStatus.Pending,  QuotationStatus.Draft,   TestName = "Pending_To_Draft_Fails")]
    [TestCase(QuotationStatus.Accepted, QuotationStatus.Pending, TestName = "Accepted_To_Pending_Fails")]
    [TestCase(QuotationStatus.Accepted, QuotationStatus.Draft,   TestName = "Accepted_To_Draft_Fails")]
    [TestCase(QuotationStatus.Rejected, QuotationStatus.Pending, TestName = "Rejected_To_Pending_Fails")]
    [TestCase(QuotationStatus.Rejected, QuotationStatus.Draft,   TestName = "Rejected_To_Draft_Fails")]
    [TestCase(QuotationStatus.Expired,  QuotationStatus.Pending, TestName = "Expired_To_Pending_Fails")]
    [TestCase(QuotationStatus.Expired,  QuotationStatus.Draft,   TestName = "Expired_To_Draft_Fails")]
    public async Task BackwardTransition_Fails(QuotationStatus from, QuotationStatus to)
    {
        var id = await CreateQuotationAsync(from);

        var result = await _service.UpdateStatusAsync(id, to);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(from), "Status must not change on failure");
    }

    // =========================================================================
    // Terminal statuses — no manual transition allowed
    // =========================================================================

    [TestCase(QuotationStatus.Accepted,             QuotationStatus.Pending,  TestName = "Accepted_IsTerminalForUser_CannotGoBack")]
    [TestCase(QuotationStatus.Accepted,             QuotationStatus.Rejected, TestName = "Accepted_IsTerminalForUser_CannotGoToRejected")]
    [TestCase(QuotationStatus.Rejected,             QuotationStatus.Accepted, TestName = "Rejected_IsTerminal_CannotBeReactivated")]
    [TestCase(QuotationStatus.Expired,              QuotationStatus.Accepted, TestName = "Expired_IsTerminal_CannotBeReactivated")]
    [TestCase(QuotationStatus.ConvertedToSale,      QuotationStatus.Accepted, TestName = "ConvertedToSale_IsTerminal_CannotBeReactivated")]
    [TestCase(QuotationStatus.ConvertedToSale,      QuotationStatus.Pending,  TestName = "ConvertedToSale_IsTerminal_CannotGoToPending")]
    [TestCase(QuotationStatus.ConvertedToRemission, QuotationStatus.Accepted, TestName = "ConvertedToRemission_IsTerminal_CannotBeReactivated")]
    [TestCase(QuotationStatus.ConvertedToRemission, QuotationStatus.Pending,  TestName = "ConvertedToRemission_IsTerminal_CannotGoToPending")]
    public async Task TerminalStatus_CannotBeChangedManually(QuotationStatus terminal, QuotationStatus attempted)
    {
        var id = await CreateQuotationAsync(terminal);

        var result = await _service.UpdateStatusAsync(id, attempted);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(terminal), "Terminal status must not change");
    }

    // =========================================================================
    // ConvertedToSale and ConvertedToRemission — set by the system,
    // cannot be re-converted via UpdateStatusAsync regardless of target
    // =========================================================================

    [Test]
    public async Task ConvertedToSale_ToAnyStatus_AlwaysFails(
        [Values(
            QuotationStatus.Draft,
            QuotationStatus.Pending,
            QuotationStatus.Accepted,
            QuotationStatus.Rejected,
            QuotationStatus.Expired,
            QuotationStatus.ConvertedToRemission)]
        QuotationStatus target)
    {
        var id = await CreateQuotationAsync(QuotationStatus.ConvertedToSale);

        var result = await _service.UpdateStatusAsync(id, target);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(QuotationStatus.ConvertedToSale));
    }

    [Test]
    public async Task ConvertedToRemission_ToAnyStatus_AlwaysFails(
        [Values(
            QuotationStatus.Draft,
            QuotationStatus.Pending,
            QuotationStatus.Accepted,
            QuotationStatus.Rejected,
            QuotationStatus.Expired,
            QuotationStatus.ConvertedToSale)]
        QuotationStatus target)
    {
        var id = await CreateQuotationAsync(QuotationStatus.ConvertedToRemission);

        var result = await _service.UpdateStatusAsync(id, target);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(await GetCurrentStatusAsync(id), Is.EqualTo(QuotationStatus.ConvertedToRemission));
    }

    // =========================================================================
    // Edge case: non-existent quotation
    // =========================================================================

    [Test]
    public async Task NonExistentQuotation_Fails()
    {
        var result = await _service.UpdateStatusAsync(99999, QuotationStatus.Pending);

        Assert.That(result.IsSuccess, Is.False);
    }

    // =========================================================================
    // Infrastructure
    // =========================================================================

    private class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
            => _options = options;

        public ApplicationDbContext CreateDbContext() => new(_options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken _ = default)
            => Task.FromResult(new ApplicationDbContext(_options));
    }
}
