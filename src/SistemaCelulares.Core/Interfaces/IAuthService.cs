using SistemaCelulares.Core.Models;

namespace SistemaCelulares.Core.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string nombreUsuario, string password, CancellationToken cancellationToken = default);
    Task<bool> CambiarPasswordObligatorioAsync(int usuarioId, string nuevaPassword, CancellationToken cancellationToken = default);
    Task<bool> CambiarPasswordVoluntarioAsync(int usuarioId, string passwordActual, string nuevaPassword, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(int adminId, int targetUsuarioId, string passwordTemporal, CancellationToken cancellationToken = default);
}
