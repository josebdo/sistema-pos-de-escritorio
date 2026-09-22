namespace SistemaCelulares.Core.Models;

public record ItemCompraDto(
    int ProductoId,
    int Cantidad,
    decimal CostoUnitario,
    IReadOnlyList<string>? Imeis = null,
    decimal? NuevoPrecioVenta = null);
