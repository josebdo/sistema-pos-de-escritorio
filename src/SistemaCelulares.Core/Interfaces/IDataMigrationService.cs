using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface IDataMigrationService
{
    Task<SnapshotTiendaDto> GenerarSnapshotAsync();
    Task<string> ExportarSnapshotJsonAsync();
    Task<bool> ImportarSnapshotAsync(SnapshotTiendaDto snapshot, bool sobreescribirExistente = false);
    Task<bool> ImportarSnapshotJsonAsync(string json, bool sobreescribirExistente = false);
    string CalcularHash(SnapshotTiendaDto snapshot);
}
