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
| Seguimiento de progreso por equipo | ⚠️ Parcial | Nivel de jugador visible, no panel de equipos |
| Quién ganó (`seg`) | ⚠️ Sin UI | Evento parseado, sin pantalla de fin de juego |
| Protocolo gráfico completo | ✅ | Todos los mensajes del servidor manejados |

---

## Estructura de Directorios

```
Monitor/
├── Camera.cs               # Cámara libre WASD + raycast
├── Connection.cs           # Hub de red TCP + dispatcher de protocolo
├── Egg.cs / EggManager.cs  # Entidad huevo + gestión
├── EquipmentManager.cs     # Armadura en huesos de esqueleto (BoneAttachment3D)
├── IInventory.cs           # Interfaz: objeto con inventario
├── ISelectable.cs          # Interfaz: objeto seleccionable (highlight)
├── Inventory.cs            # Modelo de datos: 7 tipos de recurso
├── InventoryPanel.cs       # UI: panel de inventario seleccionado
├── MessageLogPanel.cs      # UI: panel scrolleable de log de mensajes del servidor
├── MockServer.cs           # Servidor simulado para tests sin red
├── Offsets.cs              # Struct: posición/rotación/escala para equipamiento
├── Player.cs               # Entidad jugador (IK, animación, nivel, orientación)
├── PlayerManager.cs        # Gestión centralizada de jugadores
├── Resource.cs             # Entidad recurso en el mundo (esferas coloreadas)
├── SelectableInventoryNode3D.cs  # Clase base: Node3D seleccionable con inventario
├── Terrain.cs              # Terreno procedural (Perlin noise + mesh + colisión)
├── Tile.cs                 # Datos de una casilla (coord + inventario)
├── Quadruped/
│   ├── QuadrupedController.cs  # Sistema IK para patas de cuadrúpedo
│   ├── Leg.cs                  # Una pata: raycast + animación de paso
│   └── LegDefinition.cs        # Config estática de una pata (huesos, offset)
├── game.tscn               # Escena principal
├── player.tscn             # Prefab jugador
├── terrain.tscn            # Prefab terreno
├── connection.tscn         # Prefab nodo de red
├── creature.tscn           # Prefab criatura animada (Creature.gltf + IK)
├── egg.tscn                # Prefab huevo
├── resource.tscn           # Prefab recurso
└── terrain.gdshader        # Shader de líneas de grid sobre terreno
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
    │   └── MessageLogPanel          [MessageLogPanel.cs] (UI Control, creado en código)
    └── Terrain (terrain.tscn)       [Terrain.cs]
        └── MeshInstance3D           (terrain.gdshader)

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

**Selección y UI:** `HandleLeftClick()` hace raycast desde la cámara. Si impacta un `Player` o `Tile`, llama a `ShowInventory()` que actualiza el `InventoryPanel`.

---

### `Terrain.cs` — Mundo Procedural
Genera el terreno al recibir `msz`. Usa `FastNoiseLite` (Perlin) para el heightmap. Crea un `ArrayMesh` con vértices, índices y normales, y le aplica colisión trimesh. El shader `terrain.gdshader` dibuja líneas de grid usando `fract()` sobre coordenadas de mundo para visualizar los límites de casillas.

Parámetros clave: `TileSize = 3.0f`, `HeightScale = 3.0f`, `NoiseScale = 0.08f`.

---

### `Player.cs` — Entidad Jugador
Hereda de `SelectableInventoryNode3D`. Al crearse instancia `player.tscn`, que contiene una criatura animada (`creature.tscn`) y un dron companion (`Drone.fbx`). 

- **Movimiento:** `SetTilePos()` lanza un `Tween` de 2 segundos + animación "Walk2", al completar reproduce "Idle".
- **Orientación:** `SetOrientation()` mapea 1=N, 2=E, 3=S, 4=W a rotación Y.
- **Equipamiento:** `_Ready()` adjunta armadura a 4 huesos de brazo vía `EquipmentManager`.

---

### `QuadrupedController.cs` + `Leg.cs` — Sistema IK
Implementa animación procedural de patas con `SkeletonIK3D`. Cada pata tiene un `RayCast3D` para detectar el suelo y un `Marker3D` como target IK. Cuando una pata supera `StepDistance`, se lanza una animación de arco (`Leg.Step()`) interpolando posición en dos fases (subida y bajada). Las patas se gestionan en cola (`StepOrder`) para alternar pasos naturalmente.

---

### `SelectableInventoryNode3D.cs` — Clase Base
Base para `Player` y casillas seleccionables. Implementa `ISelectable` (highlight con material cian oscuro sobre el mesh) e `IInventory` (lazy-init de `Inventory`).

---

### `Inventory.cs`
Modelo de datos puro. Almacena cantidades para los 7 tipos de recurso del juego (`Nourriture`, `Linemate`, `Deraumere`, `Sibur`, `Mendiane`, `Phiras`, `Thystame`). Dispara evento `Changed` en cada modificación.

---

### `MessageLogPanel.cs`
Panel `Control` creado programáticamente en `Connection._Ready()`. Se ancla a la esquina inferior izquierda (400×300 px). Muestra todos los mensajes entrantes con color coding por tipo:

| Color | Tipos |
|-------|-------|
| Cyan | `pnw` (spawn) |
| Rojo | `pdi` (muerte) |
| Verde | `plv` (nivel) |
| Amarillo | `pbc` (broadcast) |
| Naranja | `pic` / `pie` (incantación) |
| Lila | `enw` / `eht` / `ebo` / `edi` (huevos) |
| Gris | `bct` / `pgt` / `pdr` / `pfk` / `pin` (recursos) |
| Azul | `msz` / `tna` / `sgt` / `seg` (sistema) |

Toggle con **F2**. Botón "Limpiar" para vaciar el log. Límite de 80 entradas (se limpia al superarlo).

---

### `MockServer.cs`
Simula mensajes del protocolo Zappy con un timer de 1 segundo por mensaje. Permite desarrollo y testing sin servidor real.

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
    ├─► Terrain.InitializeMap() → genera mesh
    ├─► Tile.Inventory.Set() → actualiza recursos de casilla
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

- **Sin UI de equipos:** No existe panel que muestre progreso por equipo ni quién está ganando.
- **Broadcasts sin dirección:** `pbc` se muestra en `MessageLogPanel` pero sin indicador visual de dirección en la escena 3D.
- **Fin de partida (`seg`):** Evento recibido pero sin pantalla de resultado.
- **Incantaciones (`pic`/`pie`):** Sin efecto visual en los jugadores participantes.
- **Mundo toroidal:** El terreno se genera como plano; no hay wrap-around visual.
- **Conexión hardcodeada:** IP/puerto fijos en `Connection._Ready()` (`127.0.0.1:12345`).
- **Typo:** `UnHightlight()` debería ser `UnHighlight()` en `ISelectable.cs` y `SelectableInventoryNode3D.cs`.
