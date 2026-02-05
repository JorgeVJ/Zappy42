using Godot;
using System.Collections.Generic;

public class MockServer
{
    private float _timer = 0f;
    private float _delay = 2f; // Segundos entre cada mensaje simulado
    private int _currentIndex = 0;

    // Secuencia de mensajes simulados para probar funcionalidades
    private readonly List<string> _messages = new List<string>
    {
        // 1. Inicialización del mapa (10x10) y equipos
        "msz 6 6",
        "tna TEAM_A",
        "tna TEAM_B",

        // Mapa inicial (bct X Y q0 q1 q2 q3 q4 q5 q6)   
        "bct 2 4 1 0 0 0 0 0 0\n",

        // 2. Conexión de un nuevo jugador (TEAM_A) en (5,5)
        "pnw #1 5 5 1 1 TEAM_A",

        // 3. Movimiento del jugador (ppo)
        "ppo #1 2 3 1",
        "ppo #1 2 4 1",

        // 4. Recoger recurso (Prendre) - pgt #n recurso (0: Nourriture)
        "pgt #1 0",

        // 5. Soltar recurso (Poser) - pdr #n recurso (1: Linemate)
        "pdr #1 1",

        // 6. Poner un huevo (pfk)
        "pfk #1",
        // Confirmación del huevo puesto (enw)
        "enw #101 #1 5 3",

        // 7. Inicio de Incantación (pic) - pic X Y Level #n ...
        "pic 5 3 1 #1",

        // 8. Fin de Incantación (pie) - pie X Y Result (1=ok)
        "pie 5 3 1",

        // 9. Cambio de nivel (plv)
        "plv #1 2",
        "plv #1 3",
        "plv #1 4",
        "plv #1 5",
        "plv #1 6",
        "plv #1 7",

        // 10. Actualización de inventario (Voir/pin)
        // pin #n X Y q0 q1 q2 q3 q4 q5 q6
        "pin #1 5 3 10 5 2 1 0 0 0",
        
        // 11. Eclosión de huevo (eht)
        "eht #101",

        // 12. Conexión de jugador en el huevo (ebo)
        "ebo #101",
        "pnw #2 5 3 1 1 TEAM_A",
        
        // 13. Muerte de jugador (pdi)
        "pdi #2"
    };

    public string GetNextCommand(double delta)
    {
        _timer += (float)delta;
        if (_timer >= _delay && _currentIndex < _messages.Count)
        {
            _timer = 0f;
            return _messages[_currentIndex++];
        }
        return null;
    }
}
