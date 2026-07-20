using Godot;

/// <summary>
/// Helper de logging centralizado.
/// </summary>
/// <remarks>
/// Sustituye a las llamadas directas a GD.Print/GD.PrintErr repartidas por el
/// proyecto (handlers del protocolo, pasos de movimiento/equipamiento, etc.)
/// para poder silenciar las trazas de depuración en partidas largas sin perder
/// errores reales.
///
/// Niveles:
///   - Debug: trazas habituales de depuración (un handler/paso). Sólo se
///            imprimen si DebugEnabled es true.
///   - Info:  eventos relevantes para el usuario final (p. ej. fin de
///            partida, equipo ganador). Se imprimen siempre.
///   - Warn:  situaciones anómalas pero no fatales. Se usa GD.PushWarning
///            para que aparezcan resaltadas en el panel de depuración del
///            editor de Godot (además de la consola).
///   - Error: errores. Se imprimen siempre con GD.PrintErr.
/// </remarks>
public static class Log
{
    /// <summary>Activa/desactiva Log.Debug. Por defecto: false.</summary>
    public static bool DebugEnabled = false;

    public static void Debug(params object[] what)
    {
        if (DebugEnabled)
            GD.Print(what);
    }

    public static void Info(params object[] what)
    {
        GD.Print(what);
    }

    public static void Warn(params object[] what)
    {
        GD.PushWarning(what);
    }

    public static void Error(params object[] what)
    {
        GD.PrintErr(what);
    }
}
