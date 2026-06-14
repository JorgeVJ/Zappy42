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
| Click en casilla → info específica | ✅ | Raycast + InventoryPanel; el título muestra coordenadas de casilla o identidad del jugador (`IInventory.DisplayTitle`) |
| Visualización mínima 2D | ✅ (3D bonus) | 3D con shader de grid |
| Visualización de broadcasts | ✅ | `pbc`: texto flotante (`Label3D`) + onda expansiva 3D (`SoundWave`) sobre el emisor, además de MessageLogPanel |
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
│       ├── terrain.md      # Skill: contexto completo del sistema de terreno
│       ├── meshy-assets.md # Skill: assets 3D (Meshy AI) — rutas, nombres y estado
│       └── trello-board.md # Skill: tablero Trello del proyecto (IDs, listas, etiquetas)
├── Camera.cs               # Cámara libre WASD + raycast
├── CameraFollowBehavior.cs # Lock de cámara sobre un jugador (TeamProgressPanel): orbita (WASD) y zoom (rueda) alrededor del objetivo sin romper el lock; clic derecho lo desactiva
├── Connection.cs           # Hub central: cablea transporte/dispatcher/managers/UI (lógica de protocolo repartida en Connection.Players.cs / Connection.Eggs.cs / Connection.System.cs)
├── ServerTransport.cs      # Transporte TCP real o MockServer; emite LineReceived(line), expone SendMessage()/SetMockSpeed()
├── MessageDispatcher.cs    # Router string→Action<string[]> del protocolo (sustituye al switch de HandleServerMessage)
├── EventLog.cs             # Historial de líneas crudas del servidor agrupadas en TimeBand (franjas de tiempo) por proximidad de llegada
├── TimelineController.cs   # Backend de la barra de tiempo: cursor de franja, IsLive, JumpTo()/GoLive() (reset + replay instantáneo)
├── Connection.Timeline.cs  # (clase parcial) ReplayInstant flag + ResetWorldState(): vacía PlayerManager/EggManager/Terrain/TeamProgressPanel para el replay
├── SelectionController.cs  # Selección por click (raycast) + InventoryPanel: HandleLeftClick/ShowInventory/PlayerClicked
├── EntityManager.cs        # Base genérica EntityManager<T> (Node3D): Dictionary<int,T> + contenedor + TryGet()/Remove(); heredada por PlayerManager y EggManager
├── Egg.cs / EggManager.cs  # Entidad huevo + gestión (EggManager : EntityManager<Egg>)
├── EquipmentManager.cs     # Gestor genérico de equipamiento: BoneAttachment3D, caché de escenas, ApplyLoadout(), hijos de equipo (gemas), AttachOrbitingGroup() (orbes en órbita)
├── EquipmentSlot.cs        # Struct genérico: (BoneName, ScenePath, Offsets?, Children?) — portable entre proyectos
├── EquipmentChild.cs       # Struct genérico: (ScenePath, Offsets?, GlowEffect?) — modelo hijo anidado dentro de una pieza de equipo (ej. gema en bastón)
├── OrbitingPivot.cs        # Node3D genérico: rota sobre su eje Y a velocidad constante (orbes en órbita sobre la cabeza)
├── OrbSpec.cs              # Struct genérico: (Offsets, Color, GlowEffect) — define una orbe procedural alrededor de un OrbitingPivot
├── GlowOrb.cs              # MeshInstance3D genérico: esfera procedural translúcida + rim + GlowEffect (orbe brillante, sin GLB)
├── GlowEffect.cs           # Struct genérico: (Color, EnergyMultiplier) — aplica emisión a los materiales de un Node3D
├── ShamanEquipmentConfig.cs # Config específica del proyecto: loadout por nivel (1-7) para el Shaman, incl. grupo de orbes brillantes en órbita
├── ShamanAnimationController.cs # Controlador de animaciones del Shaman: PlayWalk/Idle/Run/Spell/etc., loop automático
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
├── PlayerManager.cs        # Gestión centralizada de jugadores (PlayerManager : EntityManager<Player>)
├── Resource.cs             # Entidad recurso: carga GLB o esfera coloreada; animación de aparición (caída + pop) al instanciarse
├── ScreenshotService.cs    # Herramienta dev: vuelca el framebuffer a .captures/*.png (auto periódico + F12)
├── SelectableInventoryNode3D.cs  # Clase base: Node3D seleccionable con inventario
├── Terrain.cs              # Terreno procedural (Perlin noise + mesh + colisión + recursos)
├── Tile.cs                 # Datos de una casilla (coord + inventario)
├── models/
│   ├── Shaman/
│   │   └── Shaman.glb      # Modelo principal del jugador (esqueleto + AnimationPlayer)
│   ├── equipment/           # Accesorios por nivel (generados con Meshy AI)
│   │   ├── Staff.glb        # Lvl 2+ (bastón base; gema hija reemplazable)
│   │   ├── skull_mask.glb   # Lvl 3
│   │   ├── Staff_Gem_Lvl1.glb # Lvl 3-4 (hija de Staff.glb)
│   │   ├── Staff_Gem_Lvl2.glb # Lvl 5-6 (hija de Staff.glb, reemplaza Lvl1)
│   │   └── Staff_Gem_Lvl3.glb # Lvl 7 (hija de Staff.glb, reemplaza Lvl2)
│   └── meshy_models/        # Recursos del mundo (linemate, deraumere, etc.)
├── game.tscn
├── player.tscn
├── terrain.tscn
├── connection.tscn
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
    │   └── CameraFollowBehavior   [CameraFollowBehavior.cs] (Node, añadido en código por Connection._Ready(); lock/orbit/zoom sobre jugador seleccionado)
    ├── DirectionalLight3D
    ├── WorldEnvironment
    ├── Connection (connection.tscn) [Connection.cs]
    │   ├── PlayerManager            [PlayerManager.cs]
    │   ├── EggManager               [EggManager.cs]
    │   ├── ServerTransport          [ServerTransport.cs] (Node, creado en código: socket TCP o MockServer)
    │   ├── InventoryPanel           [InventoryPanel.cs] (UI Control)
    │   ├── MessageLogPanel          [MessageLogPanel.cs] (UI Control, creado en código)
    │   └── TeamProgressPanel        [TeamProgressPanel.cs] (UI Control, creado en código)
    ├── Terrain (terrain.tscn)       [Terrain.cs]
    │   ├── MeshInstance3D           (terrain.gdshader via ShaderMaterial)
    │   ├── GrassSystem              [GrassSystem.cs] (césped procedural via MultiMeshInstance3D)
    │   └── DecorationSystem         [DecorationSystem.cs] (props FBX: árboles/rocas/arbustos/hierba)
    └── ScreenshotService            [ScreenshotService.cs] (herramienta dev: captura PNG)

player.tscn
└── Node3D "Player"  [Player.cs]
    ├── Node3D "Model" (Shaman.glb) — esqueleto bípedo + AnimationPlayer
    └── StaticBody3D + CollisionShape3D
```

---

## Clases Principales

### `Connection.cs` — Hub Central
El corazón del monitor, ahora repartido en varios archivos para mantenerlo delgado:

- **`Connection.cs`** (clase parcial principal): cablea los componentes en `_Ready()` — managers (`PlayerManager`, `EggManager`, `Terrain`, `Camera`), `SelectionController`, `CrowdSystem`, `MessageLogPanel`, `TeamProgressPanel`, `SpeedControlPanel`, `ServerTransport` y `MessageDispatcher`. Recibe `LineReceived(line)` de `ServerTransport`, loguea en `MessageLogPanel` y delega en `MessageDispatcher.Dispatch()`. Mantiene `_UnhandledInput` (F2/F3) y `SendMessage()` (delega a `ServerTransport`).
- **`Connection.Players.cs`** (clase parcial): handlers de jugadores (`pnw/ppo/plv/pin/pex/pbc/pic/pie/pfk/pdr/pgt/pdi`) + estado/efectos asociados (`_incantations`, `ShowPlayerMessage`, `ShowSoundWave`, `ShowIncantationResult`, `FadeOutTile`). Registra sus handlers en `RegisterPlayerHandlers()`.
- **`Connection.Eggs.cs`** (clase parcial): handlers de huevos (`enw/eht/ebo/edi`), registrados en `RegisterEggHandlers()`.
- **`Connection.System.cs`** (clase parcial): handshake (`WELCOME`→`OnWelcome()`), mapa/equipos (`msz/bct/tna`), velocidad (`sgt`, `OnSpeedChanged`, `ApplySpeedFactor`, `_currentSpeedFactor`, `teams`) y mensajería genérica (`smg/seg/suc/sbp`), registrados en `RegisterSystemHandlers()`.
- **`Connection.Timeline.cs`** (clase parcial): backend de la barra de tiempo (ver sección dedicada más abajo). Define `static bool ReplayInstant` y `ResetWorldState()`; instancia `TimelineController` en `_Ready()`.
- **`ServerTransport.cs`** (`Node`, hijo de `Connection`, creado en código): encapsula el socket TCP real **o** `MockServer` de forma transparente. Decide el modo en `ParseConnectionArgs()` (flags `-h`/`-p`/`--mock`) y expone `UseMockServer` (que `Connection` copia para su propio log/estado). En `_Process()` acumula el stream TCP en `_recvBuffer`, procesa solo líneas completas (`\n`) y emite `LineReceived(line)` — o, en modo mock, reenvía cada mensaje de `MockServer.GetNextCommand()`. Ante fin de stream o error de socket (`IOException`/`ObjectDisposedException`), cierra `stream`/`client` y emite `Disconnected(reason)` (que `Connection` loguea en `MessageLogPanel`). `SendMessage(string)` escribe al socket real (no-op en mock, y también no-op durante `ReplayInstant`); `SetMockSpeed(t)` reenvía a `MockServer.SetSpeed()` (usado por `OnSpeedChanged`).
- **`MessageDispatcher.cs`**: router `Dictionary<string, Action<string[]>>` que sustituye al switch monolítico de `HandleServerMessage`. Cada clase parcial de `Connection` registra sus comandos vía `Register(cmd, handler)`; `Dispatch(line)` parsea y enruta (o loguea "Mensaje desconocido").
- **`SelectionController.cs`**: extrae `HandleLeftClick`/`ShowInventory`/`PlayerClicked` (selección por click vía raycast de `Camera` + `InventoryPanel`). Recibe `Terrain` e `InventoryPanel` por constructor; `Connection` lo instancia en `_Ready()` y suscribe `camera.OnLeftClick += _selectionController.HandleLeftClick`.

**Mensajes soportados** (sin cambios de comportamiento, solo de enrutado/ubicación):
| Mensaje | Handler | Efecto |
|---------|---------|--------|
| `WELCOME` | → `SendMessage("GRAPHIC")` | Handshake: respuesta al saludo del servidor |
| `msz W H` | → `Terrain.InitializeMap()` | Crea el mundo |
| `bct X Y q0..q6` | → `Tile.Inventory` | Actualiza recursos de casilla |
| `tna NAME` | → `teams` list | Registra nombre de equipo |
| `pnw #N X Y O L TEAM` | → `PlayerManager.GetOrCreate()` + `Player.SetTilePos()` | Crea jugador y fija su tile lógico (sin esto `TilePos` queda en (0,0) y CrowdSystem arrastra al jugador al tile (0,0) hasta su primer `ppo`/`pin`) |
| `ppo #N X Y O` | → `Player.SetTilePos()` / `SetOrientation()` | Mueve jugador |
| `plv #N L` | → `Player.SetLevel()` | Actualiza nivel |
| `pin #N X Y q0..q6` | → `Player.Inventory` | Actualiza inventario |
| `pex #N` | — | Jugador expulsado |
| `pbc #N MSG` | — | Broadcast de jugador |
| `pic X Y L #N...` | → `Player.PlaySpell()` + `Terrain.SelectTile()` | Inicio de incantación: hechizo + resaltado de tile |
| `pie X Y R` | → `Player.StopSpell()` + `SoundWave` | Fin de incantación: pulso verde/rojo según resultado |
| `pfk #N` | — | Jugador pone huevo |
| `pdr #N ITEM` | — | Jugador suelta recurso |
| `pgt #N ITEM` | — | Jugador recoge recurso |
| `pdi #N` | → `PlayerManager.Remove()` | Jugador muere |
| `enw #E #N X Y` | → `EggManager.CreateEgg()` | Huevo puesto |
| `eht #E` | → `Egg.Hatch()` | Eclosión: transición visual (no elimina el huevo) |
| `ebo #E` | → `EggManager.Remove()` | Jugador se conecta desde el huevo: lo consume (tolerante si ya no existe) |
| `edi #E` | → `EggManager.Remove()` | Huevo muere |
| `sgt T` | — | Tiempo del servidor |
| `seg TEAM` | — | Fin de partida |
| `smg MSG` | → `MessageLogPanel` | Mensaje informativo del servidor (no pausa la escena) |

**Posicionamiento de entidades:** todas usan `x * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f` para centrarlas en su tile.

**Selección y UI:** `SelectionController.HandleLeftClick()` hace raycast desde la cámara. Si impacta un `Player` o `Tile`, llama a `ShowInventory()` que actualiza el `InventoryPanel`. Si impacta un `Resource`, resuelve la casilla bajo él (`GetTileFromPosition`) y muestra el inventario de esa casilla.

**Lectura de red robusta:** `ServerTransport._Process()` acumula el stream TCP en `_recvBuffer` y procesa solo líneas completas (terminadas en `\n`), conservando los fragmentos parciales entre frames. Ante fin de stream (`bytesRead == 0`) o error de socket (`IOException`/`ObjectDisposedException`), cierra limpiamente `stream`/`client`, resetea `_recvBuffer` y emite `Disconnected(reason)`, que `Connection` registra en `MessageLogPanel`.

---

### Barra de tiempo (Timeline / Replay) — backend

Permite "deshacer" hasta un momento anterior y reanudar después con los mensajes ya
recibidos, al estilo de un streaming en vivo. Esta iteración cubre solo el backend; la UI
(`TimelineBar`, slider) es la tarjeta Trello **D9** (pendiente).

- **`EventLog.cs`**: guarda cada línea cruda recibida (`LogEntry(Raw, ReceivedAtMs)`) y las
  agrupa en `TimeBand(StartIndex, EndIndex)` por proximidad de llegada (`BandGapMs = 100.0`).
  El servidor notifica los resultados de las acciones uno a uno, pero los de un mismo tick
  llegan en una ráfaga muy próxima en tiempo real; agruparlos por proximidad da una
  granularidad de scrub con sentido ("qué pasó en este momento") sin depender de `sgt`
  (el Monitor no recibe ticks explícitos del servidor).
- **`TimelineController.cs`**: vive en `Connection._timeline`. Mantiene `Log: EventLog`,
  `CursorBandIndex` (-1 = mundo vacío) e `IsLive`.
  - `OnLineReceived(line)`: añade la línea al `Log`; si `IsLive`, la despacha normalmente
    (animada) y avanza el cursor a la última franja.
  - `JumpTo(bandIndex)`: pone `ReplayInstant = true`, llama a `Connection.ResetWorldState()`
    y reproduce instantáneamente `Log.Messages[0..Bands[bandIndex].EndIndex]` vía
    `MessageDispatcher.Dispatch()`. Al terminar, `ReplayInstant = false`,
    `CursorBandIndex = bandIndex`, `IsLive = (bandIndex == Bands.Count - 1)`.
  - `GoLive()`: `JumpTo(Bands.Count - 1)` + `IsLive = true`. Si llegaron mensajes nuevos
    mientras `IsLive` era `false`, se aplican aquí.
- **`Connection.Timeline.cs`**:
  - `static bool ReplayInstant` — activo durante `JumpTo()`. Lo consultan los handlers con
    efectos visuales para aplicar el resultado final sin animar:
    - `Player.SetTilePos()` ([Player.cs](../entities/player/Player.cs)): si `ReplayInstant`,
      además de fijar `TilePos` clava `GlobalPosition` al centro del tile
      (`TerrainSnap.TileCenter`) y pone `Velocity = Vector3.Zero` (sin esto, `CrowdSystem`
      tendría que recorrer la distancia frame a frame al volver a Live).
    - `Resource._Ready()` ([Resource.cs](../entities/resources/Resource.cs)): si
      `ReplayInstant`, no llama a `PlaySpawnAnimation()` (aparece directo en su posición/escala
      final).
    - `pbc`/`pie` ([Connection.Players.cs](../network/Connection.Players.cs)): si
      `ReplayInstant`, omiten los efectos transitorios sin estado persistente
      (`ShowPlayerMessage`/`ShowSoundWave`/`ShowIncantationResult`).
    - `Connection.SendMessage()`: no-op durante `ReplayInstant` (no se reenvían `mct`/`sgt`/
      `GRAPHIC`... al servidor real durante el replay).
  - `ResetWorldState()` — vacía el mundo para que `JumpTo()` pueda reproducir desde el
    principio: `PlayerManager.Clear()`, `EggManager.Clear()`, `Terrain.Reset()`,
    `_incantations.Clear()`, `teams.Clear()`, `TeamProgressPanel.Reset()` (incluye
    `HideWinner()`), `_currentSpeedFactor = 1f`, `GetTree().Paused = false`.
  - `EntityManager<T>.Clear()` ([EntityManager.cs](../managers/EntityManager.cs)): `QueueFree()`
    de todas las entidades + vacía el diccionario; heredado por `PlayerManager`/`EggManager`.
  - `Terrain.Reset()` ([Terrain.cs](../entities/terrain/Terrain.cs)): libera los `Resource`
    instanciados sobre el terreno y vacía `tileResources`, para que `InitializeMap()` (disparado
    de nuevo por `msz` durante el replay) sea idempotente. `InitializeMap()` llama a `Reset()`
    al empezar.

---

### `Terrain.cs` — Mundo Procedural
Ver skill `/terrain` para contexto completo. Resumen:
- `TILE_SIZE = 2.0f` (const pública) — controla escala de todo el mundo
- Genera `ArrayMesh` con heightmap Perlin en `InitializeMap()`
- Sincroniza `tile_size` del shader al generar el mesh
- Instancia nodos `Resource` en tiles al recibir `bct` vía `Inventory.Changed`; cada recurso se posiciona con un offset pseudoaleatorio dentro del tile, sembrado por `(x, y, tipo)` (`GetResourceOffset`) — varía por tile/tipo pero es estable entre actualizaciones de inventario, evitando parpadeos posicionales
- `GetTileFromPosition()` divide por `TILE_SIZE` antes de hacer `FloorToInt`
- Tras generar el mesh, `DecorationSystem.Generate()` esparce props GLB (árboles/rocas/arbustos/hierba) sobre el heightmap, descubriendo modelos por convención de nombre `<Tipo>_<Letra>_<Ancho>x<Largo>.glb` en `entities/terrain/models/` (sin listas hardcodeadas)
- `GrassSystem` y `DecorationSystem` son complementarios, no redundantes (C11): `GrassSystem` cubre todo el mapa con una "alfombra" densa de billboards animados por shader (viento); `DecorationSystem` reparte props grandes y estáticos de forma dispersa vía occupancy grid — las matas `Grass_*_1x1.glb` son solo uno de sus cuatro tipos de prop, a modo de variedad puntual junto a árboles/rocas/arbustos, no un sustituto del césped base

---

### `Player.cs` — Entidad Jugador
Hereda de `SelectableInventoryNode3D`. Al crearse instancia `player.tscn`, que contiene el modelo bípedo (`Shaman.glb`, nodo `"Model"`).

- **Movimiento (steering / boids):** `SetTilePos()` solo registra el tile destino; el desplazamiento real lo conduce `CrowdSystem` (ver abajo), que cada frame dirige al jugador al centro de su tile (*arrival*) separándolo de los vecinos (*separation*). La velocidad máxima y el `SpeedScale` de la animación escalan con el time unit del servidor (`Connection.ApplySpeedFactor` → `Player.SetSpeedFactor`, desde `OnSpeedChanged`/`sgt`/`pnw`). `UpdateLocomotion(speed)` elige idle/`PlayWalk`/`PlayRun` (corre cuando `SpeedFactor` supera el umbral).

### `CrowdSystem.cs` — Posicionamiento dinámico
Nodo bajo `Connection`. Cada frame itera `PlayerManager.All` y aplica steering tipo boids (Reynolds): **arrival** hacia `TerrainSnap.TileCenter(p.TilePos)` + **separation** de los jugadores cercanos, con velocidad escalada por `Player.SpeedFactor` y la altura siguiendo `Terrain.GetTileHeight`. La **separation es tile-local**: solo considera vecinos con el mismo `TilePos`, y su alcance efectivo se acota a `Terrain.TILE_SIZE` (`sepDist = Min(SeparationDist, TILE_SIZE * 0.9f)`), de modo que la afección nunca cruza a celdas vecinas. Resultado: varios jugadores comparten tile agrupándose sin solaparse, sin empujar a los de celdas contiguas. Parámetros (`BaseSpeed`, `SeparationDist`, pesos, `Damping`) son `[Export]` tuneables.
- **Movimiento:** `SetTilePos()` lanza un `Tween` cuya duración (`BaseMoveDuration / SpeedFactor`) y el `SpeedScale` de la animación escalan con el time unit del servidor (`Connection.ApplySpeedFactor` → `Player.SetSpeedFactor`, desde `OnSpeedChanged`/`sgt`/`pnw`). Por debajo del umbral camina (`PlayWalk`), por encima corre (`PlayRun`); al completar, `PlayIdle()`. Posición = `x * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f`.
- **Orientación:** `SetOrientation()` mapea 1=N, 2=E, 3=S, 4=W a rotación Y.
- **Equipamiento:** `_Ready()` y `SetLevel()` llaman a `equipmentManager.ApplyLoadout()` con el loadout de `ShamanEquipmentConfig.GetLoadout(level)`.
- **Animaciones:** delegadas a `ShamanAnimationController` (ver abajo).

### `ShamanAnimationController.cs` — Controlador de Animaciones
Encapsula toda la lógica de animación del Shaman siguiendo el mismo patrón que `EquipmentManager`. `Player.cs` solo llama a `PlayWalk()`, `PlayIdle()`, etc., sin conocer strings internos ni el `AnimationPlayer`.

- Clase interna privada `Clip` centraliza los nombres de animación del GLB: `idle`, `walking`, `running`, `spell_cast`, `collect_object`, `pick_up_pocket`.
- `EnableLoopOnAll()` en el constructor activa `LoopModeEnum.Linear` en todas las clips al inicializar.

### `EquipmentManager.cs` + `EquipmentSlot.cs` — Sistema de Equipamiento
Patrón portable entre proyectos. `EquipmentManager`, `EquipmentSlot`, `EquipmentChild`, `OrbitingPivot`, `OrbSpec`, `GlowOrb` y `GlowEffect` son genéricos (sin datos de proyecto). `ShamanEquipmentConfig` es el único archivo específico: mapea nivel → lista de `EquipmentSlot(boneName, glbPath)`.

- `ApplyLoadout(owner, slots)` → limpia adjuntos actuales y adjunta los nuevos a los huesos del `Skeleton3D`.
- `AttachOrbitingGroup(owner, boneName, pivotOffsets, rotationSpeedDeg, orbs)` → crea un `OrbitingPivot` (Node3D que rota sobre Y) anclado al hueso, con una lista de `OrbSpec` (orbes procedurales) alrededor; si `orbs` es null/vacío no adjunta nada. Cada `OrbSpec` se instancia como un `GlowOrb` (esfera translúcida + rim + `GlowEffect`). Se registra junto al resto de adjuntos del hueso, así que `ApplyLoadout()`/`ClearAll()` también lo limpian.
- Para reusar en otro proyecto: copiar `EquipmentManager.cs`, `EquipmentSlot.cs`, `EquipmentChild.cs`, `OrbitingPivot.cs`, `OrbSpec.cs`, `GlowOrb.cs`, `GlowEffect.cs`, `Offsets.cs` y crear un nuevo `XxxEquipmentConfig.cs`.

**Orbes brillantes sobre la cabeza (D6, sustituyen al collar — ver C10):** `ShamanEquipmentConfig.GetOrbitingGems(level)` devuelve el grupo de orbes para el hueso `Head`: `null` en niveles 1-3 (sin orbes), 2 orbes en niveles 4-5, 3 orbes en niveles 6-7, distribuidas en círculo. Cada orbe es un `GlowOrb` (esfera procedural + `GlowEffect`, sin GLB) con un color/brillo arcano único (`OrbColor`/`OrbGlow`). `Player.ApplyEquipment()` llama a `ApplyLoadout()` y luego a `AttachOrbitingGroup()` en `_Ready()` y `SetLevel()`. Offsets de posición/escala son placeholders pendientes de ajuste visual en el editor.

**Huesos del Shaman disponibles para equipamiento:**
`neck`, `headfront`, `Head`, `RightHand`, `LeftShoulder`, `RightShoulder`, `LeftForeArm`, `RightForeArm`

---

### `Resource.cs` — Entidad Recurso
Carga automáticamente `res://models/{tipo}.glb` si existe (Meshy AI); si no, usa `SphereMesh` coloreada. Los modelos GLB se escalan a `0.15f`. Añadir un nuevo tipo = generar el GLB con `meshy generate` y colocarlo en `res://models/`.

---

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

### `ScreenshotService.cs` — Herramienta de Desarrollo
Nodo bajo `Game` que vuelca el framebuffer de la ventana principal a PNG en disco para verificación visual sin capturas manuales. Captura el render final (post-proceso/glow incluidos) tras esperar `RenderingServer.FramePostDraw`.
- **Auto-captura:** un `Timer` (`CaptureInterval`, def. 2 s, `[Export]`) sobrescribe `res://.captures/latest.png` — el archivo refleja siempre el estado actual. Vía pensada para inspección automatizada.
- **Tecla F12:** guarda `latest.png` + una copia con timestamp `shot_yyyyMMdd_HHmmss.png` (uso manual).
- `OutputDir` resuelto con `ProjectSettings.GlobalizePath`; sólo escribible corriendo sin empaquetar (editor o `--path`). El directorio `.captures/` está en `.gitignore`.

---

## Flujo de Datos

```
Servidor TCP / MockServer
    │
    ▼
ServerTransport._Process()  ← lee stream TCP (o MockServer) cada frame, reensambla líneas por \n
    │ evento LineReceived(line)
    ▼
Connection.OnLineReceived()  ← loguea en MessageLogPanel
    │
    ▼
MessageDispatcher.Dispatch()  ← parsea comando y enruta a Action<string[]> registrada
    │
    ├─► (Connection.Players.cs) PlayerManager.GetOrCreate() → Player.Init() / SetTilePos() / SetLevel()
    ├─► (Connection.Eggs.cs)    EggManager.CreateEgg() / Remove()
    ├─► (Connection.System.cs)  Terrain.InitializeMap() → genera mesh + shader sync
    ├─► (Connection.System.cs)  Tile.Inventory.Set() → Changed → Terrain.UpdateTileResources()
    └─► InventoryPanel.ShowForTile() ← desde SelectionController.HandleLeftClick()

Camera._UnhandledInput()
    │ click izquierdo
    ▼
Camera.OnLeftClick signal
    │
    ▼
SelectionController.HandleLeftClick()
    ├─► Player.Highlight() + ShowInventory()
    └─► Terrain.GetTileFromPosition() + Tile.Highlight() + ShowInventory()
```

---

## Áreas de Mejora / Pendientes

- **Mundo toroidal:** El terreno se genera como plano; no hay wrap-around visual.
- **Typo:** `UnHightlight()` debería ser `UnHighlight()` en `ISelectable.cs` y `SelectableInventoryNode3D.cs`.
- **Altura de jugadores:** Y fija en `0.3f`; no sigue la altura real del terreno.
- **Barra de tiempo (UI):** el backend (`EventLog`/`TimelineController`, ver sección dedicada) está implementado; falta `TimelineBar.cs`/`.tscn` (slider + "Live") — tarjeta Trello D9.
