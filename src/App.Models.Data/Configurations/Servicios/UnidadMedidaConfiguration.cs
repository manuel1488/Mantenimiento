using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using App.Models.Servicios;

namespace App.Models.Data.Configurations.Servicios;

public class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> builder)
    {
        builder.Property(e => e.Codigo)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Nombre)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => e.Codigo)
            .IsUnique();

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        const string systemUser = "System";

        builder.HasData(
            new UnidadMedida { Id = 1, Codigo = "PZA", Nombre = "Pieza", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 2, Codigo = "SRV", Nombre = "Servicio", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 3, Codigo = "KIT", Nombre = "Kit", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 4, Codigo = "M", Nombre = "Metro", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 5, Codigo = "M2", Nombre = "Metro Cuadrado", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 6, Codigo = "M3", Nombre = "Metro Cúbico", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 7, Codigo = "KM", Nombre = "Kilómetro", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 8, Codigo = "KG", Nombre = "Kilogramo", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 9, Codigo = "TON", Nombre = "Tonelada", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 10, Codigo = "L", Nombre = "Litro", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 11, Codigo = "HR", Nombre = "Hora", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 12, Codigo = "DIA", Nombre = "Día", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 13, Codigo = "MES", Nombre = "Mes", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 14, Codigo = "JGO", Nombre = "Juego", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 },
            new UnidadMedida { Id = 15, Codigo = "VIS", Nombre = "Visita", CreatedBy = systemUser, CreatedAt = seedDate, IsDeleted = 0 }
        );
    }
}
