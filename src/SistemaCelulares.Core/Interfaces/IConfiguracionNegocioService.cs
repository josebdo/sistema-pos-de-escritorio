using SistemaCelulares.Core.Entities;

namespace SistemaCelulares.Core.Interfaces;

public interface IConfiguracionNegocioService
{
    Task<ConfiguracionNegocio> ObtenerConfiguracionAsync();
    ConfiguracionNegocio ObtenerConfiguracionSincrona();
    Task<bool> GuardarConfiguracionAsync(ConfiguracionNegocio configuracion);
}
