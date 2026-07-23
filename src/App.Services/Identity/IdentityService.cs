using System.Globalization;
using System.Security.Claims;

using App.Core.Common;
using App.Core.DTOs.Identity;
using App.Core.Identity.Interfaces;
using App.Core.Interfaces;
using App.Core.Options;
using App.Models.Data.Contexts;
using App.Models.Identity;
using App.Services.Email;
using App.Shared.Services;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Services.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<IdentityService> _localizer;
    private readonly ILogger<IdentityService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ApplicationOptions _applicationOptions;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        IStringLocalizer<IdentityService> localizer,
        ILogger<IdentityService> logger,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IOptions<ApplicationOptions> applicationOptions)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _contextFactory = contextFactory;
        _mapper = mapper;
        _localizer = localizer;
        _logger = logger;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _applicationOptions = applicationOptions.Value;
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(string userId)
    {
        try
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Result<UserDto>.Failure(_localizer["Invalid user ID"]);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<UserDto>.Failure(_localizer["User not found"]);
            }

            var userDto = _mapper.Map<UserDto>(user);

            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return Result<UserDto>.Failure(_localizer["An error occurred while retrieving user information"]);
        }
    }

    public async Task<(int TotalCount, IList<UserDto> Users)> GetUsersAsync(
        int page = 1,
        int pageSize = 10,
        string? searchString = null,
        string? role = null,
        bool? isActive = null)
    {
        try
        {
            // Start with all users
            IQueryable<ApplicationUser> query = _userManager.Users;

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(u =>
                    (u.UserName != null && u.UserName.Contains(searchString)) ||
                    (u.Email != null && u.Email.Contains(searchString)) ||
                    u.FullName.Contains(searchString));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Project to DTO and apply pagination
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            // Filter by role if specified
            if (!string.IsNullOrWhiteSpace(role))
            {
                var filteredUsers = new List<UserDto>();
                foreach (var userDto in users)
                {
                    var user = await _userManager.FindByIdAsync(userDto.Id);
                    if (user != null && await _userManager.IsInRoleAsync(user, role))
                    {
                        filteredUsers.Add(userDto);
                    }
                }
                users = filteredUsers;
                totalCount = users.Count; // Update total count for filtered results
            }

            return (totalCount, users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            throw;
        }
    }

    public async Task<(IdentityResult Result, string UserId)> CreateUserAsync(CreateUserDto createUserDto)
    {
        var user = _mapper.Map<ApplicationUser>(createUserDto);

        // These will be handled by the interceptor
        user.CreatedBy = _currentUserService.FullName;
        user.ModifiedBy = _currentUserService.FullName;
        user.CreatedAt = _dateTime.Now;
        user.IsActive = true;
        user.EmailConfirmed = true; // Since we're not implementing email confirmation

        await using var _context = await _contextFactory.CreateDbContextAsync();

        // Use transaction to ensure both user creation and role assignment succeed
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.CreateAsync(user, createUserDto.Password);
                if (result.Succeeded && !string.IsNullOrEmpty(createUserDto.Role))
                {
                    // Assign role to the user
                    var roleResult = await _userManager.AddToRoleAsync(user, createUserDto.Role);
                    if (!roleResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return (roleResult, string.Empty);
                    }

                    // Get the role claims to assign them as individual claims to the user
                    var role = await _roleManager.FindByNameAsync(createUserDto.Role);
                    if (role != null)
                    {
                        var roleClaims = await _roleManager.GetClaimsAsync(role);
                        if (roleClaims.Any())
                        {
                            var claimsResult = await _userManager.AddClaimsAsync(user, roleClaims);
                            if (!claimsResult.Succeeded)
                            {
                                await transaction.RollbackAsync();
                                return (claimsResult, string.Empty);
                            }
                        }
                    }
                }

                await transaction.CommitAsync();
                return (result, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {UserName}", createUserDto.UserName);
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<IdentityResult> UpdateUserAsync(string userId, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(
                new IdentityError { Description = _localizer["User not found"] });
        }

        // Update user properties
        user.UserName = updateUserDto.UserName;
        user.FullName = updateUserDto.FullName;
        user.Email = updateUserDto.Email;
        user.IsActive = updateUserDto.IsActive;

        // The interceptor will handle these
        user.ModifiedBy = _currentUserService.UserId;
        user.ModifiedAt = _dateTime.Now;

        await using var _context = await _contextFactory.CreateDbContextAsync();

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return result;
                }

                if (!string.IsNullOrEmpty(updateUserDto.Role))
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return removeResult;
                    }

                    var addResult = await _userManager.AddToRoleAsync(user, updateUserDto.Role);
                    if (!addResult.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return addResult;
                    }
                }

                await transaction.CommitAsync();
                return IdentityResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", userId);
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(
                new IdentityError { Description = _localizer["User not found"] });
        }
        user.DeletedBy = _currentUserService.FullName ?? "Unknown";
        // The interceptor will handle the soft delete
        return await _userManager.DeleteAsync(user);
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Array.Empty<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }

    public async Task<IdentityResult> AddUserToRolesAsync(string userId, IEnumerable<string> roles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(
                new IdentityError { Description = _localizer["User not found"] });
        }

        return await _userManager.AddToRolesAsync(user, roles);
    }

    public async Task<IdentityResult> RemoveUserFromRolesAsync(string userId, IEnumerable<string> roles)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(
                new IdentityError { Description = _localizer["User not found"] });
        }

        return await _userManager.RemoveFromRolesAsync(user, roles);
    }

    public async Task<IdentityResult> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return IdentityResult.Failed(
                new IdentityError { Description = _localizer["User not found"] });
        }

        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<bool> ValidateUserCredentialsAsync(string userName, string password)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null || !user.IsActive)
        {
            return false;
        }

        var result = await _userManager.CheckPasswordAsync(user, password);
        if (result)
        {
            // Update last login time
            user.LastLogin = _dateTime.Now;
            await _userManager.UpdateAsync(user);
        }

        return result;
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Array.Empty<string>();

        // Obtener claims directos del usuario
        var claims = await _userManager.GetClaimsAsync(user);
        var directPermissions = claims.Where(c => c.Type == c.Value).Select(c => c.Value).ToList();

        // Si no hay permisos directos, obtener los del rol
        if (!directPermissions.Any())
        {
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    directPermissions.AddRange(roleClaims.Where(c => c.Type == c.Value).Select(c => c.Value));
                }
            }
        }

        return directPermissions.Distinct();
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
            return Array.Empty<string>();

        var claims = await _roleManager.GetClaimsAsync(role);
        return claims.Where(c => c.Type == c.Value).Select(c => c.Value);
    }

    public async Task<Dictionary<string, IEnumerable<string>>> GetAllRolesWithPermissionsAsync()
    {
        var result = new Dictionary<string, IEnumerable<string>>();
        var roles = _roleManager.Roles.ToList();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            var permissions = claims.Where(c => c.Type == c.Value).Select(c => c.Value);
            if (role.Name != null)
            {
                result.Add(role.Name, permissions);
            }
        }

        return result;
    }

    public async Task UpdateUserPermissionsAsync(string userId, IEnumerable<string> permissions)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            throw new ArgumentException(_localizer["User not found"]);

        // Obtener roles y permisos actuales
        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentClaims = await _userManager.GetClaimsAsync(user);

        // Eliminar todos los claims actuales de permisos
        var permissionClaims = currentClaims.Where(c => c.Type == c.Value).ToList();
        if (permissionClaims.Any())
        {
            await _userManager.RemoveClaimsAsync(user, permissionClaims);
        }

        // Agregar los nuevos permisos como claims
        var newClaims = permissions.Select(p => new Claim(p, p));
        await _userManager.AddClaimsAsync(user, newClaims);

        // Verificar si los permisos coinciden exactamente con algún rol
        var allRolesWithPermissions = await GetAllRolesWithPermissionsAsync();
        var matchingRole = allRolesWithPermissions
            .FirstOrDefault(rp => new HashSet<string>(rp.Value).SetEquals(new HashSet<string>(permissions)));

        if (!string.IsNullOrEmpty(matchingRole.Key))
        {
            // Permissions match a role — assign it if different from current
            if (currentRoles.Count != 1 || currentRoles.First() != matchingRole.Key)
            {
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                await _userManager.AddToRoleAsync(user, matchingRole.Key);
            }
        }
        else
        {
            // Custom permissions — remove all roles so role claims don't bleed into the session
            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        // Invalidate active sessions so the user re-authenticates with the new claims
        await _userManager.UpdateSecurityStampAsync(user);
    }

    public async Task<(bool Succeeded, string? Error)> ForgotPasswordAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                // Don't reveal that the user does not exist or is not active
                return (true, null);
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Send email with token
            var callbackUrl = $"{_applicationOptions.BaseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

            // Obtener la cultura actual
            string currentCulture = CultureInfo.CurrentUICulture.Name.Split('-')[0];

            // Get email template
            var templateData = new Dictionary<string, object>
            {
                ["user"] = new { full_name = user.FullName },
                ["reset_link"] = callbackUrl,
                ["expiry_hours"] = 24, // Token expiry time
                ["culture"] = currentCulture // Pasar la cultura actual
            };

            var emailBody = await _emailTemplateService.GetTemplateAsync("password-reset", templateData);

            // Determinar el asunto según la cultura
            string subject = currentCulture.StartsWith("es")
                ? "Solicitud de Restablecimiento de Contraseña"
                : "Password Reset Request";

            // Create email message
            var message = EmailExtensions.CreateHtmlMessage(
                user.Email ?? throw new ArgumentNullException(nameof(user.Email)),
                subject,
                emailBody);

            // Send email
            var emailResult = await _emailService.SendAsync(message);

            return (emailResult.Success, emailResult.Success ? null : emailResult.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotPasswordAsync for email {Email}", email);
            return (false, _localizer["An error occurred while processing your request."]);
        }
    }

    public async Task<(bool Succeeded, string? Error)> ResetPasswordAsync(ResetPasswordDto resetDto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(resetDto.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return (false, _localizer["Invalid request"]);
            }

            var result = await _userManager.ResetPasswordAsync(user, resetDto.Token, resetDto.NewPassword);
            if (result.Succeeded)
            {
                return (true, null);
            }

            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPasswordAsync for email {Email}", resetDto.Email);
            return (false, _localizer["An error occurred while resetting your password."]);
        }
    }

    public async Task<(bool Succeeded, string? Error)> AdminResetPasswordAsync(string userId, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return (false, _localizer["User not found"]);
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Reset password with token
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                // Update modified info
                user.ModifiedBy = _currentUserService.FullName;
                user.ModifiedAt = _dateTime.Now;
                await _userManager.UpdateAsync(user);

                return (true, null);
            }

            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AdminResetPasswordAsync for user {UserId}", userId);
            return (false, _localizer["An error occurred while resetting the password."]);
        }
    }
}