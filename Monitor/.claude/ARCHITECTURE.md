# Zappy Monitor — Arquitectura del Proyecto

## Visión General

Cliente gráfico 3D para el juego **Zappy** (proyecto UNIX 42). Conecta al servidor Zappy vía TCP y visualiza en tiempo real el estado del mundo: jugadores, huevos, recursos, incantaciones y eventos de equipo. Cumple con el requisito mínimo de interfaz gráfica del subject y va más allá con visualización 3D completa.

**Motor:** Godot 4.x  
**Lenguaje:** C# (.NET 6)  
**Escena principal:** `game.tscn`  
**Proyecto C#:** `zappy.csproj`

---

## Cumplimiento del Subject

| Requisito | Estado | Notas |
|-----------|--------|-------|
| Visualización en tiempo real del mundo | ✅ | Terreno 3D con grid shader |
| Click en casilla → info específica | ✅ | Raycast + InventoryPanel |
| Visualización mínima 2D | ✅ (3D bonus) | 3D con shader de grid |
| Visualización de broadcasts | ⚠️ Parcial | `pbc` manejado, visible en MessageLogPanel |
| Seguimiento de progreso por equipo | ✅ | TeamProgressPanel: jugadores, niveles y equipo líder |
| Quién ganó (`seg`) | ✅ | Overlay pantalla completa con nombre del ganador |
| Protocolo gráfico completo | ✅ | Todos los mensajes del servidor manejados |

---

## Estructura de Directorios

```
Monitor/
├── .claude/
│   ├── CLAUDE.md           # Instrucciones para Claude Code (este archivo vive aquí)
│   ├── ARCHITECTURE.md     # Este archivo
│   └── commands/
│       └── terrain.md      # Skill: contexto completo del sistema de terreno
├── Camera.cs               # Cámara libre WASD + raycast
├── Connection.cs           # Hub de red TCP + dispatcher de protocolo
├── Egg.cs / EggManager.cs  # Entidad huevo + gestión
├── EquipmentManager.cs     # Armadura en huesos de esqueleto (BoneAttachment3D)
├── IInventory.cs           # Interfaz: objeto con inventario
├── ISelectable.cs          # Interfaz: objeto seleccionable (highlight)
├── Inventory.cs            # Modelo de datos: 7 tipos de recurso
├── CollapsiblePanel.cs     # UI base: panel con título, botón ✕ y botón de restauración
├── InventoryPanel.cs       # UI: panel de inventario seleccionado
├── MessageLogPanel.cs      # UI: log de mensajes (hereda CollapsiblePanel)
├── TeamProgressPanel.cs    # UI: progreso por equipo (hereda CollapsiblePanel)
├── MockServer.cs           # Servidor simulado para tests sin red
├── Offsets.cs              # Struct: posición/rotación/escala para equipamiento
├── Player.cs               # Entidad jugador (IK, animación, nivel, orientación)
├── PlayerManager.cs        # Gestión centralizada de jugadores
├── Resource.cs             # Entidad recurso: carga GLB de res://models/ o esfera coloreada
├── SelectableInventoryNode3D.cs  # Clase base: Node3D seleccionable con inventario
├── Terrain.cs              # Terreno procedural (Perlin noise + mesh + colisión + recursos)
├── Tile.cs                 # Datos de una casilla (coord + inventario)
├── models/                 # Assets 3D generados con Meshy AI (.glb)
│   └── linemate.glb
├── Quadruped/
│   ├── QuadrupedController.cs
│   ├── Leg.cs
│   └── LegDefinition.cs
├── game.tscn
├── player.tscn
├── terrain.tscn
├── connection.tscn
├── creature.tscn
├── egg.tscn
├── resource.tscn
└── terrain.gdshader
```

---

## Jerarquía de Escenas

```
game.tscn
└── Node3D "Game"
    ├── Camera3D "Camera"          [Camera.cs]
    ├── DirectionalLight3D
    ├── WorldEnvironment
    ├── Connection (connection.tscn) [Connection.cs]
    │   ├── PlayerManager            [PlayerManager.cs]
    │   ├── EggManager               [EggManager.cs]
    │   ├── InventoryPanel           [InventoryPanel.cs] (UI Control)
    │   ├── MessageLogPanel          [MessageLogPanel.cs] (UI Control, creado en código)
    │   └── TeamProgressPanel        [TeamProgressPanel.cs] (UI Control, creado en código)
    └── Terrain (terrain.tscn)       [Terrain.cs]
        └── MeshInstance3D           (terrain.gdshader via ShaderMaterial)

player.tscn
└── Node3D "Player"  [Player.cs]
    ├── Node3D "Creature" (creature.tscn)
    ├── MeshInstance3D "Mesh"  (cápsula, oculta, para highlight)
    ├── Node3D "Drone"  (Drone.fbx)
    └── StaticBody3D + CollisionShape3D

creature.tscn
└── Creature (Creature.gltf)
    └── Armature → Skeleton3D
        └── QuadrupedController  [QuadrupedController.cs]
```

---

## Clases Principales

### `Connection.cs` — Hub Central
El corazón del monitor. Abre un socket TCP a `127.0.0.1:12345`, envía `GRAPHIC\n` + `mct\n` al conectar, y en cada frame parsea mensajes del servidor en `HandleServerMessage()`, delegando a handlers específicos.

**Mensajes soportados:**
| Mensaje | Handler | Efecto |
|---------|---------|--------|
| `msz W H` | → `Terrain.InitializeMap()` | Crea el mundo |
| `bct X Y q0..q6` | → `Tile.Inventory` | Actualiza recursos de casilla |
| `tna NAME` | → `teams` list | Registra nombre de equipo |
| `pnw #N X Y O L TEAM` | → `PlayerManager.GetOrCreate()` | Crea jugador |
| `ppo #N X Y O` | → `Player.SetTilePos()` / `SetOrientation()` | Mueve jugador |
| `plv #N L` | → `Player.SetLevel()` | Actualiza nivel |
| `pin #N X Y q0..q6` | → `Player.Inventory` | Actualiza inventario |
| `pex #N` | — | Jugador expulsado |
| `pbc #N MSG` | — | Broadcast de jugador |
| `pic X Y L #N...` | — | Inicio de incantación |
| `pie X Y R` | — | Fin de incantación |
| `pfk #N` | — | Jugador pone huevo |
| `pdr #N ITEM` | — | Jugador suelta recurso |
| `pgt #N ITEM` | — | Jugador recoge recurso |
| `pdi #N` | → `PlayerManager.Remove()` | Jugador muere |
| `enw #E #N X Y` | → `EggManager.CreateEgg()` | Huevo puesto |
| `eht #E` | → `EggManager.Remove()` | Huevo eclosiona |
| `ebo #E` | — | Conexión de huevo |
| `edi #E` | → `EggManager.Remove()` | Huevo muere |
| `sgt T` | — | Tiempo del servidor |
| `seg TEAM` | — | Fin de partida |
| `smg MSG` | — | Mensaje del servidor |

**Posicionamiento de entidades:** todas usan `x * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f` para centrarlas en su tile.

**Selección y UI:** `HandleLeftClick()` hace raycast desde la cámara. Si impacta un `Player` o `Tile`, llama a `ShowInventory()` que actualiza el `InventoryPanel`.

---

### `Terrain.cs` — Mundo Procedural
Ver skill `/terrain` para contexto completo. Resumen:
- `TILE_SIZE = 10.0f` (const pública) — controla escala de todo el mundo
- Genera `ArrayMesh` con heightmap Perlin en `InitializeMap()`
- Sincroniza `tile_size` del shader al generar el mesh
- Instancia nodos `Resource` en tiles al recibir `bct` vía `Inventory.Changed`
- `GetTileFromPosition()` divide por `TILE_SIZE` antes de hacer `FloorToInt`

---

### `Player.cs` — Entidad Jugador
Hereda de `SelectableInventoryNode3D`. Al crearse instancia `player.tscn`, que contiene una criatura animada (`creature.tscn`) y un dron companion (`Drone.fbx`).

- **Movimiento:** `SetTilePos()` lanza un `Tween` de 2 segundos + animación "Walk2", al completar reproduce "Idle". Posición = `x * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f`.
- **Orientación:** `SetOrientation()` mapea 1=N, 2=E, 3=S, 4=W a rotación Y.
- **Equipamiento:** `_Ready()` adjunta armadura a 4 huesos de brazo vía `EquipmentManager`.

---

### `Resource.cs` — Entidad Recurso
Carga automáticamente `res://models/{tipo}.glb` si existe (Meshy AI); si no, usa `SphereMesh` coloreada. Los modelos GLB se escalan a `0.15f`. Añadir un nuevo tipo = generar el GLB con `meshy generate` y colocarlo en `res://models/`.

---

### `QuadrupedController.cs` + `Leg.cs` — Sistema IK
Implementa animación procedural de patas con `SkeletonIK3D`. Cada pata tiene un `RayCast3D` para detectar el suelo y un `Marker3D` como target IK. Cuando una pata supera `StepDistance`, se lanza una animación de arco (`Leg.Step()`) interpolando posición en dos fases (subida y bajada).

---

### `SelectableInventoryNode3D.cs` — Clase Base
Base para `Player` y casillas seleccionables. Implementa `ISelectable` (highlight con material cian oscuro sobre el mesh) e `IInventory` (lazy-init de `Inventory`).

---

### `Inventory.cs`
Modelo de datos puro. Almacena cantidades para los 7 tipos de recurso (`Nourriture`, `Linemate`, `Deraumere`, `Sibur`, `Mendiane`, `Phiras`, `Thystame`). Dispara evento `Changed` en cada modificación.

---

### `MessageLogPanel.cs`
Panel `Control` creado programáticamente en `Connection._Ready()`. Se ancla a la esquina inferior izquierda (400×300 px). Toggle con **F2**.

---

### `MockServer.cs`
Simula mensajes del protocolo Zappy con un timer de 1 segundo por mensaje. Permite desarrollo y testing sin servidor real. El tamaño del mapa lo controla el mensaje `msz`.

---

## Flujo de Datos

```
Servidor TCP
    │
    ▼
Connection._Process()  ← lee stream TCP cada frame
    │
    ▼
HandleServerMessage()  ← parsea comando y argumentos
    │
    ├─► PlayerManager.GetOrCreate() → Player.Init() / SetTilePos() / SetLevel()
    ├─► EggManager.CreateEgg() / Remove()
    ├─► Terrain.InitializeMap() → genera mesh + shader sync
    ├─► Tile.Inventory.Set() → Changed → Terrain.UpdateTileResources()
    └─► InventoryPanel.ShowForTile() ← desde HandleLeftClick()

Camera._UnhandledInput()
    │ click izquierdo
    ▼
Camera.OnLeftClick signal
    │
    ▼
Connection.HandleLeftClick()
    ├─► Player.Highlight() + ShowInventory()
    └─► Terrain.GetTileFromPosition() + Tile.Highlight() + ShowInventory()
```

---

## Áreas de Mejora / Pendientes

- **Broadcasts sin dirección:** `pbc` se muestra en `MessageLogPanel` pero sin indicador visual de dirección en la escena 3D.
- **Fin de partida (`seg`):** Evento recibido pero sin pantalla de resultado.
- **Incantaciones (`pic`/`pie`):** Sin efecto visual en los jugadores participantes.
- **Mundo toroidal:** El terreno se genera como plano; no hay wrap-around visual.
- **Conexión hardcodeada:** IP/puerto fijos en `Connection._Ready()` (`127.0.0.1:12345`).
- **Typo:** `UnHightlight()` debería ser `UnHighlight()` en `ISelectable.cs` y `SelectableInventoryNode3D.cs`.
- **Altura de jugadores:** Y fija en `0.3f`; no sigue la altura real del terreno.
