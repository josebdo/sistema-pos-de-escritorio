using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class DataMigrationService : IDataMigrationService
{
    private readonly AppDbContext _context;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public DataMigrationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SnapshotTiendaDto> GenerarSnapshotAsync()
    {
        var snapshot = new SnapshotTiendaDto
        {
            VersionEsquema = "1.0",
            FechaExportacion = DateTime.UtcNow,
            OrigenCaja = "Caja Principal SQLite"
        };

        // 1. Roles y Permisos
        var roles = await _context.Roles
            .Include(r => r.RolPermisos)
            .ThenInclude(rp => rp.Permiso)
            .ToListAsync();

        snapshot.Roles = roles.Select(r => new SnapshotRolDto
        {
            Id = r.Id,
            Nombre = r.Nombre,
            Descripcion = r.Descripcion,
            EsFijo = r.EsFijo,
            Permisos = r.RolPermisos.Select(rp => rp.Permiso.Codigo).ToList()
        }).ToList();

        // 2. Usuarios
        var usuarios = await _context.Usuarios
            .Include(u => u.Rol)
            .ToListAsync();

        snapshot.Usuarios = usuarios.Select(u => new SnapshotUsuarioDto
        {
            Id = u.Id,
            NombreCompleto = u.NombreCompleto,
            NombreUsuario = u.NombreUsuario,
            PasswordHash = u.PasswordHash,
            Email = u.Email,
            Telefono = u.Telefono,
            Activo = u.Activo,
            DebeCambiarPassword = u.DebeCambiarPassword,
            RolId = u.RolId,
            NombreRol = u.Rol?.Nombre ?? string.Empty
        }).ToList();

        // 3. Categorías
        var categorias = await _context.Categorias.ToListAsync();
        snapshot.Categorias = categorias.Select(c => new SnapshotCategoriaDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            PrefijoSku = c.PrefijoSku,
            Activo = c.Activo
        }).ToList();

        // 4. Productos
        var productos = await _context.Productos.ToListAsync();
        snapshot.Productos = productos.Select(p => new SnapshotProductoDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            Sku = p.Sku,
            CodigoBarras = p.CodigoBarras,
            PrecioCosto = p.PrecioCosto,
            PrecioVenta = p.PrecioVenta,
            StockActual = p.StockActual,
            CantidadMinima = p.CantidadMinima,
            Activo = p.Activo,
            CategoriaId = p.CategoriaId
        }).ToList();

        // 5. Proveedores
        var proveedores = await _context.Proveedores.ToListAsync();
        snapshot.Proveedores = proveedores.Select(pr => new SnapshotProveedorDto
        {
            Id = pr.Id,
            Nombre = pr.Nombre,
            Rnc = pr.Rnc,
            Telefono = pr.Telefono,
            Email = pr.Email,
            Direccion = pr.Direccion,
            Contacto = pr.Contacto,
            Activo = pr.Activo
        }).ToList();

        // 6. Categorías Financieras
        var catFinancieras = await _context.CategoriasFinancieras.ToListAsync();
        snapshot.CategoriasFinancieras = catFinancieras.Select(cf => new SnapshotCategoriaFinancieraDto
        {
            Id = cf.Id,
            Nombre = cf.Nombre,
            Tipo = cf.Tipo,
            Descripcion = cf.Descripcion,
            Activo = cf.Activo
        }).ToList();

        snapshot.HashSha256 = CalcularHash(snapshot);
        return snapshot;
    }

    public async Task<string> ExportarSnapshotJsonAsync()
    {
        var snapshot = await GenerarSnapshotAsync();
        return JsonSerializer.Serialize(snapshot, _jsonOptions);
    }

    public string CalcularHash(SnapshotTiendaDto snapshot)
    {
        // Concatenar conteos y firmas para generar checksum determinista
        string raw = $"{snapshot.VersionEsquema}|{snapshot.Roles.Count}|{snapshot.Usuarios.Count}|{snapshot.Categorias.Count}|{snapshot.Productos.Count}|{snapshot.Proveedores.Count}|{snapshot.CategoriasFinancieras.Count}";
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<bool> ImportarSnapshotAsync(SnapshotTiendaDto snapshot, bool sobreescribirExistente = false)
    {
        if (snapshot == null) return false;

        // 1. Validar Hash
        string hashCalculado = CalcularHash(snapshot);
        if (snapshot.HashSha256 != hashCalculado)
        {
            return false;
        }

        // 2. Importar Categorías
        var mapCategorias = new Dictionary<int, int>();
        foreach (var c in snapshot.Categorias)
        {
            var existente = await _context.Categorias.FirstOrDefaultAsync(x => x.Nombre.ToLower() == c.Nombre.ToLower());
            if (existente == null)
            {
                var nueva = new Categoria
                {
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    PrefijoSku = c.PrefijoSku,
                    Activo = c.Activo
                };
                _context.Categorias.Add(nueva);
                await _context.SaveChangesAsync();
                mapCategorias[c.Id] = nueva.Id;
            }
            else
            {
                if (sobreescribirExistente)
                {
                    existente.Descripcion = c.Descripcion;
                    existente.PrefijoSku = c.PrefijoSku;
                    existente.Activo = c.Activo;
                }
                mapCategorias[c.Id] = existente.Id;
            }
        }
        await _context.SaveChangesAsync();

        // 3. Importar Productos
        foreach (var p in snapshot.Productos)
        {
            var existente = await _context.Productos.FirstOrDefaultAsync(x => x.Sku == p.Sku);
            int catId = mapCategorias.ContainsKey(p.CategoriaId) ? mapCategorias[p.CategoriaId] : p.CategoriaId;

            if (existente == null)
            {
                var nuevo = new Producto
                {
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Sku = p.Sku,
                    CodigoBarras = p.CodigoBarras,
                    PrecioCosto = p.PrecioCosto,
                    PrecioVenta = p.PrecioVenta,
                    StockActual = p.StockActual,
                    CantidadMinima = p.CantidadMinima,
                    Activo = p.Activo,
                    CategoriaId = catId
                };
                _context.Productos.Add(nuevo);
            }
            else if (sobreescribirExistente)
            {
                existente.Nombre = p.Nombre;
                existente.Descripcion = p.Descripcion;
                existente.CodigoBarras = p.CodigoBarras;
                existente.PrecioCosto = p.PrecioCosto;
                existente.PrecioVenta = p.PrecioVenta;
                existente.StockActual = p.StockActual;
                existente.CantidadMinima = p.CantidadMinima;
                existente.Activo = p.Activo;
            }
        }
        await _context.SaveChangesAsync();

        // 4. Importar Proveedores
        foreach (var pr in snapshot.Proveedores)
        {
            var existente = await _context.Proveedores.FirstOrDefaultAsync(x => x.Nombre.ToLower() == pr.Nombre.ToLower());
            if (existente == null)
            {
                _context.Proveedores.Add(new Proveedor
                {
                    Nombre = pr.Nombre,
                    Rnc = pr.Rnc,
                    Telefono = pr.Telefono,
                    Email = pr.Email,
                    Direccion = pr.Direccion,
                    Contacto = pr.Contacto,
                    Activo = pr.Activo
                });
            }
        }
        await _context.SaveChangesAsync();

        // 5. Importar Categorías Financieras
        foreach (var cf in snapshot.CategoriasFinancieras)
        {
            var existente = await _context.CategoriasFinancieras
                .FirstOrDefaultAsync(x => x.Nombre.ToLower() == cf.Nombre.ToLower() && x.Tipo == cf.Tipo);
            if (existente == null)
            {
                _context.CategoriasFinancieras.Add(new CategoriaFinanciera
                {
                    Nombre = cf.Nombre,
                    Tipo = cf.Tipo,
                    Descripcion = cf.Descripcion,
                    Activo = cf.Activo
                });
            }
        }
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ImportarSnapshotJsonAsync(string json, bool sobreescribirExistente = false)
    {
        try
        {
            var snapshot = JsonSerializer.Deserialize<SnapshotTiendaDto>(json);
            if (snapshot == null) return false;
            return await ImportarSnapshotAsync(snapshot, sobreescribirExistente);
        }
        catch
        {
            return false;
        }
    }
}
