using Godot;
using System.Collections.Generic;

public class MockServer
{
    private float _timer = 0f;
    private float _delay = 1f; // Segundos entre cada mensaje simulado
    private int _currentIndex = 0;

    // Secuencia de mensajes simulados para probar funcionalidades
    private readonly List<string> _messages = new List<string>
    {
        // ── 1. Inicialización del mapa (25x25) y equipos ──────────────────
        "msz 25 25",
        "tna TEAM_A",
        "tna TEAM_B",

        // Recursos iniciales en algunas casillas
        "bct 2 4 1 0 0 0 0 0 0",
        "bct 5 5 0 3 1 0 0 0 0",
        "bct 10 10 2 1 0 1 0 0 0",
        "bct 15 15 1 0 2 0 1 0 0",
        "bct 6 7 0 2 0 0 0 0 0",
        "bct 12 14 3 1 1 0 0 0 0",

        // ── 2. Spawn de 4 jugadores (2 por equipo) ────────────────────────
        "pnw #1 2 2 1 1 TEAM_A",
        "pnw #2 20 20 3 1 TEAM_B",
        "pnw #3 5 5 2 1 TEAM_A",
        "pnw #4 15 15 4 1 TEAM_B",

        // ── 3. Movimiento inicial de cada jugador ─────────────────────────
        // Jugador #1 (TEAM_A) se dirige a (3,4)
        "ppo #1 2 3 1",
        "ppo #1 2 4 1",
        "ppo #1 3 4 1",

        // Jugador #2 (TEAM_B) baja hacia el centro
        "ppo #2 19 20 3",
        "ppo #2 18 20 3",
        "ppo #2 17 20 3",

        // Jugador #3 (TEAM_A) se mueve a (6,7)
        "ppo #3 5 6 2",
        "ppo #3 6 6 2",
        "ppo #3 6 7 2",

        // Jugador #4 (TEAM_B) sube hacia el centro
        "ppo #4 15 14 4",
        "ppo #4 14 14 4",
        "ppo #4 13 14 4",
        "ppo #4 12 14 4",

        // ── 4. Recogida y soltada de recursos ─────────────────────────────
        "pgt #1 0",   // #1 recoge Nourriture
        "pgt #3 1",   // #3 recoge Linemate
        "pdr #2 2",   // #2 suelta Deraumere
        "pgt #4 3",   // #4 recoge Sibur

        // ── 5. Actualización de inventarios ───────────────────────────────
        "pin #1 3 4 5 2 0 0 0 0 0",
        "pin #3 6 7 3 4 1 0 0 0 0",
        "pin #4 12 14 2 1 1 1 0 0 0",

        // ── 6. Broadcasts ─────────────────────────────────────────────────
        "pbc #1 Gathering resources for incantation!",
        "pbc #4 Heading to center, need Sibur!",

        // ── 7. Jugador #3 pone un huevo ───────────────────────────────────
        "pfk #3",
        "enw #101 #3 6 7",

        // ── 8. Incantación grupal entre #1 y #3 en (6,7) ─────────────────
        "ppo #1 5 7 2",
        "ppo #1 6 7 2",
        "pic 6 7 1 #1 #3",
        "pie 6 7 1",
        "plv #1 2",
        "plv #3 2",

        // ── 9. Eclosión del huevo → nuevo jugador #5 ─────────────────────
        "eht #101",
        "ebo #101",
        "pnw #5 6 7 1 1 TEAM_A",

        // ── 10. #2 realiza su propia incantación en (10,10) ───────────────
        "ppo #2 15 20 3",
        "ppo #2 12 18 3",
        "ppo #2 10 10 3",
        "pic 10 10 1 #2",
        "pie 10 10 1",
        "plv #2 2",

        // ── 11. Movimientos de #5 y #4 tras los eventos ───────────────────
        "ppo #5 7 7 2",
        "ppo #5 8 7 2",
        "ppo #5 9 7 2",
        "ppo #4 11 14 4",
        "ppo #4 10 14 4",

        // ── 12. Más leveling para #1 ──────────────────────────────────────
        "pin #1 6 7 8 4 2 1 0 0 0",
        "plv #1 3",
        "ppo #1 7 7 1",
        "plv #1 4",

        // ── 13. Broadcast de #2 tras subir de nivel ───────────────────────
        "pbc #2 Level 2 reached, advancing!",

        // ── 14. Jugador #4 pone un huevo para TEAM_B ─────────────────────
        "pfk #4",
        "enw #102 #4 10 14",

        // ── 15. Incantación de #2 y #4 juntos en (10,14) ─────────────────
        "ppo #2 10 14 4",
        "pic 10 14 2 #2 #4",
        "pie 10 14 1",
        "plv #2 3",
        "plv #4 3",

        // ── 16. Eclosión del huevo de TEAM_B → jugador #6 ─────────────────
        "eht #102",
        "ebo #102",
        "pnw #6 10 14 1 1 TEAM_B",

        // ── 17. Movimientos finales y broadcasts ──────────────────────────
        "ppo #6 11 14 2",
        "ppo #3 7 8 1",
        "pbc #3 Searching for Sibur!",
        "pgt #6 1",
        "pin #6 11 14 4 2 0 0 0 0 0",

        // ── 18. Muerte de jugador #5 (hambre) ─────────────────────────────
        "pdi #5",

        // ── 19. Incantación final de #1 con #3 y #6 ──────────────────────
        "ppo #3 10 10 1",
        "ppo #6 10 10 3",
        "ppo #1 10 10 2",
        "pic 10 10 4 #1 #3 #6",
        "pie 10 10 1",
        "plv #1 5",
        "plv #3 4",
        "plv #6 2",

        // ── 20. Muerte de jugador #2 (recursos agotados) ──────────────────
        "pdi #2",
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
