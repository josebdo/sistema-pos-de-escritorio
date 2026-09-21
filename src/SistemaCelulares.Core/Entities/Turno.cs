namespace SistemaCelulares.Core.Entities;

public enum TurnoEstado
{
    Abierto = 1,
    Cerrado = 2
}

public class Turno
{
    public int Id { get; set; }

    public int UsuarioAperturaId { get; set; }
    public Usuario UsuarioApertura { get; set; } = null!;

    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public decimal MontoApertura { get; set; }

    public int? UsuarioCierreId { get; set; }
    public Usuario? UsuarioCierre { get; set; }

    public DateTime? FechaCierre { get; set; }
    
    /// <summary>
    /// Total de ventas cobradas en efectivo durante el turno.
    /// </summary>
    public decimal TotalVentasEfectivo { get; set; } = 0m;

    /// <summary>
    /// Total calculado que debería haber en caja: MontoApertura + TotalVentasEfectivo.
    /// </summary>
    public decimal MontoEsperado { get; set; }

    /// <summary>
    /// Dinero físico contado y declarado por el cajero al momento del cierre.
    /// </summary>
    public decimal? MontoCierre { get; set; }

    /// <summary>
    /// Diferencia = MontoCierre - MontoEsperado.
    /// Negativo indica faltante, positivo sobrante, cero cuadre exacto.
    /// </summary>
    public decimal? Diferencia { get; set; }

    public TurnoEstado Estado { get; set; } = TurnoEstado.Abierto;
    public string? ObservacionesApertura { get; set; }
    public string? ObservacionesCierre { get; set; }
    public int CajaId { get; set; } = 1; // Para futura expansión multi-caja
}
