using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolPermisos => Set<RolPermiso>();
    public DbSet<Turno> Turnos => Set<Turno>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
            entity.Property(u => u.NombreUsuario).IsRequired().HasMaxLength(50);
            entity.HasIndex(u => u.NombreUsuario).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.Telefono).HasMaxLength(30);

            entity.HasOne(u => u.Rol)
                  .WithMany(r => r.Usuarios)
                  .HasForeignKey(u => u.RolId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Rol
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Nombre).IsRequired().HasMaxLength(60);
            entity.HasIndex(r => r.Nombre).IsUnique();
            entity.Property(r => r.Descripcion).HasMaxLength(255);
        });

        // Configuración de Permiso
        modelBuilder.Entity<Permiso>(entity =>
        {
            entity.ToTable("Permisos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Codigo).IsRequired().HasMaxLength(80);
            entity.HasIndex(p => p.Codigo).IsUnique();
            entity.Property(p => p.Modulo).IsRequired().HasMaxLength(60);
            entity.Property(p => p.Descripcion).IsRequired().HasMaxLength(200);
        });

        // Configuración de RolPermiso (N:M)
        modelBuilder.Entity<RolPermiso>(entity =>
        {
            entity.ToTable("RolPermisos");
            entity.HasKey(rp => new { rp.RolId, rp.PermisoId });

            entity.HasOne(rp => rp.Rol)
                  .WithMany(r => r.RolPermisos)
                  .HasForeignKey(rp => rp.RolId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Permiso)
                  .WithMany(p => p.RolPermisos)
                  .HasForeignKey(rp => rp.PermisoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de Turno
        modelBuilder.Entity<Turno>(entity =>
        {
            entity.ToTable("Turnos");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.MontoApertura).HasPrecision(18, 2);
            entity.Property(t => t.TotalVentasEfectivo).HasPrecision(18, 2);
            entity.Property(t => t.MontoEsperado).HasPrecision(18, 2);
            entity.Property(t => t.MontoCierre).HasPrecision(18, 2);
            entity.Property(t => t.Diferencia).HasPrecision(18, 2);
            entity.Property(t => t.ObservacionesApertura).HasMaxLength(300);
            entity.Property(t => t.ObservacionesCierre).HasMaxLength(300);

            entity.HasOne(t => t.UsuarioApertura)
                  .WithMany()
                  .HasForeignKey(t => t.UsuarioAperturaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.UsuarioCierre)
                  .WithMany()
                  .HasForeignKey(t => t.UsuarioCierreId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => new { t.UsuarioAperturaId, t.Estado });
        });
    }
}
