using Microsoft.EntityFrameworkCore;

namespace TallerCapricornio.Models;

public class TallerDbContext : DbContext
{
    public TallerDbContext(DbContextOptions<TallerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();

    public DbSet<Imagen> Imagenes => Set<Imagen>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Presupuesto>(entity =>
        {
            entity.ToTable("Presupuestos");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Numero).IsRequired();
            entity.Property(p => p.FechaCreacion).IsRequired();

            entity.Property(p => p.Impuestos).HasColumnType("decimal(18,2)");
            entity.Property(p => p.ManoObraChapa).HasColumnType("decimal(18,2)");
            entity.Property(p => p.ManoObraPintura).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Bancada).HasColumnType("decimal(18,2)");
            entity.Property(p => p.OtrosConceptos).HasColumnType("decimal(18,2)");

            entity.Property(p => p.IncluirIVA)
                .HasDefaultValue(false)
                .IsRequired();

            entity.HasMany(p => p.Imagenes)
                .WithOne(i => i.Presupuesto)
                .HasForeignKey(i => i.PresupuestoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Imagen>(entity =>
        {
            entity.ToTable("Imagenes");
            entity.HasKey(i => i.Id);

            entity.Property(i => i.Id).ValueGeneratedOnAdd();
            entity.Property(i => i.Contenido).HasColumnType("varbinary(max)");
        });
    }
}