using System;
using System.Collections.Generic;

// Router del protocolo Zappy: mapea el comando (primer token de una línea del
// servidor) a un handler Action<string[]>. Sustituye al switch monolítico de
// HandleServerMessage; los handlers se registran vía Register() desde Connection
// (directamente o a través de las clases de dominio en network/handlers/).
public class MessageDispatcher
{
    private readonly Dictionary<string, Action<string[]>> _handlers = new();

    // Registra (o sobrescribe) el handler para un comando del protocolo.
    public void Register(string command, Action<string[]> handler)
    {
        _handlers[command] = handler;
    }

    // Parsea la línea y despacha al handler registrado para parts[0].
    // Si no hay handler registrado, loguea "Mensaje desconocido" (comportamiento previo).
    public void Dispatch(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (_handlers.TryGetValue(parts[0], out var handler))
        {
            handler(parts);
        }
        else
        {
            Log.Debug("Mensaje desconocido: " + line);
        }
    }
}
