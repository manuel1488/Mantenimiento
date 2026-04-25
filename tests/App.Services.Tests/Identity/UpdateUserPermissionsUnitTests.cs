using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Services.Identity;
using App.Shared.Services;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace App.Services.Tests.Identity;

/// <summary>
/// Unit tests for UpdateUserPermissionsAsync using mocked UserManager / RoleManager.
/// Verifies branching logic without touching a real database.
///
/// Regression for: users with "custom permissions" (no matching role) kept their old role
/// assigned, causing role claims to bleed into the auth session even though the admin
/// had unchecked those permissions.
/// </summary>
[TestFixture]
[Category("Unit")]
public class UpdateUserPermissionsUnitTests
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private Mock<RoleManager<IdentityRole>> _roleManagerMock = null!;
    private IdentityService _sut = null!;

    private readonly ApplicationUser _testUser = new()
    {
        Id = "user-1",
        UserName = "testuser",
        FullName = "Test User",
        IsActive = true,
        CreatedBy = "system",
        CreatedAt = DateTime.UtcNow
    };

    [SetUp]
    public void SetUp()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null, null, null, null, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole>>();
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            roleStore.Object, null, null, null, null);

        // Default happy-path returns
        _userManagerMock.Setup(x => x.FindByIdAsync(_testUser.Id)).ReturnsAsync(_testUser);
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);
        _userManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>())).ReturnsAsync([]);
        _userManagerMock.Setup(x => x.RemoveClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddClaimsAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<Claim>>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        _roleManagerMock.Setup(x => x.Roles).Returns(Enumerable.Empty<IdentityRole>().AsQueryable());
        _roleManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<IdentityRole>())).ReturnsAsync([]);

        var localizerMock = new Mock<IStringLocalizer<IdentityService>>();
        localizerMock.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        _sut = new IdentityService(
            _userManagerMock.Object,
            _roleManagerMock.Object,
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

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void SetupUserInRole(string roleName, IEnumerable<string> rolePermissions)
    {
        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync([roleName]);

        var role = new IdentityRole { Id = $"role-{roleName}", Name = roleName };
        _roleManagerMock.Setup(x => x.Roles)
            .Returns(new List<IdentityRole> { role }.AsQueryable());
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(rolePermissions.Select(p => new Claim(p, p)).ToList());
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CustomPermissions_UserHasRole_RoleIsRemoved()
    {
        // Reproduce the production bug: ShopEmployee includes ConvertToSale,
        // admin unchecks it and saves → the role must be stripped so its claims
        // don't survive in the auth session.
        SetupUserInRole("ShopEmployee", [
            "Shop.Quotations.ConvertToSale",
            "Shop.Sales.Create",
            "Shop.Sales.View"
        ]);

        var customPermissions = new[] { "Shop.Sales.View" }; // no matching role

        await _sut.UpdateUserPermissionsAsync(_testUser.Id, customPermissions);

        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<IEnumerable<string>>(r => r.Contains("ShopEmployee"))),
            Times.Once);

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task CustomPermissions_UserHasNoRole_NoRoleOperations()
    {
        // No prior role — saving custom permissions should not try to remove/add any role.
        var customPermissions = new[] { "Shop.Sales.View" };

        await _sut.UpdateUserPermissionsAsync(_testUser.Id, customPermissions);

        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()),
            Times.Never);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task MatchingRole_DifferentCurrentRole_SwitchesRole()
    {
        // Permissions saved match "Vendedor" exactly → user should be moved to Vendedor.
        var vendedorPerms = new[] { "Shop.Sales.View", "Shop.Sales.Create", "Shop.Quotations.View" };

        var vendedorRole = new IdentityRole { Id = "role-Vendedor", Name = "Vendedor" };
        var otherRole = new IdentityRole { Id = "role-Other", Name = "OtherRole" };

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(["OtherRole"]);
        _roleManagerMock.Setup(x => x.Roles)
            .Returns(new List<IdentityRole> { vendedorRole, otherRole }.AsQueryable());
        _roleManagerMock.Setup(x => x.GetClaimsAsync(vendedorRole))
            .ReturnsAsync(vendedorPerms.Select(p => new Claim(p, p)).ToList());
        _roleManagerMock.Setup(x => x.GetClaimsAsync(otherRole))
            .ReturnsAsync([new Claim("Admin.Access", "Admin.Access")]);

        await _sut.UpdateUserPermissionsAsync(_testUser.Id, vendedorPerms);

        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<IEnumerable<string>>(r => r.Contains("OtherRole"))), Times.Once);

        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Vendedor"),
            Times.Once);
    }

    [Test]
    public async Task MatchingRole_AlreadyInCorrectRole_SkipsRoleOperations()
    {
        // User is already in the role that matches the permissions — no DB churn needed.
        var rolePerms = new[] { "Shop.Sales.View", "Shop.Sales.Create" };

        _userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(["ShopEmployee"]);

        var role = new IdentityRole { Id = "role-ShopEmployee", Name = "ShopEmployee" };
        _roleManagerMock.Setup(x => x.Roles).Returns(new List<IdentityRole> { role }.AsQueryable());
        _roleManagerMock.Setup(x => x.GetClaimsAsync(role))
            .ReturnsAsync(rolePerms.Select(p => new Claim(p, p)).ToList());

        await _sut.UpdateUserPermissionsAsync(_testUser.Id, rolePerms);

        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()),
            Times.Never);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Always_UpdatesSecurityStamp()
    {
        // Security stamp must be refreshed after every permission change so active
        // Blazor sessions re-authenticate and pick up the new claim set.
        await _sut.UpdateUserPermissionsAsync(_testUser.Id, ["Shop.Sales.View"]);

        _userManagerMock.Verify(x => x.UpdateSecurityStampAsync(_testUser), Times.Once);
    }

    [Test]
    public async Task ExistingDirectClaims_ReplacedWithExactNewSet()
    {
        // Old direct claims are removed first, then exactly the new permissions are written.
        var existingClaims = new List<Claim>
        {
            new("Shop.Quotations.ConvertToSale", "Shop.Quotations.ConvertToSale"),
            new("Shop.Sales.View", "Shop.Sales.View")
        };
        _userManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(existingClaims);

        var newPermissions = new[] { "Shop.Inventory.View" };

        await _sut.UpdateUserPermissionsAsync(_testUser.Id, newPermissions);

        _userManagerMock.Verify(x => x.RemoveClaimsAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<IEnumerable<Claim>>(c => c.Count() == 2)), Times.Once);

        _userManagerMock.Verify(x => x.AddClaimsAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<IEnumerable<Claim>>(c =>
                c.Count() == 1 &&
                c.Single().Type == "Shop.Inventory.View" &&
                c.Single().Value == "Shop.Inventory.View")), Times.Once);
    }
}
