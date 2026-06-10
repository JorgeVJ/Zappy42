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
| `/meshy` | `C:\Users\desarrollo\tools\meshy-client\.claude\commands\meshy.md` | Generar o actualizar assets 3D con Meshy AI (modelos GLB para `res://models/`) |
| `/trello-board` | `.claude/commands/trello-board.md` | Antes de crear/editar/mover tarjetas en Trello para este proyecto: IDs del tablero "Zappy Monitor", listas, etiquetas, formato de descripción y flujo de movimiento de tarjetas |

## Notas de Desarrollo

- `MockServer.cs` permite probar sin servidor real; útil para desarrollo de UI.
- La IP/puerto están hardcodeados en `Connection._Ready()` (`127.0.0.1:12345`).
- El typo `UnHightlight` (falta una 'h') existe intencionalmente por compatibilidad; corregir sólo si se refactoriza la interfaz completa.
- Assets 3D generados con Meshy AI: herramienta en `C:\Users\desarrollo\tools\meshy-client\`. Ver `/meshy` skill para el workflow completo.
