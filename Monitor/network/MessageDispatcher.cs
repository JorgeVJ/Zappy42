using System;
using System.Collections.Generic;

/// <summary>
/// Router del protocolo Zappy: mapea el comando (primer token de una línea del
/// servidor) a un handler Action&lt;string[]&gt;. Los handlers se registran vía
/// Register() desde Connection (directamente o a través de las clases de
/// dominio en network/handlers/).
/// </summary>
public class MessageDispatcher
{
    private readonly Dictionary<string, Action<string[]>> _handlers = new();

    /// <summary>
    /// Registra (o sobrescribe) el handler para un comando del protocolo.
    /// </summary>
    public void Register(string command, Action<string[]> handler)
    {
        _handlers[command] = handler;
    }

    /// <summary>
    /// Parsea la línea y despacha al handler registrado para parts[0]. Si no
    /// hay handler registrado, loguea "Mensaje desconocido".
    /// </summary>
    public void Dispatch(string line)
    {
        string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (_handlers.TryGetValue(parts[0], out Action<string[]> handler))
        {
            handler(parts);
        }
        else
        {
            Log.Debug("Mensaje desconocido: " + line);
        }
    }
}
