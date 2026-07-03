# CLAUDE.md — Configuración para Claude Code

## Contexto del Proyecto

Este es el **cliente gráfico (monitor)** del proyecto Zappy (42 Network). Es una aplicación Godot 4 en C# que se conecta vía TCP a un servidor Zappy y visualiza el estado del juego en 3D.

## Instrucciones Obligatorias

**Al iniciar cualquier conversación sobre este proyecto, leer primero:**
- [`ARCHITECTURE.md`](ARCHITECTURE.md) — arquitectura completa, clases, flujo de datos y estado de cumplimiento del subject.

**Actualizar `ARCHITECTURE.md` cuando se realicen cambios que afecten:**
- Nuevas clases o scripts C# añadidos
- Cambios en la jerarquía de escenas (`.tscn`)
- Nuevos mensajes del protocolo manejados en `Connection.cs`
- Cambios en el flujo de datos principal
- Nuevas features de UI
- Resolución de items en la sección "Pendientes"

Al actualizar, modificar sólo las secciones relevantes; no reescribir el archivo completo salvo que sea necesario.

**No ejecutar comandos `git`** (status, diff, add, commit, push, etc.) salvo que el usuario lo pida explícitamente para esa tarea concreta.

## Stack Técnico

- **Motor:** Godot 4.x
- **Lenguaje:** C# (.NET 6)
- **Paradigma:** Nodos Godot + señales + managers por tipo de entidad
- **Red:** TCP puro, protocolo texto Zappy (ver tabla en ARCHITECTURE.md)

## Convenciones del Proyecto

- Los managers (`PlayerManager`, `EggManager`) usan `Dictionary<int, T>` con ID numérico como clave.
- Las entidades del mundo (`Player`, `Egg`, `Resource`) tienen métodos `Create()` estáticos como factory.
- La selección de objetos pasa por `ISelectable` (highlight) + `IInventory` (mostrar panel).
- Los prefabs se referencian con `GD.Load<PackedScene>()` o `[Export]` en el inspector.
- No usar `GDScript` — todo en C#.

## Convenciones de Código C#

- No usar `var`: se debe declarar siempre el tipo concreto de la variable.
- No debe haber comentarios dentro del cuerpo de los métodos.
- Los comentarios se escriben con triple barra (`///`, XML doc) para que los lea el intellisense de VS, nunca con `//` ni `/* */`.
- Los comentarios no deben mencionar otros proyectos ni funcionalidades pendientes de desarrollar a futuro.
- Cuando se use un atributo (p. ej. `[Export]`), debe ir en la línea anterior a la propiedad/campo, nunca en la misma línea.
- Los métodos deben tener como máximo 25 líneas y 4 parámetros.

## Subject de Referencia

El PDF del subject está en `C:\Users\desarrollo\Downloads\en.subject.pdf`.  
Requisitos clave del monitor (Capítulo III.12):
- Visualización en tiempo real del mundo
- Click en casilla → información específica
- Visualización de sonidos/broadcasts
- Seguimiento de progreso de equipos y ganador
- Mínimo 2D (3D es bonus — ya implementado)

## Skills Disponibles

Antes de modificar áreas específicas del proyecto, invocar la skill correspondiente para obtener el contexto completo:

| Skill | Archivo | Cuándo usarla |
|---|---|---|
| `/terrain` | `.claude/commands/terrain.md` | Cualquier cambio en terreno, coordenadas, recursos sobre el mapa, shader de grid, altura de entidades |
| `/equipment` | `.claude/commands/equipment.md` | Cualquier cambio en el equipamiento por nivel del Shaman: piezas/offsets/gemas, brillos (`GlowEffect`) y orbes orbitales sobre la cabeza (`OrbitingPivot`/`GlowOrb`/`OrbSpec`) |
| `/meshy-assets` | `.claude/commands/meshy-assets.md` | Generar o actualizar assets 3D (GLB) con Meshy AI: rutas, nombres de archivo y estado de recursos/equipamiento de Zappy Monitor |
| `/work-on-trello` | `.claude/commands/work-on-trello.md` | Para ejecutar tarjetas del backlog de Trello de forma autónoma: selección sin conflictos con En curso/Test, rama nueva por tarjeta desde `Player-Models&Animations`, commit local y movimiento de tarjetas |
| `/trello-board` | `.claude/commands/trello-board.md` | Antes de crear/editar/mover tarjetas en Trello para este proyecto: IDs del tablero "Zappy Monitor", listas, etiquetas, formato de descripción y flujo de movimiento de tarjetas |
| `/screenshot` | `.claude/commands/screenshot.md` | Para ver el monitor renderizado (verificar equipamiento, orbes, glows, terreno, UI): cómo lanzar el proyecto y leer las capturas que genera `ScreenshotService` (`.captures/latest.png`) |
| `/animals` | `.claude/commands/animals.md` | Cualquier cambio en el sistema de fauna decorativa: colocación (`AnimalSystem`), movimiento por capas (dominio `IAnimalDomain`/`AquaticDomain`, locomoción `AnimalLocomotion`), Utility AI (`UtilityBrain<Animal>`/`ScoringUtils` en la carpeta portable `utility-ai/` + comportamientos `IUtilityBehavior<Animal>`: `WanderBehavior`, `FleeBehavior`), animación procedural de huesos (`Fish`), añadir comportamientos/especies/medios (terrestre/aéreo), y portar/eliminar el sistema |

### Creación de nuevas skills

Si para completar una tarea necesitas reunir mucho contexto específico de un área del
proyecto (rutas, IDs, fórmulas, convenciones, comandos exactos) que probablemente vuelva
a hacer falta en el futuro, **pregunta al usuario si conviene convertir ese contexto en
una nueva skill** (`.claude/commands/<nombre>.md` + entrada en esta tabla), siguiendo el
mismo patrón que `/terrain`, `/equipment`, etc. No la crees sin preguntar.

## Notas de Desarrollo

- `MockServer.cs` permite probar sin servidor real; útil para desarrollo de UI.
- La IP/puerto están hardcodeados en `Connection._Ready()` (`127.0.0.1:12345`).
- El typo `UnHightlight` (falta una 'h') existe intencionalmente por compatibilidad; corregir sólo si se refactoriza la interfaz completa.
- Assets 3D generados con Meshy AI. Ver `/meshy-assets` skill para rutas, convenciones y workflow completo.
