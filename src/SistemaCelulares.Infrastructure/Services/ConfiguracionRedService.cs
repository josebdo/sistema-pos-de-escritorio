using System.Net.Sockets;
using System.Text.Json;
using SistemaCelulares.Core.Constants;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Infrastructure.Services;

public class ConfiguracionRedService : IConfiguracionRedService
{
    private readonly string _configFilePath;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ConfiguracionRedService(string? basePathOrFilePath = null)
    {
        if (string.IsNullOrWhiteSpace(basePathOrFilePath))
        {
            _configFilePath = AppPaths.RutaConfigRed;
        }
        else if (basePathOrFilePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            _configFilePath = basePathOrFilePath;
        }
        else
        {
            _configFilePath = Path.Combine(basePathOrFilePath, "config_red.json");
        }
    }

    public ConfiguracionRedDto ObtenerConfiguracion()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<ConfiguracionRedDto>(json);
                if (config != null) return config;
            }
        }
        catch
        {
            // En caso de lectura corrupta, retornar configuracion por defecto
        }

        return new ConfiguracionRedDto();
    }

    public async Task GuardarConfiguracionAsync(ConfiguracionRedDto config)
    {
        config.UltimaModificacion = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(_configFilePath, json);
    }

    public async Task<bool> ProbarConexionAsync(ConfiguracionRedDto config)
    {
        if (config.ModoOperacion == ModoOperacionCaja.CajaUnicaLocal)
        {
            return true;
        }

        try
        {
            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(config.ServidorIpOHost, config.Puerto);
            var timeoutTask = Task.Delay(3000);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            if (completedTask == connectTask && tcpClient.Connected)
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
