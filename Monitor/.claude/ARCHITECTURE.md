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
| Audio/música | ✅ | `MusicPlayer` (`audio/MusicPlayer.cs`): música de fondo en bucle, hijo directo de `Game` (sobrevive al replay/reset de la timeline), `ProcessMode.Always` (sigue sonando con `GetTree().Paused = true` tras `seg`), botón Mute de icono (toggle, estilo `ui/IconButton.cs`)/tecla M |

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
├── TimelineController.cs   # Backend de la barra de tiempo: cursor de franja, IsLive, IsPlaying, JumpTo()/GoLive() (reset + replay instantáneo) y Play()/Pause()/Tick() (reproducción franja a franja)
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
├── PlacementFinder.cs      # Helper estático genérico: busca un offset/posición libre dentro de una región evitando obstáculos circulares (XZ + radio); usado por Terrain para no embeber recursos en decoraciones (C12)
├── IInventory.cs           # Interfaz: objeto con inventario
├── ISelectable.cs          # Interfaz: objeto seleccionable (highlight)
├── Inventory.cs            # Modelo de datos: 7 tipos de recurso
├── CollapsiblePanel.cs     # UI base: panel con título, botón ✕ (icono) y botón de restauración (icono por panel vía Setup(minimizedIcon))
├── IconButton.cs           # UI: helper estático de estilo para botones de icono (iconos en ui/icons/*.svg, tamaño/estilo uniforme)
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
├── entities/
│   └── sky/
│       └── DayNightCycle.cs # Ciclo día/noche: Sol+Luna direccionales + cielo/ambiente según TimeOfDay
├── audio/
│   ├── music.mp3           # Pista de música de fondo (loop manual, ver MusicPlayer.cs)
│   ├── MusicPlayer.cs       # Música de fondo en bucle (Finished->Play()); botón Mute de icono (toggle) + tecla M; ProcessMode.Always
│   └── MusicPlayer.tscn     # UI: Button (toggle) Mute con icono altavoz/tachado en esquina superior derecha + AudioStreamPlayer hijo
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
    ├── DayNightCycle               [DayNightCycle.cs] (Node3D; controla Sol/Luna + cielo según TimeOfDay)
    │   ├── Sun                     (DirectionalLight3D; antigua luz estática, ahora dinámica)
    │   └── Moon                    (DirectionalLight3D; nueva, luz nocturna azulada)
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
    │   ├── DecorationSystem         [DecorationSystem.cs] (props FBX: árboles/rocas/arbustos/hierba)
    │   └── WaterSystem              [WaterSystem.cs] (mar procedural infinito via water.gdshader; sigue a la cámara)
    ├── ScreenshotService            [ScreenshotService.cs] (herramienta dev: captura PNG)
    └── MusicPlayer (MusicPlayer.tscn) [MusicPlayer.cs] (música de fondo en bucle, ProcessMode.Always; hijo directo de Game para no verse afectado por ResetWorldState; Button toggle Mute con icono arriba-derecha + tecla M)

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
| `pdr #N ITEM` | → `Player.PlayPickUp()` | Jugador suelta recurso: gesto one-shot, vuelve a Idle solo |
| `pgt #N ITEM` | → `Player.PlayCollect()` | Jugador recoge recurso: gesto one-shot, vuelve a Idle solo |
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

### Barra de tiempo (Timeline / Replay)

Permite "deshacer" hasta un momento anterior y reanudar después con los mensajes ya
recibidos, al estilo de un streaming en vivo. UI: `ui/TimelineBar.cs` + `ui/timeline_bar.tscn`
(slider + botones de icono "Live" y "Play/Pause", estilo unificado `ui/IconButton.cs`) —
tarjetas Trello D9 (slider/Live) y B10 (Play/Pause).

- **`EventLog.cs`**: guarda cada línea cruda recibida (`LogEntry(Raw, ReceivedAtMs)`) y las
  agrupa en `TimeBand(StartIndex, EndIndex)` por proximidad de llegada (`BandGapMs = 100.0`).
  El servidor notifica los resultados de las acciones uno a uno, pero los de un mismo tick
  llegan en una ráfaga muy próxima en tiempo real; agruparlos por proximidad da una
  granularidad de scrub con sentido ("qué pasó en este momento") sin depender de `sgt`
  (el Monitor no recibe ticks explícitos del servidor).
- **`TimelineController.cs`**: vive en `Connection._timeline`. Mantiene `Log: EventLog`,
  `CursorBandIndex` (-1 = mundo vacío), `IsLive` e `IsPlaying`.
  - `OnLineReceived(line)`: añade la línea al `Log`; si `IsLive`, la despacha normalmente
    (animada) y avanza el cursor a la última franja.
  - `JumpTo(bandIndex)`: pone `ReplayInstant = true`, llama a `Connection.ResetWorldState()`
    y reproduce instantáneamente `Log.Messages[0..Bands[bandIndex].EndIndex]` vía
    `MessageDispatcher.Dispatch()`. Al terminar, `ReplayInstant = false`,
    `CursorBandIndex = bandIndex`, `IsLive = (bandIndex == Bands.Count - 1)`.
  - `GoLive()`: `JumpTo(Bands.Count - 1)` + `IsLive = true` + `IsPlaying = false`. Si llegaron
    mensajes nuevos mientras `IsLive` era `false`, se aplican aquí.
  - **Modo Play (B10)**: `Play()` activa `IsPlaying` (no-op si `IsLive` o si el cursor ya está
    en la última franja); `Pause()` lo desactiva sin mover el cursor. `Tick(delta)` —llamado
    desde `TimelineBar._Process` en cada frame— acumula `delta` y, al superar
    `BaseStepIntervalSeconds` (0.6s, escalado por `Connection.CurrentSpeedFactor` para ir más
    rápido si `sgt` > 1), llama a `JumpTo(CursorBandIndex + 1)`. Al llegar a la última franja
    conocida, `Tick` llama a `GoLive()` y termina el modo Play.
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
- **Margen de borde (falloff de isla):** la malla se genera sobre un grid extendido `±BorderMargin` (def. 4) anillos alrededor de la región jugable `[0..Width, 0..Height]`. El `heightMap` sigue siendo solo jugable `[W+1, H+1]` (recursos/entidades/hierba/decoración/`GetTileHeight` intactos); las esquinas del margen las calcula `CornerHeight(cx, cy)`, que mezcla la altura natural de ruido hacia `outerY = _minHeight - SkirtDepth` (def. 5) según un `SmoothStep` de cuán fuera del grid jugable está la esquina. Así el terreno **desciende bajo el nivel del mar en los bordes**, ocultando la cara inferior de la malla flotando sobre el agua. Las casillas del margen **no son seleccionables** (coordenadas fuera de rango → `GetTile` devuelve `null`). `BorderMargin`/`SkirtDepth` son `[Export]` tuneables
- Tras generar el mesh, `DecorationSystem.Generate()` esparce props GLB (árboles/rocas/arbustos/hierba) sobre el heightmap, descubriendo modelos por convención de nombre `<Tipo>_<Letra>_<Ancho>x<Largo>.glb` en `entities/terrain/models/` (sin listas hardcodeadas)
- `GrassSystem` y `DecorationSystem` son complementarios, no redundantes (C11): `GrassSystem` cubre todo el mapa con una "alfombra" densa de billboards animados por shader (viento); `DecorationSystem` reparte props grandes y estáticos de forma dispersa vía occupancy grid — las matas `Grass_*_1x1.glb` son solo uno de sus cuatro tipos de prop, a modo de variedad puntual junto a árboles/rocas/arbustos, no un sustituto del césped base
- `WaterSystem` ([WaterSystem.cs](../entities/terrain/WaterSystem.cs), D10) rodea el terreno de un **mar procedural infinito**: un único plano grande (sin colisión, así que la selección de tile sigue impactando el terreno bajo el agua) que `_Process` **recentra sobre la cámara** cada frame; como el shader es world-space, deslizar el plano no desplaza el patrón → el agua llega siempre al horizonte sin bordes. `Generate()` lo coloca a un nivel del mar relativo al heightMap (`SeaLevelFraction`, def. 0.35) → **archipiélago**: los valles quedan sumergidos y los picos sobresalen como islotes, en cualquier tamaño de mapa. `water.gdshader` (transparente, `depth_draw_never`) anima **caústicas** (capas de voronoi scrolleando), perturba la normal para specular en movimiento, añade fresnel hacia el horizonte y usa `DEPTH_TEXTURE` para la **costa**: aclara el color, sube la transparencia y dibuja una banda de **espuma** animada donde el agua toca el terreno. No modifica el `WorldEnvironment`.
- **C12 — evitar recursos embebidos en decoraciones:** `DecorationSystem` expone `Obstacles` (`IReadOnlyList<PlacementFinder.Obstacle>`, poblada en `PlaceInstance` durante `Generate()`) con la posición world XZ y un radio de exclusión por cada prop colocado (mitad de la diagonal de su footprint). `Terrain.GetNearbyDecorationObstacles(x, y)` filtra los obstáculos cercanos al tile (`DecorationProximityRange = TILE_SIZE * 1.5f`) y `GetResourceOffset()` los pasa a `PlacementFinder.FindFreeOffset()` junto con el mismo RNG sembrado por `(x, y, tipo)`, manteniendo el determinismo (sin parpadeos). Si tras varios intentos no hay hueco libre, hace fallback al último offset candidato (comportamiento previo sin chequeo de colisión)

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
- `SpellDuration`/`CollectDuration`/`PickUpDuration` exponen la longitud real (segundos) de `spell_cast`/`collect_object`/`pick_up_pocket` leída del propio `AnimationPlayer`; `IsPlayingCollect`/`IsPlayingPickUp` indican si esos clips son el `CurrentAnimation` actual.
- `Player.PlayCollect()`/`PlayPickUp()` (llamados desde `pgt`/`pdr`) son gestos "one-shot": reproducen el clip y, tras su duración real, vuelven a `PlayIdle()` solos vía `Player.PlayOneShot()` (no hay mensaje de "fin" del servidor para estos, a diferencia de `pic`/`pie` con `PlaySpell`/`StopSpell`). `PlayOneShot` solo fuerza Idle si el clip one-shot sigue siendo el actual (no interrumpe una incantación u otra animación que haya empezado mientras tanto).

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

### `DayNightCycle.cs` — Ciclo día/noche
Controlador `Node3D` (`entities/sky/DayNightCycle.cs`) que da vida a un ciclo día/noche dinámico. Sustituye a la antigua `DirectionalLight3D` estática del `game.tscn`: esa luz pasa a ser el hijo `Sun` del nodo `DayNightCycle`, y se añade un hijo nuevo `Moon` (luz nocturna azulada). El `WorldEnvironment` hermano se cablea por `NodePath` (`[Export] WorldEnv`) para acceder a su `ProceduralSkyMaterial`.

- **Todo deriva de `TimeOfDay`** (`[Export(PropertyHint.Range, "0,1")]`, 0=medianoche, 0.25=amanecer, 0.5=mediodía, 0.75=atardecer). `Apply(t)` calcula la elevación del sol (`el = -cos(t·τ)`), orienta Sol y Luna en arcos opuestos, y deriva un `dayFactor` (`SmoothStep`) que controla:
  - **Iluminación:** energía/color del Sol (de naranja en el horizonte a blanco cálido al mediodía) y de la Luna (energía inversa al sol, color azul fijo). Energías máximas tuneables (`MaxSunEnergy`, `MaxMoonEnergy`).
  - **Cielo:** lerp de `SkyTopColor`/`SkyHorizonColor`/`GroundHorizonColor` entre paletas día/noche, con un tinte cálido (`HorizonDuskColor`) que aparece cerca del horizonte al amanecer/atardecer. El cielo nocturno se mantiene azul oscuro (nunca negro puro) para que el ambiente derivado del cielo conserve algo de visibilidad; la Luna aporta el relleno direccional nocturno.
- **Ciclo automático en reloj de pared independiente:** con `AutoRun` (def. `true`), `_Process` avanza `TimeOfDay` según `DayDurationSeconds` (def. 120 s por ciclo completo). NO está ligado al tiempo del servidor ni a la timeline/replay. Usa el process mode por defecto, así que **se pausa junto con el juego** (p. ej. tras `seg`).
- **Controles en runtime** (`_UnhandledInput`): **L** alterna `AutoRun` (pausa/reanuda el ciclo); **`[`** y **`]`** retroceden/avanzan la hora del día en pasos de 0.02 (scrub manual, aplicando al instante). Teclas libres: la cámara usa WASD/QE/ratón/rueda/clic-derecho y los toggles existentes son M, F2, F3, F12.
- **Solo iluminación y cielo:** no hay mallas/discos de sol o luna visibles, solo luces direccionales + cambios de cielo/ambiente.
- **Limitación conocida:** `grass.gdshader` es `unshaded`, por lo que el césped no se oscurece de noche (no responde a la iluminación de la escena).

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
- **Altura de jugadores:** Y fija en `0.3f`; no sigue la altura real del terreno.
- **Barra de tiempo (UI):** `EventLog`/`TimelineController`/`TimelineBar` (slider + "Live" +
  "Play/Pause", ver sección dedicada) implementados — tarjetas Trello D9 y B10 resueltas.
