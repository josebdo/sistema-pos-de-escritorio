using Microsoft.EntityFrameworkCore;
using SistemaCelulares.Core.Entities;
using SistemaCelulares.Core.Interfaces;
using SistemaCelulares.Infrastructure.Data;

namespace SistemaCelulares.Infrastructure.Services;

public class ConfiguracionNegocioService : IConfiguracionNegocioService
{
    private readonly AppDbContext _context;
    private static ConfiguracionNegocio? _cache;

    public ConfiguracionNegocioService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ConfiguracionNegocio> ObtenerConfiguracionAsync()
    {
        if (_cache != null)
        {
            return _cache;
        }

        var config = await _context.ConfiguracionesNegocio.FirstOrDefaultAsync();
        if (config == null)
        {
            config = new ConfiguracionNegocio();
            _context.ConfiguracionesNegocio.Add(config);
            await _context.SaveChangesAsync();
        }

        _cache = config;
        return config;
    }

    public ConfiguracionNegocio ObtenerConfiguracionSincrona()
    {
        if (_cache != null)
        {
            return _cache;
        }

        var config = _context.ConfiguracionesNegocio.FirstOrDefault();
        if (config == null)
        {
            config = new ConfiguracionNegocio();
            _context.ConfiguracionesNegocio.Add(config);
            _context.SaveChanges();
        }

        _cache = config;
        return config;
    }

    public async Task<bool> GuardarConfiguracionAsync(ConfiguracionNegocio configuracion)
    {
        var existente = await _context.ConfiguracionesNegocio.FirstOrDefaultAsync();
        if (existente == null)
        {
            configuracion.UltimaModificacion = DateTime.UtcNow;
            _context.ConfiguracionesNegocio.Add(configuracion);
        }
        else
        {
            existente.NombreEmpresa = configuracion.NombreEmpresa;
            existente.RncCedula = configuracion.RncCedula;
            existente.Telefono = configuracion.Telefono;
            existente.WhatsApp = configuracion.WhatsApp;
            existente.Email = configuracion.Email;
            existente.Direccion = configuracion.Direccion;
            existente.Ciudad = configuracion.Ciudad;
            existente.MensajePieFactura = configuracion.MensajePieFactura;
            existente.MensajeGarantiaReparacion = configuracion.MensajeGarantiaReparacion;
            existente.MonedaSimbolo = configuracion.MonedaSimbolo;
            existente.ItbisPorcentaje = configuracion.ItbisPorcentaje;
            existente.UltimaModificacion = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        _cache = await _context.ConfiguracionesNegocio.FirstOrDefaultAsync();
        return true;
    }
}
