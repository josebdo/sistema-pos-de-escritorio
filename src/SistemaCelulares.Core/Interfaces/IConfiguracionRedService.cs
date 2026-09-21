using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface IConfiguracionRedService
{
    ConfiguracionRedDto ObtenerConfiguracion();
    Task GuardarConfiguracionAsync(ConfiguracionRedDto config);
    Task<bool> ProbarConexionAsync(ConfiguracionRedDto config);
}
