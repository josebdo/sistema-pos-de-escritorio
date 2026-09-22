namespace SistemaCelulares.Core.Entities;

/// <summary>
/// Estados del ciclo de vida de una orden de reparación / servicio técnico.
/// </summary>
public enum EstadoReparacion
{
    /// <summary>
    /// Equipo recibido en taller y en proceso de diagnóstico/reparación.
    /// </summary>
    EnReparacion = 1,

    /// <summary>
    /// Reparación finalizada con éxito, lista para entrega y cobro.
    /// </summary>
    ListaParaEntrega = 2,

    /// <summary>
    /// Equipo entregado al cliente con pago cobrado.
    /// </summary>
    Entregada = 3,

    /// <summary>
    /// Orden cancelada o devuelta sin reparar.
    /// </summary>
    Cancelada = 4
}
