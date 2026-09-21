namespace SistemaCelulares.Core.Models;

public enum LoginStatus
{
    Exitoso,
    DebeCambiarPassword,
    CredencialesInvalidas,
    UsuarioInactivo,
    UsuarioNoExiste
}

public class LoginResult
{
    public bool EsExitoso => Status == LoginStatus.Exitoso || Status == LoginStatus.DebeCambiarPassword;
    public LoginStatus Status { get; }
    public string Mensaje { get; }
    public SesionUsuario? Sesion { get; }

    private LoginResult(LoginStatus status, string mensaje, SesionUsuario? sesion = null)
    {
        Status = status;
        Mensaje = mensaje;
        Sesion = sesion;
    }

    public static LoginResult Exito(SesionUsuario sesion) =>
        new(LoginStatus.Exitoso, "Inicio de sesión correcto.", sesion);

    public static LoginResult RequiereCambioPassword(SesionUsuario sesion) =>
        new(LoginStatus.DebeCambiarPassword, "Debe cambiar su contraseña antes de continuar.", sesion);

    public static LoginResult Fallo(LoginStatus status, string mensaje) =>
        new(status, mensaje);
}
