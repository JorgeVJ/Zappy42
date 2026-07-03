# Animals — Sistema de animales decorativos (peces)

Skill de referencia para el sistema de fauna decorativa del Zappy Monitor (actualmente: peces payaso nadando en el agua). Leer antes de modificar, ampliar o portar este sistema a otro proyecto.

---

## Diseño: autocontenido y portable

Este sistema se diseñó deliberadamente **independiente del resto del proyecto**, para poder copiarlo/pegarlo a otro proyecto Godot con heightmap, o eliminarlo por completo sin dejar rastro. Reglas que mantienen esa independencia:

- **Ningún** archivo de `entities/animals/` (`AnimalSystem.cs`, `Animal.cs`, `Fish.cs`, locomoción, comportamientos) referencia `Terrain`, `Connection`, `TerrainSnap`, `CrowdSystem`, `EntityManager` ni ningún otro tipo específico de este proyecto. Solo usan tipos de Godot (`Node3D`, `Skeleton3D`, etc.), primitivas (`float[,] heightMap, int width, int height`) y las interfaces/dominios de `spatial/` (ver más abajo).
- `entities/animals/` depende de la carpeta hermana `spatial/` para saber **dónde puede moverse un animal** (`IAnimalDomain`, `AquaticDomain`). `spatial/` es a su vez 100% Godot-only y no depende de nada de `entities/animals/` ni del resto del proyecto — también la consume `entities/terrain/` (p. ej. `TerrainDomain` para restringir dónde nace la vegetación), así que la dependencia siempre va de los sistemas concretos hacia `spatial/`, nunca al revés.
- No usa `PlacementFinder` (evita solapes entre decoraciones en tierra; no aplica a peces en agua) ni `EntityManager<T>` (eso es para entidades con ID de servidor — altas/bajas dinámicas; los peces se generan una sola vez junto con el terreno y no tienen ID). `PlacementFinder` vive en `spatial/` junto a los dominios (misma carpeta portable), pero es una utilidad independiente que `entities/animals/` no consume.
- No depende de `Connection.ReplayInstant` ni de ningún global estático del proyecto.

## Archivos

El sistema está organizado en **capas** (pensadas para crecer a animales terrestres/aéreos y a un futuro Utility AI):

| Archivo | Capa | Rol |
|---|---|---|
| `entities/animals/AnimalSystem.cs` | Colocación | Recibe el heightmap, calcula tiles de agua, construye el **dominio** y reparte `FishCount` peces al azar (modelo al azar de `FishModels`), inyectándoles dominio + tuning. |
| `spatial/ISpatialDomain.cs` | Dominio | Interfaz mínima "**es válido este punto**": sólo `Contains(worldPos)`. Base de `IAnimalDomain`; también la implementa `spatial/TerrainDomain.cs` (consumida por `entities/terrain/DecorationSystem.cs`) para restringir dónde nace la vegetación. |
| `spatial/IAnimalDomain.cs` | Dominio | Interfaz "**dónde puede moverse** un animal": extiende `ISpatialDomain` y añade `ClampToValid`, `SampleWanderTarget`. El eje del diseño. |
| `spatial/AquaticDomain.cs` | Dominio | Implementación acuática: volumen de agua entre el fondo (+margen) y la superficie del mar (−margen), construido desde el heightmap. Usa los structs `spatial/HeightMapGrid.cs` y `spatial/NavigableMargins.cs`. |
| `entities/animals/AnimalLocomotion.cs` | Locomoción | Steering procedural genérico (estilo `CrowdSystem`): mueve un `Node3D` hacia un objetivo con aceleración/frenado suaves y giro gradual hacia el rumbo. |
| `entities/animals/IAnimalBehavior.cs` | Comportamiento | Interfaz de comportamiento (`Enter`/`Tick`/`Score`). `Score` es la utilidad para el cerebro. |
| `entities/animals/WanderBehavior.cs` | Comportamiento | Pasear: elige destinos del dominio con pausas. `Score` = baseline constante (estado por defecto). |
| `entities/animals/FleeBehavior.cs` | Comportamiento | Huir de la cámara: `Score` sube al acercarse la cámara; al activarse, acelera el nado y elige destinos alejándose. |
| `entities/animals/ScoringUtils.cs` | Utility AI | Curvas de respuesta (`Normalize`, `Proximity`, `Falloff`) para construir scores. Espejo del proyecto de referencia. |
| `entities/animals/UtilityBrain.cs` | Utility AI | `IAnimalBehavior` compuesto: puntúa los comportamientos candidatos y ejecuta el de mayor `Score`, reevaluando con histéresis. |
| `entities/animals/Animal.cs` | Entidad base | `Node3D` genérico que reúne dominio + locomoción + comportamiento (el cerebro) y los ejecuta cada frame; hook `OnLocomotionUpdate(speed)` para animación. |
| `entities/animals/Fish.cs` | Entidad | `Fish : Animal`. Carga el `.glb` (ruta como parámetro), anima los huesos `Body`/`Tail` por código y modula el aleteo con la velocidad. Sirve para cualquier especie con ese rig. |
| `entities/animals/ClownFish.glb`, `entities/animals/SurgeonFish.glb` | Asset | Modelos con 2 huesos `Body`/`Tail`, sin animaciones. Mismo rig → intercambiables por la misma clase `Fish`. |

## Único punto de integración externo

Todo el acoplamiento con el resto del proyecto se reduce a estas dos líneas (en `entities/terrain/Terrain.cs` y `entities/terrain/terrain.tscn`):

```csharp
// Terrain.cs — campo
private AnimalSystem _animalSystem;

// Terrain.cs — _Ready()
_animalSystem = GetNodeOrNull<AnimalSystem>("AnimalSystem");

// Terrain.cs — GenerateTerrainMesh(), justo después de _waterSystem?.Generate(...)
_animalSystem?.Generate(heightMap, Width, Height);
```

```
# terrain.tscn — nodo hijo de Terrain, paralelo a WaterSystem
[node name="AnimalSystem" type="Node3D" parent="."]
script = ExtResource("...AnimalSystem.cs")
```

**Para portar el sistema a otro proyecto:** copiar las carpetas `entities/animals/` **y** `spatial/` (esta última aporta `IAnimalDomain`/`AquaticDomain`, sin los cuales `entities/animals/` no compila) y añadir una llamada a `animalSystem.Generate(heightMap, width, height)` en cualquier punto donde ese proyecto tenga un `float[,] heightMap` con su ancho/alto — no requiere más que eso. Si el proyecto destino ya tiene su propia copia de `spatial/` (p. ej. porque también se portó `TerrainDomain`/`PlacementFinder`), basta con `entities/animals/`.

**Para eliminarlo por completo:** borrar `entities/animals/`, quitar el campo `_animalSystem`, la línea de `GetNodeOrNull` y la línea de `Generate(...)` en `Terrain.cs`, y quitar el nodo `AnimalSystem` + su `ext_resource` de `terrain.tscn`. `spatial/` se puede conservar si algún otro sistema (p. ej. `DecorationSystem`) sigue usando `TerrainDomain`/`PlacementFinder`.

## Cómo decide dónde colocar los peces

`AnimalSystem.Generate` replica (no reutiliza) el mismo cálculo de nivel del mar que `entities/terrain/WaterSystem.cs` — `Mathf.Lerp(min, max, SeaLevelFraction)` sobre el heightmap — porque ambos sistemas son hermanos desacoplados en el `.tscn` y no deben depender uno del otro. Si se cambia `SeaLevelFraction`/`SeaLevelOffset` en `WaterSystem`, hay que replicar el cambio en `AnimalSystem` (son `[Export]` independientes, no están sincronizados automáticamente).

Pasos del algoritmo:
1. Calcula `seaY` con el heightmap.
2. Recorre todos los tiles `(x, y)` y se queda con los que tienen altura de tile (promedio de las 2 esquinas diagonales, misma fórmula que `Terrain.GetTileHeight`) por debajo de `seaY`.
3. Construye un `AquaticDomain` compartido (el volumen navegable) desde el heightmap + `seaY` + márgenes.
4. Elige `FishCount` tiles al azar de esa lista y coloca un `Fish.Create(pos, modelPath)` en el centro de cada tile, a media columna y ajustado al volumen con `domain.ClampToValid`. A cada pez le inyecta `Domain`, `Locomotion.MaxSpeed` y un `WanderBehavior { WanderRadius }`. El `modelPath` se elige al azar de `FishModels` → mezcla de especies.
5. Si no hay tiles de agua, no genera nada — no hay fallback.

## Movimiento / paseo (capas dominio · locomoción · comportamiento)

El eje del diseño es que **el animal sepa a dónde puede moverse**, vía la abstracción `IAnimalDomain`. Cada frame, `Animal._Process` ejecuta: `Behavior.Tick` (decide destino) → `Locomotion.Tick` (avanza/gira hacia él, sin salir del dominio) → `OnLocomotionUpdate(speed)` (la especie ajusta su animación).

- **Dominio (`IAnimalDomain` / `AquaticDomain`)**: responde `Contains(pos)`, `ClampToValid(pos)` y `SampleWanderTarget(from, radius, rng)`. El acuático define un **volumen 3D**: columnas de agua entre `fondo+FloorMargin` y `seaY−SurfaceMargin`. Replica el muestreo bilineal de altura internamente (sin `TerrainSnap`) para no acoplar.
- **Locomoción (`AnimalLocomotion`)**: clase simple (no nodo) que imita el steering de `CrowdSystem` — `Velocity.Lerp(desiredVel, Damping*dt)`, frenado de llegada, `ClampToValid` cada paso, y giro suave (slerp de orientación, con pitch para subir/bajar; **no** snapping a 90° como `Player`).
- **Comportamiento (`IAnimalBehavior` / `WanderBehavior`)**: pasea eligiendo destinos cercanos del dominio con pausas ocasionales. Es la **costura del futuro Utility AI**: hoy cada animal corre un único comportamiento; mañana un `UtilityBrain` elegirá entre varios por `Score` (ver comentario en `IAnimalBehavior`).

**No-objetivos actuales:** sin pathfinding (los saltos cortos validados por `Contains` bastan para un paseo decorativo; en aguas no convexas un tramo recto puede rozar tierra brevemente), sin Utility AI todavía (solo el interfaz + `WanderBehavior`), sin separación entre individuos.

## Parámetros `[Export]`

**`AnimalSystem`**
- `FishCount` (3–6 recomendado, por defecto 6, rango 0–20 en el inspector) — cantidad de peces a generar.
- `FishModels` — array de rutas `.glb` entre las que se elige al azar por cada pez. Para añadir una especie nueva: meter un `.glb` con huesos `Body`/`Tail` en `entities/animals/` y añadir su ruta a este array (no requiere tocar código). Si queda vacío, no se genera nada.
- `SeaLevelFraction` / `SeaLevelOffset` — deben coincidir con los de `WaterSystem` si se quiere que los peces queden visualmente bajo el agua real.
- `FloorMargin` / `SurfaceMargin` — holgura que el pez deja respecto al fondo y a la superficie (define la altura del volumen navegable).
- `MaxSpeed` — velocidad de nado máxima (se inyecta en `Locomotion`).
- `WanderRadius` — radio de los saltos de paseo (se inyecta en `WanderBehavior`).
- `TileSize` — debe coincidir con `Terrain.TILE_SIZE` (por defecto 2.0); no se referencia la constante del proyecto a propósito (ver "Diseño").

**`Fish`**
- `TailFrequency` / `TailAmplitudeDegrees`, `BodyFrequency` / `BodyAmplitudeDegrees` — frecuencia/amplitud base del aleteo de cola y balanceo del cuerpo (en contrafase).
- `SpeedTailBoost` — cuánto acelera el aleteo con la velocidad de nado (0 = constante).
- Cada instancia arranca con fase aleatoria para no nadar sincronizada.

- Tuning de huida (Utility AI): `FleeInner` / `FleeOuter` (distancias de cámara para huida máx/nula), `FleeSpeedScale` (aceleración al huir).

**Locomoción/comportamiento** (no son `[Export]`; defaults en código, configurables si se exponen): `AnimalLocomotion` (`MaxSpeed`, `SpeedScale`, `Damping`, `ArrivalRadius`, `TurnSpeed`); `WanderBehavior` (`WanderRadius`, `WanderWeight`, `PauseChance`, `PauseMin/Max`); `FleeBehavior` (`FleeInner/Outer`, `FleeWeight`, `FleeSpeedScale`, `FleeStep`); `UtilityBrain` (`EvalInterval`, `SwitchMargin`).

## Animación procedural de huesos

Los modelos no traen `AnimationPlayer`. `Fish._Ready()` busca el `Skeleton3D` recursivamente dentro del modelo instanciado, resuelve `FindBone("Body")` / `FindBone("Tail")` y guarda la pose de reposo (`GetBoneRest`). La animación se aplica en `OnLocomotionUpdate(speed)` (llamado cada frame desde `Animal._Process`): compone una rotación sinusoidal sobre la pose de reposo y la aplica con `SetBonePoseRotation`, **modulando frecuencia y amplitud según la velocidad** de nado (aleteo suave en reposo, más vivo al crucero). Si un `.glb` cambia de nombres de hueso, actualizar los strings `"Body"`/`"Tail"` en `Fish.cs` — si `FindBone` devuelve `-1` el hueso simplemente no se anima (sin warnings, sin crash).

## Añadir una especie de pez nueva

Mientras el modelo comparta el rig de 2 huesos `Body`/`Tail`, **no hace falta tocar código**: meter el `.glb` en `entities/animals/` y añadir su ruta al array `FishModels` de `AnimalSystem` (en el inspector o en el valor por defecto del `[Export]`). La misma clase `Fish` lo anima. Las especies se mezclan al azar por posición.

## Añadir animales terrestres / aéreos (capas listas)

La arquitectura ya está preparada para ello sin tocar locomoción ni comportamiento:
- **Nuevo dominio**: crear `TerrestrialDomain` (superficie: `Y` pegado a la altura del terreno, X/Z en tiles de tierra) o `AerialDomain` (volumen de aire sobre el terreno hasta un techo) en `spatial/`, implementando `IAnimalDomain` completa (no basta con `ISpatialDomain`: un animal terrestre/aéreo también necesita `ClampToValid`/`SampleWanderTarget` para moverse). Es la única pieza específica del medio. Nota: `spatial/TerrainDomain.cs` ya cubre el caso de "punto válido en tierra" para la colocación de decoraciones, pero sólo implementa `ISpatialDomain` — no sirve tal cual para un animal que necesite pasear, sólo como referencia de la fórmula de altura de tierra.
- **Nueva entidad**: subclase de `Animal` (p. ej. `Bird : Animal`) que cargue su modelo y anime su rig en `OnLocomotionUpdate`. La locomoción (`AnimalLocomotion`) y el paseo (`WanderBehavior`) se reutilizan tal cual.
- **Colocación**: `AnimalSystem` puede generalizarse (p. ej. spawnear varias especies/medios) o crearse un sistema hermano; mantener el principio de no-dependencias.

- **Comportamientos**: el cerebro (`UtilityBrain`) se reutiliza; el animal puede tener su propio conjunto de comportamientos según su medio (p. ej. un ave: `FlyBehavior`/`PerchBehavior`/`HuntBehavior`).

## Utility AI (sistema de decisiones)

Inspirado en el patrón de `C:\Users\desarrollo\Documents\SpringChallenge2026` (`TrollFarmBot/AI`): cada comportamiento expone un `Score`, y un "decider" elige el de mayor puntuación. Aquí el decider es `UtilityBrain`, que es a su vez un `IAnimalBehavior` compuesto, así que `Animal` sigue corriendo un único `Behavior` (el cerebro) sin cambios en su bucle.

- **`IAnimalBehavior.Score(animal)`**: utilidad actual del comportamiento (mayor = más deseable). `ScoringUtils` (`Normalize`, `Proximity = 1/(1+dist·k)`, `Falloff`) ayuda a construir scores a partir de distancias/conteos.
- **`UtilityBrain`**: cada `EvalInterval` (0,25 s) puntúa todos los candidatos y conmuta al mejor si supera al activo por `SwitchMargin` (histéresis anti-parpadeo); cada frame ejecuta el `Tick` del activo. Llama `Enter` al cambiar.
- **Comportamientos actuales**: `WanderBehavior` (`Score` = `WanderWeight` constante, el suelo por defecto) y `FleeBehavior` (`Score` = `FleeWeight · Falloff(distCámara, FleeInner, FleeOuter)`; cerca de la cámara supera a pasear → el pez acelera (`Locomotion.SpeedScale = FleeSpeedScale`) y se aleja; lejos cae a 0 → vuelve a pasear).
- **Aceleración del nado**: los comportamientos fijan `Locomotion.SpeedScale` cada frame (1 al pasear, >1 al huir). El aleteo de la cola se acelera solo porque `Fish.OnLocomotionUpdate` lo modula con la velocidad real.
- **Cámara sin acoplar**: `FleeBehavior` la obtiene con `animal.GetViewport().GetCamera3D()` (API de Godot), no con un tipo del proyecto → portabilidad intacta.

**Añadir un comportamiento nuevo** (p. ej. `JumpBehavior`, `EatBehavior`): crear la clase `IAnimalBehavior` con su `Score`/`Enter`/`Tick`, y añadirla al array que `AnimalSystem` pasa al `UtilityBrain`. Nada más cambia.

**Al crecer**: con muchos comportamientos/features conviene externalizar los pesos en una tabla (como el `WeightTable`/`FeatureVector` del proyecto de referencia); hoy van como campos públicos de cada comportamiento.
