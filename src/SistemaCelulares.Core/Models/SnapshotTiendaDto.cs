using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Models;

public class SnapshotUsuarioDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public bool DebeCambiarPassword { get; set; }
    public int RolId { get; set; }
    public string NombreRol { get; set; } = string.Empty;
}

public class SnapshotRolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsFijo { get; set; }
    public List<string> Permisos { get; set; } = new();
}

public class SnapshotCategoriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string PrefijoSku { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class SnapshotProductoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public int CantidadMinima { get; set; }
    public bool Activo { get; set; }
    public int CategoriaId { get; set; }
}

public class SnapshotProveedorDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Rnc { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
    public bool Activo { get; set; }
}

public class SnapshotCategoriaFinancieraDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public TipoMovimientoFinanciero Tipo { get; set; }
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
}

public class SnapshotTiendaDto
{
    public string VersionEsquema { get; set; } = "1.0";
    public DateTime FechaExportacion { get; set; } = DateTime.UtcNow;
    public string OrigenCaja { get; set; } = "Caja Principal";
    public string HashSha256 { get; set; } = string.Empty;

    public List<SnapshotRolDto> Roles { get; set; } = new();
    public List<SnapshotUsuarioDto> Usuarios { get; set; } = new();
    public List<SnapshotCategoriaDto> Categorias { get; set; } = new();
    public List<SnapshotProductoDto> Productos { get; set; } = new();
    public List<SnapshotProveedorDto> Proveedores { get; set; } = new();
    public List<SnapshotCategoriaFinancieraDto> CategoriasFinancieras { get; set; } = new();
}
