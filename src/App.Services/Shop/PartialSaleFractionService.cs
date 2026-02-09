using AutoMapper;
using App.Core.Common;
using App.Core.DTOs.Shop;
using App.Core.Interfaces.Shop;
using App.Models.Data.Contexts;
using App.Models.Shop;
using App.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Shop;

public class PartialSaleFractionService : IPartialSaleFractionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<PartialSaleFractionService> _logger;
    private readonly IStringLocalizer<PartialSaleFractionService> L;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;

    public PartialSaleFractionService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<PartialSaleFractionService> logger,
        IStringLocalizer<PartialSaleFractionService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        L = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
    }

    public async Task<Result<IList<PartialSaleFractionDto>>> GetActiveFractionsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fractions = await context.PartialSaleFractions
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.FractionValue)
                .ToListAsync();

            var dtos = _mapper.Map<IList<PartialSaleFractionDto>>(fractions);
            return Result<IList<PartialSaleFractionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active fractions");
            return Result<IList<PartialSaleFractionDto>>.Failure(L["Error retrieving fractions"]);
        }
    }

    public async Task<Result<IList<PartialSaleFractionDto>>> GetAllFractionsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fractions = await context.PartialSaleFractions
                .AsNoTracking()
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.FractionValue)
                .ToListAsync();

            var dtos = _mapper.Map<IList<PartialSaleFractionDto>>(fractions);
            return Result<IList<PartialSaleFractionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all fractions");
            return Result<IList<PartialSaleFractionDto>>.Failure(L["Error retrieving fractions"]);
        }
    }

    public async Task<Result<PartialSaleFractionDto>> GetFractionByIdAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fraction = await context.PartialSaleFractions
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fraction == null)
                return Result<PartialSaleFractionDto>.Failure(L["Fraction not found"]);

            var dto = _mapper.Map<PartialSaleFractionDto>(fraction);
            return Result<PartialSaleFractionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraction {Id}", id);
            return Result<PartialSaleFractionDto>.Failure(L["Error retrieving fraction"]);
        }
    }

    public async Task<Result<PartialSaleFractionDto>> GetFractionByCodeAsync(string code)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fraction = await context.PartialSaleFractions
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Code == code && f.IsActive);

            if (fraction == null)
                return Result<PartialSaleFractionDto>.Failure(L["Fraction not found"]);

            var dto = _mapper.Map<PartialSaleFractionDto>(fraction);
            return Result<PartialSaleFractionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fraction by code {Code}", code);
            return Result<PartialSaleFractionDto>.Failure(L["Error retrieving fraction"]);
        }
    }

    public async Task<Result<PartialSaleFractionDto>> CreateFractionAsync(CreatePartialSaleFractionDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Validate unique code
            var existingCode = await context.PartialSaleFractions
                .AnyAsync(f => f.Code == dto.Code);

            if (existingCode)
                return Result<PartialSaleFractionDto>.Failure(L["Fraction code already exists"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            var fraction = new PartialSaleFraction
            {
                Code = dto.Code,
                Name = dto.Name,
                Numerator = dto.Numerator,
                Denominator = dto.Denominator,
                FractionValue = (decimal)dto.Numerator / dto.Denominator,
                DisplayOrder = dto.DisplayOrder,
                IsActive = dto.IsActive,
                CreatedBy = currentUser,
                CreatedAt = currentTime,
                ModifiedBy = currentUser,
                ModifiedAt = currentTime,
                IsDeleted = 0
            };

            context.PartialSaleFractions.Add(fraction);
            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<PartialSaleFractionDto>(fraction);
            return Result<PartialSaleFractionDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fraction");
            return Result<PartialSaleFractionDto>.Failure(L["Error creating fraction"]);
        }
    }

    public async Task<Result<PartialSaleFractionDto>> UpdateFractionAsync(int id, UpdatePartialSaleFractionDto dto)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fraction = await context.PartialSaleFractions
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fraction == null)
                return Result<PartialSaleFractionDto>.Failure(L["Fraction not found"]);

            // Validate unique code (excluding current)
            var existingCode = await context.PartialSaleFractions
                .AnyAsync(f => f.Code == dto.Code && f.Id != id);

            if (existingCode)
                return Result<PartialSaleFractionDto>.Failure(L["Fraction code already exists"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            fraction.Code = dto.Code;
            fraction.Name = dto.Name;
            fraction.Numerator = dto.Numerator;
            fraction.Denominator = dto.Denominator;
            fraction.FractionValue = (decimal)dto.Numerator / dto.Denominator;
            fraction.DisplayOrder = dto.DisplayOrder;
            fraction.IsActive = dto.IsActive;
            fraction.ModifiedBy = currentUser;
            fraction.ModifiedAt = currentTime;

            await context.SaveChangesAsync();

            var resultDto = _mapper.Map<PartialSaleFractionDto>(fraction);
            return Result<PartialSaleFractionDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fraction {Id}", id);
            return Result<PartialSaleFractionDto>.Failure(L["Error updating fraction"]);
        }
    }

    public async Task<Result> DeleteFractionAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fraction = await context.PartialSaleFractions
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fraction == null)
                return Result.Failure(L["Fraction not found"]);

            // Check if fraction is in use
            var isInUse = await context.ProductPartialSurcharges
                .AnyAsync(s => s.PartialSaleFractionId == id);

            if (isInUse)
                return Result.Failure(L["Cannot delete fraction that is in use by products"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            fraction.IsDeleted = 1;
            fraction.ModifiedBy = currentUser;
            fraction.ModifiedAt = currentTime;

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fraction {Id}", id);
            return Result.Failure(L["Error deleting fraction"]);
        }
    }

    public async Task<Result> ToggleActiveAsync(int id)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fraction = await context.PartialSaleFractions
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fraction == null)
                return Result.Failure(L["Fraction not found"]);

            var currentUser = _currentUserService.UserId ?? "System";
            var currentTime = _dateTimeService.Now;

            fraction.IsActive = !fraction.IsActive;
            fraction.ModifiedBy = currentUser;
            fraction.ModifiedAt = currentTime;

            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling fraction status {Id}", id);
            return Result.Failure(L["Error updating fraction status"]);
        }
    }

    public async Task<Result<bool>> ValidateUniqueCodeAsync(string code, int? excludeId = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.PartialSaleFractions.AsQueryable();

            if (excludeId.HasValue)
                query = query.Where(f => f.Id != excludeId.Value);

            var exists = await query.AnyAsync(f => f.Code == code);

            return Result<bool>.Success(!exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating fraction code");
            return Result<bool>.Failure(L["Error validating fraction code"]);
        }
    }
}
