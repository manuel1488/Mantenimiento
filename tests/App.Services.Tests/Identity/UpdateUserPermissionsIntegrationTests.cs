using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Services.Identity;
using App.Shared.Services;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace App.Services.Tests.Identity;

/// <summary>
/// Integration tests for UpdateUserPermissionsAsync using a real UserManager / RoleManager
/// backed by an EF Core in-memory database.
/// Verifies that the actual DB state (AspNetUserRoles, AspNetUserClaims, SecurityStamp)
/// is correct after each operation.
///
/// Regression for: users with custom permissions retained their previous role, causing
/// its claims to appear in the authenticated ClaimsPrincipal even though the admin had
/// explicitly unchecked those permissions.
/// </summary>
[TestFixture]
[Category("Integration")]
public class UpdateUserPermissionsIntegrationTests
{
    private ApplicationDbContext _db = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private RoleManager<IdentityRole> _roleManager = null!;
    private IdentityService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new ApplicationDbContext(options);

        _userManager = BuildUserManager(_db);
        _roleManager = BuildRoleManager(_db);

        var localizerMock = new Mock<IStringLocalizer<IdentityService>>();
        localizerMock.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        _sut = new IdentityService(
            _userManager,
            _roleManager,
            Mock.Of<IDbContextFactory<ApplicationDbContext>>(),
            Mock.Of<IMapper>(),
            localizerMock.Object,
            Mock.Of<ILogger<IdentityService>>(),
            Mock.Of<ICurrentUserService>(),
            Mock.Of<IDateTime>(),
            Mock.Of<IEmailService>(),
            Mock.Of<IEmailTemplateService>(),
            Options.Create(new ApplicationOptions())
        );
    }

    [TearDown]
    public void TearDown()
    {
        _userManager.Dispose();
        _roleManager.Dispose();
        _db.Dispose();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static UserManager<ApplicationUser> BuildUserManager(ApplicationDbContext db)
    {
        var store = new UserStore<ApplicationUser>(db);
        return new UserManager<ApplicationUser>(
            store,
            new OptionsWrapper<IdentityOptions>(new IdentityOptions
            {
                Password = { RequireDigit = false, RequiredLength = 1, RequireNonAlphanumeric = false, RequireUppercase = false, RequireLowercase = false },
                User = { RequireUniqueEmail = false }
            }),
            new PasswordHasher<ApplicationUser>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<ApplicationUser>>>()
        );
    }

    private static RoleManager<IdentityRole> BuildRoleManager(ApplicationDbContext db)
    {
        var store = new RoleStore<IdentityRole>(db);
        return new RoleManager<IdentityRole>(
            store,
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<IdentityRole>>>()
        );
    }

    private async Task<ApplicationUser> CreateUserAsync(string userName)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName}@test.com",
            FullName = userName,
            IsActive = true,
            CreatedBy = "test",
            CreatedAt = DateTime.UtcNow
        };
        var result = await _userManager.CreateAsync(user);
        Assert.That(result.Succeeded, Is.True, $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return user;
    }

    private async Task<IdentityRole> CreateRoleWithClaimsAsync(string roleName, IEnumerable<string> permissions)
    {
        var role = new IdentityRole(roleName);
        await _roleManager.CreateAsync(role);
        foreach (var p in permissions)
            await _roleManager.AddClaimAsync(role, new Claim(p, p));
        return role;
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Test]
    public async Task RegressionBug_UserWithRole_CustomPermissionsSaved_RoleRemovedFromDb()
    {
        // This is the exact production scenario:
        // 1. brenda is in ShopEmployee, which includes ConvertToSale
        // 2. Admin unchecks ConvertToSale and clicks "Guardar Cambios"
        // 3. Expected: brenda is removed from ShopEmployee so the role's ConvertToSale
        //    claim no longer appears in her auth session

        var shopEmployeePerms = new[]
        {
            "Shop.Quotations.ConvertToSale",
            "Shop.Sales.Create",
            "Shop.Sales.View",
            "Shop.Quotations.View"
        };
        await CreateRoleWithClaimsAsync("ShopEmployee", shopEmployeePerms);

        var brenda = await CreateUserAsync("brenda");
        await _userManager.AddToRoleAsync(brenda, "ShopEmployee");

        var customPermissions = new[]
        {
            "Shop.Sales.Create",
            "Shop.Sales.View",
            "Shop.Quotations.View"
            // ConvertToSale intentionally excluded
        };

        await _sut.UpdateUserPermissionsAsync(brenda.Id, customPermissions);

        var rolesAfter = await _userManager.GetRolesAsync(brenda);
        Assert.That(rolesAfter, Is.Empty,
            "User must have no roles after saving custom permissions: " +
            "role claims would otherwise bleed into the ClaimsPrincipal");

        var claimsAfter = (await _userManager.GetClaimsAsync(brenda))
            .Where(c => c.Type == c.Value)
            .Select(c => c.Value)
            .ToHashSet();

        Assert.That(claimsAfter, Is.EquivalentTo(customPermissions));
        Assert.That(claimsAfter, Does.Not.Contain("Shop.Quotations.ConvertToSale"),
            "ConvertToSale must not be present after being explicitly excluded");
    }

    [Test]
    public async Task PermissionsMatchRole_RoleAssignedInDb()
    {
        // When the saved permissions exactly match a role, the user should be
        // assigned to that role in the DB.
        var vendedorPerms = new[] { "Shop.Sales.View", "Shop.Sales.Create", "Shop.Quotations.View" };
        await CreateRoleWithClaimsAsync("Vendedor", vendedorPerms);

        var jose = await CreateUserAsync("jose");

        await _sut.UpdateUserPermissionsAsync(jose.Id, vendedorPerms);

        var rolesAfter = await _userManager.GetRolesAsync(jose);
        Assert.That(rolesAfter, Is.EquivalentTo(new[] { "Vendedor" }));
    }

    [Test]
    public async Task UserSwitchesBetweenRoles_OldRoleRemovedNewRoleAssigned()
    {
        var role1Perms = new[] { "Shop.Sales.View" };
        var role2Perms = new[] { "Shop.Inventory.View", "Shop.Products.View" };

        await CreateRoleWithClaimsAsync("Role1", role1Perms);
        await CreateRoleWithClaimsAsync("Role2", role2Perms);

        var user = await CreateUserAsync("user-switch");
        await _userManager.AddToRoleAsync(user, "Role1");

        await _sut.UpdateUserPermissionsAsync(user.Id, role2Perms);

        var rolesAfter = await _userManager.GetRolesAsync(user);
        Assert.That(rolesAfter, Is.EquivalentTo(new[] { "Role2" }));
        Assert.That(rolesAfter, Does.Not.Contain("Role1"));
    }

    [Test]
    public async Task SecurityStamp_ChangesInDb_AfterEveryUpdate()
    {
        // A changed SecurityStamp causes Blazor Server's RevalidatingAuthenticationStateProvider
        // to invalidate the active session, forcing a fresh claims load on next request.
        var user = await CreateUserAsync("pao");
        var stampBefore = (await _userManager.FindByIdAsync(user.Id))!.SecurityStamp;

        await _sut.UpdateUserPermissionsAsync(user.Id, ["Shop.Sales.View"]);

        var stampAfter = (await _userManager.FindByIdAsync(user.Id))!.SecurityStamp;
        Assert.That(stampAfter, Is.Not.EqualTo(stampBefore),
            "SecurityStamp must change to invalidate active sessions");
    }

    [Test]
    public async Task GetUserPermissions_AfterCustomSave_ReturnsDirectClaimsOnly_NoBleeding()
    {
        // After saving custom permissions, GetUserPermissionsAsync must return exactly
        // those permissions — not the old role's claims.
        var shopEmployeePerms = new[]
        {
            "Shop.Quotations.ConvertToSale",
            "Shop.Sales.Create",
            "Shop.Sales.View"
        };
        await CreateRoleWithClaimsAsync("ShopEmployee", shopEmployeePerms);

        var admin2 = await CreateUserAsync("admin2");
        await _userManager.AddToRoleAsync(admin2, "ShopEmployee");

        var customPermissions = new[] { "Shop.Sales.View" };
        await _sut.UpdateUserPermissionsAsync(admin2.Id, customPermissions);

        var resultPermissions = (await _sut.GetUserPermissionsAsync(admin2.Id)).ToList();

        Assert.That(resultPermissions, Is.EquivalentTo(customPermissions));
        Assert.That(resultPermissions, Does.Not.Contain("Shop.Quotations.ConvertToSale"),
            "ConvertToSale must not appear — it was removed and the role was stripped");
    }
}
