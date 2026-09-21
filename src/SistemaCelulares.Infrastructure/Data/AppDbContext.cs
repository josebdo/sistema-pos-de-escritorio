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
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<DetalleCompra> DetalleCompras => Set<DetalleCompra>();
    public DbSet<CategoriaFinanciera> CategoriasFinancieras => Set<CategoriaFinanciera>();
    public DbSet<MovimientoFinanciero> MovimientosFinancieros => Set<MovimientoFinanciero>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<DetallePago> DetallePagos => Set<DetallePago>();

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

        // Configuración de Categoria
        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("Categorias");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Nombre).IsRequired().HasMaxLength(80);
            entity.HasIndex(c => c.Nombre).IsUnique();
            entity.Property(c => c.PrefijoSku).IsRequired().HasMaxLength(10);
            entity.Property(c => c.Descripcion).HasMaxLength(255);
        });

        // Configuración de Producto
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.ToTable("Productos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.CodigoBarras).HasMaxLength(50);
            entity.HasIndex(p => p.CodigoBarras);
            entity.Property(p => p.PrecioCosto).HasPrecision(18, 2);
            entity.Property(p => p.PrecioVenta).HasPrecision(18, 2);
            entity.Property(p => p.Descripcion).HasMaxLength(500);

            entity.HasOne(p => p.Categoria)
                  .WithMany(c => c.Productos)
                  .HasForeignKey(p => p.CategoriaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Proveedor
        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.ToTable("Proveedores");
            entity.HasKey(pr => pr.Id);
            entity.Property(pr => pr.Nombre).IsRequired().HasMaxLength(150);
            entity.HasIndex(pr => pr.Nombre);
            entity.Property(pr => pr.Rnc).HasMaxLength(20);
            entity.Property(pr => pr.Telefono).HasMaxLength(30);
            entity.Property(pr => pr.Email).HasMaxLength(100);
            entity.Property(pr => pr.Direccion).HasMaxLength(250);
            entity.Property(pr => pr.Contacto).HasMaxLength(100);
        });

        // Configuración de Compra
        modelBuilder.Entity<Compra>(entity =>
        {
            entity.ToTable("Compras");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.NumeroFactura).HasMaxLength(50);
            entity.Property(c => c.Total).HasPrecision(18, 2);
            entity.Property(c => c.Observaciones).HasMaxLength(300);

            entity.HasOne(c => c.Proveedor)
                  .WithMany()
                  .HasForeignKey(c => c.ProveedorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Usuario)
                  .WithMany()
                  .HasForeignKey(c => c.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => c.FechaCompra);
        });

        // Configuración de DetalleCompra
        modelBuilder.Entity<DetalleCompra>(entity =>
        {
            entity.ToTable("DetalleCompras");
            entity.HasKey(dc => dc.Id);
            entity.Property(dc => dc.CostoUnitario).HasPrecision(18, 2);
            entity.Property(dc => dc.Subtotal).HasPrecision(18, 2);

            entity.HasOne(dc => dc.Compra)
                  .WithMany(c => c.Detalles)
                  .HasForeignKey(dc => dc.CompraId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dc => dc.Producto)
                  .WithMany()
                  .HasForeignKey(dc => dc.ProductoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de CategoriaFinanciera
        modelBuilder.Entity<CategoriaFinanciera>(entity =>
        {
            entity.ToTable("CategoriasFinancieras");
            entity.HasKey(cf => cf.Id);
            entity.Property(cf => cf.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(cf => cf.Descripcion).HasMaxLength(250);
            entity.HasIndex(cf => new { cf.Nombre, cf.Tipo }).IsUnique();
        });

        // Configuración de MovimientoFinanciero
        modelBuilder.Entity<MovimientoFinanciero>(entity =>
        {
            entity.ToTable("MovimientosFinancieros");
            entity.HasKey(mf => mf.Id);
            entity.Property(mf => mf.Monto).HasPrecision(18, 2);
            entity.Property(mf => mf.Descripcion).IsRequired().HasMaxLength(200);
            entity.Property(mf => mf.NumeroComprobante).HasMaxLength(50);

            entity.HasOne(mf => mf.CategoriaFinanciera)
                  .WithMany(cf => cf.Movimientos)
                  .HasForeignKey(mf => mf.CategoriaFinancieraId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(mf => mf.Usuario)
                  .WithMany()
                  .HasForeignKey(mf => mf.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(mf => mf.Fecha);
        });

        // Configuración de Pago
        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("Pagos");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.MontoTotal).HasPrecision(18, 2);
            entity.Property(p => p.MontoPagado).HasPrecision(18, 2);
            entity.Property(p => p.MontoVuelto).HasPrecision(18, 2);
            entity.Property(p => p.Notas).HasMaxLength(300);

            entity.HasOne(p => p.Usuario)
                  .WithMany()
                  .HasForeignKey(p => p.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Turno)
                  .WithMany()
                  .HasForeignKey(p => p.TurnoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.FechaPago);
            entity.HasIndex(p => p.Estado);
        });

        // Configuración de DetallePago
        modelBuilder.Entity<DetallePago>(entity =>
        {
            entity.ToTable("DetallesPago");
            entity.HasKey(dp => dp.Id);
            entity.Property(dp => dp.Monto).HasPrecision(18, 2);
            entity.Property(dp => dp.MontoEntregado).HasPrecision(18, 2);
            entity.Property(dp => dp.MontoVuelto).HasPrecision(18, 2);
            entity.Property(dp => dp.BancoDestino).HasMaxLength(100);
            entity.Property(dp => dp.NumeroReferencia).HasMaxLength(100);
            entity.Property(dp => dp.NumeroAutorizacionPos).HasMaxLength(100);

            entity.HasOne(dp => dp.Pago)
                  .WithMany(p => p.Detalles)
                  .HasForeignKey(dp => dp.PagoId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
