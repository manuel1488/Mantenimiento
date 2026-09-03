using AutoMapper;

using App.Core.Common;
using App.Core.DTOs.Obras;
using App.Core.Interfaces;
using App.Core.Models.Email;
using App.Models.Data.Contexts;
using App.Models.Obras;
using App.Shared.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace App.Services.Obras;

public class ObraClienteAccesoService : IObraClienteAccesoService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ObraClienteAccesoService> _logger;
    private readonly IStringLocalizer<ObraClienteAccesoService> _localizer;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTimeService;
    private readonly IObraSeguimientoClienteSettingsService _settingsService;
    private readonly IEmailService _emailService;

    public ObraClienteAccesoService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        IMapper mapper,
        ILogger<ObraClienteAccesoService> logger,
        IStringLocalizer<ObraClienteAccesoService> localizer,
        ICurrentUserService currentUserService,
        IDateTime dateTimeService,
        IObraSeguimientoClienteSettingsService settingsService,
        IEmailService emailService)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
        _logger = logger;
        _localizer = localizer;
        _currentUserService = currentUserService;
        _dateTimeService = dateTimeService;
        _settingsService = settingsService;
        _emailService = emailService;
    }

    public async Task<Result<ObraClienteAccesoDto>> GetByObraIdAsync(int obraId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.ObraClienteAccesos
                .AsNoTracking()
                .Include(a => a.Obra)
                .FirstOrDefaultAsync(a => a.ObraId == obraId, cancellationToken);

            if (entity == null)
            {
                // Obras created before this feature shipped (see migration AddObraClienteAcceso)
                // never got their 1:1 access row — backfill it lazily on first access instead of
                // failing forever. ObraId has a unique index, so a request racing to create the
                // same row concurrently just falls through to re-reading it below.
                var obraExists = await context.Obras.AnyAsync(o => o.Id == obraId, cancellationToken);
                if (!obraExists)
                    return Result<ObraClienteAccesoDto>.Failure(_localizer["Client access link not found"]);

                var currentUser = await _currentUserService.GetUserIdAsync();
                var currentTime = _dateTimeService.Now;

                var strategy = context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        context.ObraClienteAccesos.Add(new ObraClienteAcceso
                        {
                            ObraId = obraId,
                            Token = ObraClienteAccesoTokenGenerator.Generate(),
                            Habilitado = true,
                            TokenGeneradoEn = currentTime,
                            CreatedBy = currentUser,
                            CreatedAt = currentTime,
                            ModifiedBy = currentUser,
                            ModifiedAt = currentTime
                        });
                        await context.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        _logger.LogWarning("Backfilled missing client access link for obra {Id} (created before this feature existed)", obraId);
                    }
                    catch (DbUpdateException)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                });

                entity = await context.ObraClienteAccesos
                    .AsNoTracking()
                    .Include(a => a.Obra)
                    .FirstOrDefaultAsync(a => a.ObraId == obraId, cancellationToken);

                if (entity == null)
                    return Result<ObraClienteAccesoDto>.Failure(_localizer["Client access link not found"]);
            }

            var settings = await _settingsService.GetSettingsAsync();
            return Result<ObraClienteAccesoDto>.Success(BuildDto(entity, settings.DiasVigenciaPostFinalizacion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client access link for obra {Id}", obraId);
            return Result<ObraClienteAccesoDto>.Failure(_localizer["Error retrieving client access link"]);
        }
    }

    public async Task<Result<ObraClienteAccesoDto>> RegenerarTokenAsync(int obraId, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var strategy = context.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var entity = await context.ObraClienteAccesos.FirstOrDefaultAsync(a => a.ObraId == obraId, cancellationToken);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result<ObraClienteAcceso>.Failure(_localizer["Client access link not found"]);
                    }

                    entity.Token = ObraClienteAccesoTokenGenerator.Generate();
                    entity.TokenGeneradoEn = _dateTimeService.Now;
                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Result<ObraClienteAcceso>.Success(entity);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (!result.IsSuccess)
                return Result<ObraClienteAccesoDto>.Failure(result.Error!);

            var settings = await _settingsService.GetSettingsAsync();
            return Result<ObraClienteAccesoDto>.Success(BuildDto(result.Value!, settings.DiasVigenciaPostFinalizacion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating client access token for obra {Id}", obraId);
            return Result<ObraClienteAccesoDto>.Failure(_localizer["Error regenerating client access link"]);
        }
    }

    public async Task<Result<ObraClienteAccesoDto>> SetHabilitadoAsync(int obraId, bool habilitado, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var strategy = context.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var entity = await context.ObraClienteAccesos.FirstOrDefaultAsync(a => a.ObraId == obraId, cancellationToken);
                    if (entity == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return Result<ObraClienteAcceso>.Failure(_localizer["Client access link not found"]);
                    }

                    entity.Habilitado = habilitado;
                    entity.ModifiedBy = await _currentUserService.GetUserIdAsync();
                    entity.ModifiedAt = _dateTimeService.Now;

                    await context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return Result<ObraClienteAcceso>.Success(entity);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (!result.IsSuccess)
                return Result<ObraClienteAccesoDto>.Failure(result.Error!);

            var settings = await _settingsService.GetSettingsAsync();
            return Result<ObraClienteAccesoDto>.Success(BuildDto(result.Value!, settings.DiasVigenciaPostFinalizacion));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating client access link for obra {Id}", obraId);
            return Result<ObraClienteAccesoDto>.Failure(_localizer["Error updating client access link"]);
        }
    }

    public async Task<Result> SendLinkByEmailAsync(int obraId, string url, string email, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var obra = await context.Obras
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == obraId, cancellationToken);

            if (obra == null)
                return Result.Failure(_localizer["Obra not found"]);

            var message = new EmailMessage
            {
                To = email,
                Subject = _localizer["Track your project at {0}", obra.Direccion],
                Body = _localizer["You can follow the progress of your project at {0} using this link: {1}", obra.Direccion, url],
                IsHtml = false
            };

            var emailResult = await _emailService.SendAsync(message, cancellationToken);
            if (!emailResult.Success)
                return Result.Failure(emailResult.Error ?? _localizer["Error sending email"]);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emailing client access link for obra {Id}", obraId);
            return Result.Failure(_localizer["Error sending email"]);
        }
    }

    public async Task<Result<int>> ResolveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Same generic failure for "not found", "disabled" and "expired" — a real token that is
        // merely disabled/expired must look identical to a made-up one, otherwise the response
        // itself becomes an oracle an attacker could use to confirm guesses.
        var invalidResult = Result<int>.Failure(_localizer["This link is invalid or has expired"]);

        if (string.IsNullOrWhiteSpace(token) || token.Length > 64)
            return invalidResult;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            var entity = await context.ObraClienteAccesos
                .AsNoTracking()
                .Include(a => a.Obra)
                .FirstOrDefaultAsync(a => a.Token == token, cancellationToken);

            if (entity == null || !entity.Habilitado)
                return invalidResult;

            if (entity.Obra.FechaFinalizacion is { } finalizada)
            {
                var settings = await _settingsService.GetSettingsAsync();
                var expiraEn = finalizada.AddDays(settings.DiasVigenciaPostFinalizacion);
                if (_dateTimeService.Now > expiraEn)
                    return invalidResult;
            }

            return Result<int>.Success(entity.ObraId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving client access token");
            return invalidResult;
        }
    }

    private ObraClienteAccesoDto BuildDto(ObraClienteAcceso entity, int diasVigenciaPostFinalizacion)
    {
        var dto = _mapper.Map<ObraClienteAccesoDto>(entity);

        dto.ExpiraEn = entity.Obra.FechaFinalizacion?.AddDays(diasVigenciaPostFinalizacion);
        dto.Vigente = entity.Habilitado && (dto.ExpiraEn is null || _dateTimeService.Now <= dto.ExpiraEn);

        return dto;
    }
}
