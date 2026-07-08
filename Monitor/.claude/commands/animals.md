# Animals — Sistema de animales decorativos (peces + aves)

Skill de referencia para el sistema de fauna decorativa del Zappy Monitor (peces nadando en el agua, aves —loros— que caminan por tierra y vuelan al acercarse la cámara, y zorros que se pasean por tierra alternando sus animaciones y cazan aves cercanas). Leer antes de modificar, ampliar o portar este sistema a otro proyecto.

---

## Diseño: autocontenido y portable

Este sistema se diseñó deliberadamente **independiente del resto del proyecto**, para poder copiarlo/pegarlo a otro proyecto Godot con heightmap, o eliminarlo por completo sin dejar rastro. Reglas que mantienen esa independencia:

- **Ningún** archivo de `entities/animals/` (`AnimalSystem.cs`, `Animal.cs`, `Fish.cs`, `Bird.cs`, `Fox.cs`, locomoción, comportamientos) referencia `Terrain`, `Connection`, `TerrainSnap`, `CrowdSystem`, `EntityManager` ni ningún otro tipo específico de este proyecto. Solo usan tipos de Godot (`Node3D`, `Skeleton3D`, etc.), primitivas (`float[,] heightMap, int width, int height`) y las interfaces/dominios de `spatial/` y `utility-ai/` (ver más abajo).
- `entities/animals/` depende de la carpeta hermana `spatial/` para saber **dónde puede moverse un animal** (`IAnimalDomain`, `AquaticDomain`). `spatial/` es a su vez 100% Godot-only y no depende de nada de `entities/animals/` ni del resto del proyecto — también la consume `entities/terrain/` (p. ej. `TerrainDomain` para restringir dónde nace la vegetación), así que la dependencia siempre va de los sistemas concretos hacia `spatial/`, nunca al revés.
- `entities/animals/` también depende de la carpeta hermana `utility-ai/` para el framework de decisiones (`IUtilityBehavior<TAgent>`, `UtilityBrain<TAgent>`, `ScoringUtils`). `utility-ai/` es C# puro, no referencia `Animal` ni ningún tipo del proyecto — está parametrizada con un genérico `TAgent`, así que sirve igual para animales que para cualquier otro tipo de agente (humanos, objetos animados) en este u otro proyecto.
- No usa `PlacementFinder` (evita solapes entre decoraciones en tierra; no aplica a peces en agua) ni `EntityManager<T>` (eso es para entidades con ID de servidor — altas/bajas dinámicas; los peces se generan una sola vez junto con el terreno y no tienen ID). `PlacementFinder` vive en `spatial/` junto a los dominios (misma carpeta portable), pero es una utilidad independiente que `entities/animals/` no consume.
- No depende de `Connection.ReplayInstant` ni de ningún global estático del proyecto.

## Archivos

El sistema está organizado en **capas** (pensadas para crecer a animales terrestres/aéreos y a un futuro Utility AI):

| Archivo | Capa | Rol |
|---|---|---|
| `entities/animals/AnimalSystem.cs` | Colocación | Recibe el heightmap; reparte peces sobre tiles de agua (`AquaticDomain`), aves sobre tiles de tierra (`AerialDomain`) y zorros sobre tiles de tierra (`GroundDomain`) según `FishProfile`/`BirdProfile`/`FoxProfile` (`AnimalProfile`), inyectando a cada uno su dominio, su blackboard (`AnimalContext`) y su cerebro de Utility AI. El spawn de cada tipo es independiente (si el array de modelos de un perfil está vacío, sólo se omite ese tipo). |
| `spatial/ISpatialDomain.cs` | Dominio | Interfaz mínima "**es válido este punto**": sólo `Contains(worldPos)`. Base de `IAnimalDomain`; también la implementa `spatial/TerrainDomain.cs` (consumida por `entities/terrain/DecorationSystem.cs`) para restringir dónde nace la vegetación. |
| `spatial/IAnimalDomain.cs` | Dominio | Interfaz "**dónde puede moverse** un animal": extiende `ISpatialDomain` y añade `ClampToValid`, `SampleWanderTarget` (destino en el medio propio), `SampleSurfaceTarget` (destino a ras de suelo, p. ej. aterrizaje) e `IsAtSurface(pos, threshold)` (¿tocando suelo?). El eje del diseño. |
| `spatial/HeightField.cs` | Dominio (base) | Clase base abstracta de las 4 regiones sobre heightmap (`AquaticDomain`/`AerialDomain`/`GroundDomain`/`TerrainDomain`). Posee `float[,] heightMap` + `HeightMapGrid` y **centraliza** el muestreo de altura por tile (`TryTileHeight`) y bilineal (`SampleHeight`), los límites (`InBounds`/`ClampXZ`) y el muestreo de destinos en anillo (`SampleRing(from, radius, rng, TrySelect)`, con el `const SampleAttempts`). Antes estaba duplicado byte a byte en cada dominio. Godot-only, portable. |
| `spatial/AquaticDomain.cs` | Dominio | Implementación acuática (`: HeightField`): volumen de agua entre el fondo (+margen) y la superficie del mar (−margen). Aporta `Contains`/`ClampToValid`/`SampleWanderTarget` y `IsWaterColumn`; el muestreo de altura lo hereda de `HeightField`. Usa el struct `spatial/NavigableMargins.cs`. |
| `spatial/AerialDomain.cs` | Dominio | Implementación aérea del ave (`: HeightField`): **volumen único suelo↔techo** sobre todo el mapa. `SampleWanderTarget` da destinos aéreos sobre tierra o agua; `SampleSurfaceTarget` da destinos a ras de suelo **sólo sobre tierra** (aterrizaje); `IsAtSurface` detecta el toque de suelo; `FloorHeight`/`IsLandColumn` públicos. `ClampToValid` no teletransporta (suelo = superficie), así despegue/aterrizaje son planeos. Usa el struct `spatial/AerialBounds.cs`. |
| `spatial/GroundDomain.cs` | Dominio | Implementación **terrestre pura** del zorro (`: HeightField`): solo columnas de **tierra** (altura ≥ nivel del mar + orilla), sin volumen aéreo. `SampleWanderTarget`/`SampleSurfaceTarget` dan destinos a ras de suelo sobre tierra y `ClampToValid` **fija `Y = FloorHeight(x,z)`** cada paso → el animal queda pegado al suelo en pendientes sin snapping extra. Ctor `(heightMap, grid, seaY, shoreMargin)`; no necesita struct de bounds. |
| `entities/animals/AnimalLocomotion.cs` | Locomoción | Steering procedural genérico (estilo `CrowdSystem`): mueve un `Node3D` hacia un objetivo con aceleración/frenado suaves y giro gradual hacia el rumbo. |
| `utility-ai/IUtilityBehavior.cs` | Comportamiento | Interfaz genérica `IUtilityBehavior<TAgent>` (`Enter`/`Tick`/`Score`). `Score` es la utilidad para el cerebro. Portable: no referencia `Animal` ni ningún tipo del proyecto. |
| `entities/animals/AmbientLocomotionBehavior.cs` | Comportamiento | Paseo ambiental (el baseline de las 3 especies): cicla entre **gaits** (`{ State, SpeedScale, Moves, DwellMin/Max }`) con dwell aleatorio — un FSM diminuto y explícito. En cada gait fija la velocidad, pide la animación por `IAnimated.PlayState(State)` y, si se mueve, elige destino (`SampleWanderTarget` o, con `UseSurface`, `SampleSurfaceTarget`). `Score` constante (`Weight`). **Sustituye** a los antiguos `WanderBehavior`/`WalkBehavior`/`FoxStateBehavior` (peces = swim/rest, aves = walk, zorro = idle/walk/run). |
| `entities/animals/FleeBehavior.cs` | Comportamiento | Huir de la cámara (peces): `IUtilityBehavior<Animal>` cuyo `Score` sube al acercarse la cámara; al activarse, acelera el nado y elige destinos alejándose. Lee la cámara **cacheada en el blackboard** (`AnimalContext`), no la consulta por su cuenta. |
| `entities/animals/FlyBehavior.cs` | Comportamiento | Volar (aves): `IUtilityBehavior<Animal>` como **máquina de estados** (Crucero → Aterrizaje → toque de suelo). Despega al acercarse la cámara; tras despegar sigue volando un **tiempo mínimo (dwell)** aunque la cámara se aleje, y solo entonces **desciende planeando** hacia un punto de tierra; al tocar suelo (`Domain.IsAtSurface`) pasa a caminar y su `Score` cae para ceder al paseo. Agnóstico de especie: usa `Domain` + `IAnimated` (estados "fly"/"walk"), no castea a `Bird`. |
| `entities/animals/HuntBehavior.cs` | Comportamiento | Caza **genérica** (cualquier depredador `IAnimated`): máquina de estados (Acecho → Ataque → Recuperación). La presa la detecta el blackboard (`AnimalContext.NearestPrey`) por **grupo de Godot** (`PreyGroup`), sin referenciar `Bird`; anima por `IAnimated` (`PlayState("hunt")`, `PlayAction("attack")`, `ActionFinished`). Su `Score` (`HuntWeight · Falloff(dist, 0, PreyDetectRange)`) supera al paseo cuando hay presa cerca; al terminar el golpe **captura** (`QueueFree`) y hace un idle breve. Reemplaza a `FoxHuntBehavior`. |
| `utility-ai/ScoringUtils.cs` | Utility AI | Curvas de respuesta (`Normalize`, `Proximity`, `Falloff`) para construir scores. Portable, espejo del proyecto de referencia. |
| `entities/animals/AnimalContext.cs` | Utility AI | **Blackboard** por animal: percepción cacheada 1×/frame (`CameraDistance`/`CameraPosition`/`HasCamera`, `NearestPrey` por grupo si se fija `PreyGroup`) + memoria compartida (`Target`). `Animal._Process` llama `Refresh(this)` antes del cerebro; los behaviors leen de aquí en vez de re-percibir cada uno. |
| `entities/animals/AnimalScoring.cs` | Utility AI | Helper estático: `CameraFalloff(animal, inner, outer)` (cercanía de la cámara en [0,1]) leyendo `animal.Context.CameraDistance` (ya cacheado). Lo consumen `FleeBehavior` (peces) y `FlyBehavior` (aves). Vive en `entities/animals/` para no acoplar el framework genérico a `Animal`. |
| `entities/animals/IAnimated.cs` | Capacidad | Interfaz de animación: `PlayState(string)` (bucle idle/walk/run/fly/hunt…), `PlayAction(string)` (one-shot), `ActionFinished`. Los behaviors expresan QUÉ animar sin conocer la especie; la implementan `Bird` y `Fox` (mapeando a sus clips), `Fish` no (anima por huesos). Desacopla los behaviors de los tipos concretos. |
| `entities/animals/AnimalProfile.cs` | Config | `Resource` con los parámetros de una especie (models, count, velocidades, wander radius, params de flee/fly/hunt). `AnimalSystem` tiene `FishProfile`/`BirdProfile`/`FoxProfile` (editables en inspector; si van sin asignar usa defaults en código). Data-driven: los valores viven aquí, la composición del cerebro sigue en `AnimalSystem`. |
| `utility-ai/UtilityBrain.cs` | Utility AI | `UtilityBrain<TAgent> : IUtilityBehavior<TAgent>` compuesto: puntúa los comportamientos candidatos y ejecuta el de mayor `Score`, reevaluando con histéresis. Animal lo usa instanciado como `UtilityBrain<Animal>`. |
| `entities/animals/Animal.cs` | Entidad base | `Node3D` genérico que reúne dominio + locomoción + comportamiento (el cerebro) y los ejecuta cada frame; hook `OnLocomotionUpdate(speed)` para animación. Aporta a las subclases `ModelPath` (lo fija el factory `Create`), `LoadModel()` (instancia el `.glb` y lo añade como hijo) y `FindInDescendants<T>(node)` (búsqueda recursiva genérica de `Skeleton3D`/`AnimationPlayer`). |
| `entities/animals/ClipAnimal.cs` | Entidad base | `ClipAnimal : Animal`, base de los animales animados por **clips** (Bird/Fox). Centraliza la carga del modelo + resolución del `AnimationPlayer` (`LoadModelAndPlayer`, expone `Model` y `Player`) y `PlayClip(clip, loop = true, blend = 0f)` (bucle/one-shot con fundido). Antes estaba duplicado entre `Bird` y `Fox`. Los animales por huesos (Fish) no la usan. |
| `entities/animals/Fish.cs` | Entidad | `Fish : Animal`. Carga el `.glb` (vía `LoadModel()`), resuelve el `Skeleton3D` (`FindInDescendants`), anima los huesos `Body`/`Tail` por código y modula el aleteo con la velocidad. Sirve para cualquier especie con ese rig. |
| `entities/animals/Bird.cs` | Entidad | `Bird : ClipAnimal, IAnimated`. Reproduce en bucle los clips del `AnimationPlayer` (`Parrot_Walk`/`Parrot_Fly`, `[Export]`), pega el ave al suelo al caminar (`GroundSnapThreshold`) y, al volar, **inclina el modelo (bank) hacia el interior de la curva** proporcional a la velocidad de giro del rumbo. `IAnimated.PlayState("fly"/"walk")` mapea a `SetFlying`. Tiene el dominio `Aerial` tipado. |
| `entities/animals/Fox.cs` | Entidad | `Fox : ClipAnimal, IAnimated`. Reproduce los clips vía `Play(Animations)` (bucle) y `PlayOnce(Animations)` (one-shot, p. ej. `Attack`), resolviendo `"Fox_"+valor`; `IAnimated.PlayState/PlayAction` resuelven el string al enum (`Enum.TryParse`) y `ActionFinished` expone el fin del golpe. Sin banking ni vuelo; el pegado al suelo lo da `GroundDomain`. Su cerebro **sí reacciona** a las aves (caza, ver `HuntBehavior`), no a la cámara. |
| `entities/animals/Fox.Animations.cs` | Entidad | Archivo parcial de `Fox` que **solo** define el `enum Animations { Idle, Walk, Run, Attack, Hunt }` anidado en la clase. Sus nombres coinciden con el sufijo de los clips del modelo (`Fox_Idle`/`Fox_Walk`/`Fox_Run`/`Fox_Attack`/`Fox_Hunt`). |
| `entities/animals/ClownFish.glb`, `entities/animals/SurgeonFish.glb` | Asset | Modelos con 2 huesos `Body`/`Tail`, sin animaciones. Mismo rig → intercambiables por la misma clase `Fish`. |
| `entities/animals/Bird.glb` | Asset | Loro con `AnimationPlayer` y clips (incl. `Parrot_Walk`/`Parrot_Fly`). Cualquier `.glb` con esos clips sirve para la clase `Bird`. |
| `entities/animals/Fox.glb` | Asset | Zorro con `AnimationPlayer` y clips `Fox_Idle`/`Fox_Walk`/`Fox_Run`/`Fox_Attack`/`Fox_Hunt`. Cualquier `.glb` con esos clips (o ajustando el enum/prefijo) sirve para la clase `Fox`. |

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

**Para portar el sistema a otro proyecto:** copiar las carpetas `entities/animals/`, `spatial/` (aporta `IAnimalDomain`/`AquaticDomain`, sin los cuales `entities/animals/` no compila) **y** `utility-ai/` (aporta `IUtilityBehavior<TAgent>`/`UtilityBrain<TAgent>`/`ScoringUtils`, el framework de decisiones que usan los behaviors) y añadir una llamada a `animalSystem.Generate(heightMap, width, height)` en cualquier punto donde ese proyecto tenga un `float[,] heightMap` con su ancho/alto — no requiere más que eso. Si el proyecto destino ya tiene su propia copia de `spatial/` y/o `utility-ai/` (p. ej. porque también se portó `TerrainDomain`/`PlacementFinder`, o porque ya usa el framework de Utility AI para otro tipo de agente), basta con copiar las que falten.

**Para eliminarlo por completo:** borrar `entities/animals/`, quitar el campo `_animalSystem`, la línea de `GetNodeOrNull` y la línea de `Generate(...)` en `Terrain.cs`, y quitar el nodo `AnimalSystem` + su `ext_resource` de `terrain.tscn`. `spatial/` se puede conservar si algún otro sistema (p. ej. `DecorationSystem`) sigue usando `TerrainDomain`/`PlacementFinder`; `utility-ai/` se puede conservar igual si algo más lo consume (es un módulo genérico, no específico de animales).

## Cómo decide dónde colocar los peces

`AnimalSystem.Generate` replica (no reutiliza) el mismo cálculo de nivel del mar que `entities/terrain/WaterSystem.cs` — `Mathf.Lerp(min, max, SeaLevelFraction)` sobre el heightmap — porque ambos sistemas son hermanos desacoplados en el `.tscn` y no deben depender uno del otro. Si se cambia `SeaLevelFraction`/`SeaLevelOffset` en `WaterSystem`, hay que replicar el cambio en `AnimalSystem` (son `[Export]` independientes, no están sincronizados automáticamente).

Pasos del algoritmo:
1. Calcula `seaY` con el heightmap.
2. Recorre todos los tiles `(x, y)` y se queda con los que tienen altura de tile (promedio de las 2 esquinas diagonales, misma fórmula que `Terrain.GetTileHeight`) por debajo de `seaY`.
3. Construye un `AquaticDomain` compartido (el volumen navegable) desde el heightmap + `seaY` + márgenes.
4. Elige `FishProfile.Count` tiles al azar de esa lista y coloca un `Fish.Create(pos, modelPath)` en el centro de cada tile, a media columna y ajustado al volumen con `domain.ClampToValid`. A cada pez le inyecta `Domain`, `Locomotion.MaxSpeed` y un cerebro `UtilityBrain<Animal>{ AmbientLocomotionBehavior (swim/rest), FleeBehavior }`. El `modelPath` se elige al azar de `FishProfile.Models` → mezcla de especies.
5. Si no hay tiles de agua, no genera nada — no hay fallback.

## Movimiento / paseo (capas dominio · locomoción · comportamiento)

El eje del diseño es que **el animal sepa a dónde puede moverse**, vía la abstracción `IAnimalDomain`. Cada frame, `Animal._Process` ejecuta: `Behavior.Tick` (decide destino) → `Locomotion.Tick` (avanza/gira hacia él, sin salir del dominio) → `OnLocomotionUpdate(speed)` (la especie ajusta su animación).

- **Dominio (`IAnimalDomain` / `AquaticDomain`)**: responde `Contains(pos)`, `ClampToValid(pos)` y `SampleWanderTarget(from, radius, rng)`. El acuático define un **volumen 3D**: columnas de agua entre `fondo+FloorMargin` y `seaY−SurfaceMargin`. El muestreo bilineal de altura (sin `TerrainSnap`, para no acoplar) lo aporta la base común `HeightField`, compartida por los 4 dominios.
- **Locomoción (`AnimalLocomotion`)**: clase simple (no nodo) que imita el steering de `CrowdSystem` — `Velocity.Lerp(desiredVel, Damping*dt)`, frenado de llegada, `ClampToValid` cada paso, y giro suave (slerp de orientación, con pitch para subir/bajar; **no** snapping a 90° como `Player`).
- **Comportamiento (`IUtilityBehavior<Animal>` / `AmbientLocomotionBehavior`)**: el baseline pasea eligiendo destinos cercanos del dominio con pausas (gaits + dwell). Es la **costura del Utility AI**: cada animal corre un único comportamiento (el `Behavior` activo), que hoy es un `UtilityBrain<Animal>` que elige entre varios por `Score`, cada frame tras refrescar el blackboard `AnimalContext` (ver `utility-ai/IUtilityBehavior.cs`).

**No-objetivos actuales:** sin pathfinding (los saltos cortos validados por `Contains` bastan para un paseo decorativo; en aguas no convexas un tramo recto puede rozar tierra brevemente), sin separación entre individuos.

## Parámetros `[Export]`

**`AnimalSystem`** (parámetros de terreno/geometría; el tuning por especie vive en los `AnimalProfile`)
- `SeaLevelFraction` / `SeaLevelOffset` — deben coincidir con los de `WaterSystem` si se quiere que los peces queden visualmente bajo el agua real.
- `TileSize` — debe coincidir con `Terrain.TILE_SIZE` (por defecto 2.0); no se referencia la constante del proyecto a propósito (ver "Diseño").
- `FloorMargin` / `SurfaceMargin` — holgura que el pez deja respecto al fondo y a la superficie (define la altura del volumen navegable acuático).
- `ShoreMargin` — margen de orilla: sólo tiles con altura ≥ nivel del mar + este margen cuentan como tierra (dónde caminan/nacen aves y zorros).
- `FishProfile` / `BirdProfile` / `FoxProfile` — `AnimalProfile` por especie (ver abajo). Sin asignar → defaults en código.

**`AnimalProfile`** (un `Resource` por especie; cada una usa el subconjunto que le aplica)
- `Models` — rutas `.glb` entre las que cada spawn elige al azar. Para añadir una especie nueva: meter el `.glb` en `entities/animals/` y añadir su ruta aquí (no requiere código). Vacío → no se genera esa especie.
- `Count` — cantidad a generar (rango 0–20).
- `MaxSpeed` — velocidad de crucero (se inyecta en `Locomotion.MaxSpeed`).
- `WanderRadius` — radio de los saltos de paseo (se inyecta en el `AmbientLocomotionBehavior`; en aves también en `FlyBehavior`).
- Peces (huida): `FleeInner` / `FleeOuter` (distancias de cámara para huida máx/nula), `FleeSpeedScale` (aceleración al huir).
- Aves (vuelo): `MinFlyAltitude` / `CeilingAltitude` (volumen aéreo), `FlyInner` / `FlyOuter` (despegue seguro / dejar de despegar), `FlySpeedScale`, `FlyDwellMin` / `FlyDwellMax` (ventana mínima de vuelo), `LandingSpeedScale`.
- Zorro (correr/caza): `RunSpeedScale` (aceleración del estado Run), `HuntDetectRange` / `HuntAttackRange`, `HuntWeight`, `HuntSpeedScale`, `HuntRecoverTime`, `MaxPreyAltitude`.

**`Fish`**
- `TailFrequency` / `TailAmplitudeDegrees`, `BodyFrequency` / `BodyAmplitudeDegrees` — frecuencia/amplitud base del aleteo de cola y balanceo del cuerpo (en contrafase).
- `SpeedTailBoost` — cuánto acelera el aleteo con la velocidad de nado (0 = constante).
- Cada instancia arranca con fase aleatoria para no nadar sincronizada.
- El tuning de huida (`FleeInner`/`FleeOuter`/`FleeSpeedScale`) vive en `FishProfile`, no en `Fish`.

**`Bird`**
- `WalkAnimation` / `FlyAnimation` — nombres de los clips del `AnimationPlayer` a reproducir en bucle (por defecto `Parrot_Walk` / `Parrot_Fly`).
- `GroundSnapThreshold` — distancia a la superficie por debajo de la cual, caminando y sobre tierra, el ave se pega al suelo (evita flotar sin provocar un salto al iniciar el descenso sobre agua).
- Tuning del banking (inclinación en curva): `BankGain` (inclinación por rad/s de giro), `MaxBankDegrees` (tope), `BankResponse` (rapidez de convergencia). Si inclina hacia el lado contrario, invertir el signo en `UpdateBank`.

**`Fox`**
- `BlendTime` — fundido (s) entre clips en bucle, para que los huesos no salten al cambiar de pose.
- `AttackBlendTime` — fundido (s) más corto al entrar en el ataque, para que el golpe sea nítido.
- Los clips se resuelven por convención (`"Fox_"+Animations.<valor>`), no como propiedades de texto. Para usar otro rig, renombrar los valores del `enum Animations` (`Fox.Animations.cs`) o el prefijo `"Fox_"` en `Fox.Play`/`PlayOnce`.
- El tuning de la caza (`HuntDetectRange`, `HuntAttackRange`, `HuntWeight`, `HuntSpeedScale`, `HuntRecoverTime`, `MaxPreyAltitude`) vive en `FoxProfile`, no en `Fox`.

**Locomoción/comportamiento** (no son `[Export]`; defaults en código, configurables si se exponen): `AnimalLocomotion` (`MaxSpeed`, `SpeedScale`, `Damping`, `ArrivalRadius`, `TurnSpeed`, `Stop()` detiene al animal); `AmbientLocomotionBehavior` (`Gait[]` con `{ State, SpeedScale, Moves, DwellMin/Max }`, `Weight`, `WanderRadius`, `UseSurface`; ciclo de gaits con dwell aleatorio — sustituye a Wander/Walk/FoxState); `FleeBehavior` (`FleeInner/Outer`, `FleeWeight`, `FleeSpeedScale`, `FleeStep`); `FlyBehavior` (`FlyInner/Outer`, `FlyWeight`, `FlySpeedScale`, `LandingSpeedScale`, `FlyDwellMin/Max`, `LandThreshold`, `WanderRadius`; máquina Crucero→Aterrizaje→toque de suelo con dwell); `HuntBehavior` (`AttackRange`, `HuntWeight`, `HuntSpeedScale`, `RecoverTime`, `AttackMaxDuration`, `HuntState`/`AttackAction`/`IdleState`; máquina Chase→Attack→Recover, presa vía blackboard por grupo `PreyGroup`); `UtilityBrain` (`EvalInterval`, `SwitchMargin`). El blackboard `AnimalContext` (`PreyGroup`, `PreyDetectRange`, `MaxPreyAltitude`) se configura por-animal en `AnimalSystem`.

## Animación procedural de huesos (peces)

Los modelos de pez no traen `AnimationPlayer`. `Fish._Ready()` busca el `Skeleton3D` recursivamente dentro del modelo instanciado, resuelve `FindBone("Body")` / `FindBone("Tail")` y guarda la pose de reposo (`GetBoneRest`). La animación se aplica en `OnLocomotionUpdate(speed)` (llamado cada frame desde `Animal._Process`): compone una rotación sinusoidal sobre la pose de reposo y la aplica con `SetBonePoseRotation`, **modulando frecuencia y amplitud según la velocidad** de nado (aleteo suave en reposo, más vivo al crucero). Si un `.glb` cambia de nombres de hueso, actualizar los strings `"Body"`/`"Tail"` en `Fish.cs` — si `FindBone` devuelve `-1` el hueso simplemente no se anima (sin warnings, sin crash).

## Animación por clips + banking (aves)

El modelo de ave **sí trae un `AnimationPlayer` con clips**. La carga del modelo y la resolución del `AnimationPlayer` viven en la base `ClipAnimal` (`LoadModelAndPlayer`, que usa `Animal.LoadModel()` + `FindInDescendants<AnimationPlayer>`); `Bird` guarda además el `Model` heredado para el banking. `Bird` implementa `IAnimated`: `PlayState("walk"/"fly")` llama a `SetFlying`, que reproduce en bucle el clip correspondiente vía `ClipAnimal.PlayClip` (le fija `LoopMode` y lo reproduce, sólo si `HasAnimation` y no era ya el activo). Los behaviors (paseo/`FlyBehavior`) piden el estado por `IAnimated`, sin castear a `Bird`.

En `OnLocomotionUpdate(speed)`:
- **Volando** → `UpdateBank`: mide la velocidad angular del rumbo horizontal (`_prevForward.SignedAngleTo(curForward, Up) / dt`) y aplica un **roll local al modelo** (`Model.Rotation.Z`) proporcional (`BankGain`), acotado a `MaxBankDegrees` y suavizado (`BankResponse`). Curva más cerrada → más `yawRate` → más inclinación, hacia el interior de la curva. El roll va en el modelo hijo, no en la raíz (que orienta la locomoción), para no interferir con la medición del rumbo ni con el pitch de subir/bajar.
- **Caminando** → `GroundAndLevel`: nivela las alas (`_bank → 0`) y, si el ave está sobre columna de tierra y ya cerca de la superficie (`GroundSnapThreshold`), le fija `Y = FloorHeight` para dejarla pegada al suelo.

## Zorro terrestre: animación por clips vía Utility AI (paseo por gaits + caza)

El zorro (`Fox`) es la referencia de un **animal terrestre puro**: se pasea por tierra con un dominio de suelo (`GroundDomain`, que lo mantiene pegado a la superficie vía `ClampToValid`). No reacciona a la cámara (a diferencia de peces/aves), pero **sí caza aves cercanas**. Es más simple que `Bird` en locomoción: sin vuelo ni banking (no tiene `OnLocomotionUpdate` propio; los clips los reproduce el `AnimationPlayer`), pero su cerebro añade una máquina de estados de caza.

- **Modelo/clips**: `Fox._Ready()` usa `ClipAnimal.LoadModelAndPlayer()` para instanciar el `.glb` y resolver su `AnimationPlayer`, fija `PlaybackDefaultBlendTime = BlendTime`, se suscribe a `AnimationFinished` (para saber cuándo acaba el ataque one-shot) y **no** añade un `Play` extra tras `base._Ready()`: el primer clip lo dispara el `Enter` del comportamiento que elige el cerebro. `Fox` implementa `IAnimated`: `PlayState/PlayAction` resuelven el string al valor del `enum Animations` (`Enum.TryParse`) y llaman a `Play`/`PlayOnce` (`ClipAnimal.PlayClip`, clip `"Fox_"+valor`); `ActionFinished` expone el fin del golpe.
- **Enum en archivo aparte**: `enum Animations { Idle, Walk, Run, Attack, Hunt }` vive **anidado** en `Fox` pero en su propio archivo parcial `Fox.Animations.cs`. Los nombres de sus valores **son** los sufijos de los clips (`Fox_Idle`…`Fox_Hunt`), así no hace falta una propiedad de texto por animación.
- **Cerebro (paseo + caza)**: `AnimalSystem.BuildFoxBrain()` arma un `UtilityBrain<Animal>{ AmbientLocomotionBehavior, HuntBehavior }`. El `AmbientLocomotionBehavior` cicla tres gaits (Idle `Moves=false`; Walk; Run `SpeedScale=RunSpeedScale`) con **dwell aleatorio** — un FSM ambiental honesto, no un oscilador de `Score`. El `HuntBehavior` tiene `Score` mayor (`HuntWeight ≈ 4`) cuando el blackboard detecta un ave cazable cerca, así que **gana el cerebro** y toma el control (acecho → ataque → captura → recuperación) antes de devolverlo al paseo.

## Caza del zorro (Utility AI dirigido por presas)

El zorro caza a las aves mediante `HuntBehavior` (genérico, cualquier depredador `IAnimated`), una máquina de estados interna (`Chase → Attack → Recover`):
- **Descubrimiento desacoplado**: las presas las detecta el **blackboard** (`AnimalContext.NearestPrey`) por **grupo de Godot** (`HuntBehavior.PreyGroup`, configurado en `fox.Context.PreyGroup`), al que `AnimalSystem` añade cada ave al crearla (`bird.AddToGroup(...)`). El depredador **no referencia el tipo `Bird`** → portabilidad intacta. El escaneo descarta las presas por encima de `Context.MaxPreyAltitude` y ocurre **1×/frame** (no por-behavior).
- **Score**: `HuntWeight · Falloff(distHorizontal, 0, Context.PreyDetectRange)` en fase `Chase`; una vez enganchado (Attack/Recover) devuelve `HuntWeight` constante para no soltar el control a mitad. Supera el baseline del paseo (≈1) cuando hay presa cerca.
- **Ataque**: a `AttackRange` lanza la acción `PlayAction("attack")`; espera a `ActionFinished` (o al timeout `AttackMaxDuration`), **captura** la presa (`QueueFree`) y pasa a `Recover` (idle breve, `RecoverTime`) antes de volver a `Chase` y ceder el cerebro al paseo.
- Tuning en `FoxProfile`: `HuntDetectRange`, `HuntAttackRange`, `HuntWeight`, `HuntSpeedScale`, `HuntRecoverTime`, `MaxPreyAltitude`.

## Añadir una especie de pez nueva

Mientras el modelo comparta el rig de 2 huesos `Body`/`Tail`, **no hace falta tocar código**: meter el `.glb` en `entities/animals/` y añadir su ruta al array `Models` del `FishProfile`. La misma clase `Fish` lo anima. Las especies se mezclan al azar por posición.

## Añadir una especie de ave nueva

Mientras el modelo comparta los clips `Parrot_Walk`/`Parrot_Fly` (o se ajusten los `[Export]` `WalkAnimation`/`FlyAnimation` de `Bird`), **no hace falta tocar código**: meter el `.glb` con su `AnimationPlayer` en `entities/animals/` y añadir su ruta al array `Models` del `BirdProfile`. La misma clase `Bird` lo camina, vuela e inclina. Si `HasAnimation` no encuentra el clip, simplemente no anima (sin crash).

## Añadir una especie de zorro (terrestre) nueva

Mientras el modelo comparta los clips `Fox_Idle`/`Fox_Walk`/`Fox_Run` (los nombres del `enum Fox.Animations` con prefijo `"Fox_"`), **no hace falta tocar código**: meter el `.glb` con su `AnimationPlayer` en `entities/animals/` y añadir su ruta al array `Models` del `FoxProfile`. La misma clase `Fox` lo pasea y anima. Si el rig usa otros nombres de clip, renombrar los valores del enum en `Fox.Animations.cs` (o el prefijo en `Fox.Play`). Si `HasAnimation` no encuentra un clip, simplemente no anima (sin crash).

## Medios implementados (acuático · aéreo · terrestre) y cómo añadir otros

- El ave (`Bird`) materializa el patrón **terrestre+aéreo**: **camina sobre tierra y vuela sobre todo el mapa** usando un **dominio único suelo↔techo** (`AerialDomain`), de modo que despegue/aterrizaje son planeos de la locomoción (sin cambiar de dominio ni teletransportar). Su cerebro es `UtilityBrain<Animal>{ AmbientLocomotionBehavior (walk, baseline), FlyBehavior (sube con la cercanía de la cámara) }`.
- El zorro (`Fox`) materializa el patrón **terrestre puro**: se pasea por tierra con `GroundDomain` (que lo pega a la superficie vía `ClampToValid`); no reacciona a la cámara, pero **caza aves**. Su cerebro `UtilityBrain<Animal>{ AmbientLocomotionBehavior (idle/walk/run por gaits), HuntBehavior }` alterna estados por dwell y toma el control cuando hay una presa cerca (ver secciones "Zorro terrestre" y "Caza del zorro").

Para añadir otro medio o animal, la arquitectura sigue lista sin tocar locomoción ni cerebro:
- **Nuevo dominio**: crear otra implementación de `IAnimalDomain` completa en `spatial/` (`ClampToValid`/`SampleWanderTarget`, no basta `ISpatialDomain`). `AerialDomain` es la referencia para un medio con suelo y aire; `GroundDomain` para un medio de **suelo puro** (pegado a la superficie); `AquaticDomain` para un volumen cerrado. Nota: `spatial/TerrainDomain.cs` sólo implementa `ISpatialDomain` (validez de punto en tierra), no sirve tal cual para pasear.
- **Nueva entidad**: subclase de `Animal` que cargue su modelo y lo anime (por clips como `Bird`/`Fox` o por huesos como `Fish`). La locomoción (`AnimalLocomotion`) se reutiliza tal cual.
- **Colocación**: extender `AnimalSystem` (spawn independiente por tipo, como fish/bird) o crear un sistema hermano; mantener el principio de no-dependencias.
- **Comportamientos**: el cerebro (`UtilityBrain<Animal>`) se reutiliza; cada animal tiene su conjunto de comportamientos (p. ej. añadir un `PerchBehavior` al array de `AnimalSystem`, como el `HuntBehavior`). Para uno que reaccione a la cámara, reutilizar `AnimalScoring.CameraFalloff`; para uno dirigido a otras entidades, la percepción por **grupo de Godot** del blackboard (`AnimalContext.NearestPrey`) evita acoplar tipos. Los behaviors piden animación por `IAnimated` (sin castear a la especie).

## Utility AI (sistema de decisiones)

Inspirado en el patrón de `C:\Users\desarrollo\Documents\SpringChallenge2026` (`TrollFarmBot/AI`): cada comportamiento expone un `Score`, y un "decider" elige el de mayor puntuación. El framework en sí (`IUtilityBehavior<TAgent>`, `UtilityBrain<TAgent>`, `ScoringUtils`) vive en la carpeta portable `utility-ai/` (raíz del proyecto) y no conoce `Animal`: está parametrizado por el genérico `TAgent`, así que sirve igual para animales, humanos o cualquier otro objeto animado. Aquí el decider es `UtilityBrain<Animal>`, que es a su vez un `IUtilityBehavior<Animal>` compuesto, así que `Animal` sigue corriendo un único `Behavior` (el cerebro) sin cambios en su bucle.

- **`IUtilityBehavior<Animal>.Score(animal)`**: utilidad actual del comportamiento (mayor = más deseable). `ScoringUtils` (`Normalize`, `Proximity = 1/(1+dist·k)`, `Falloff`) ayuda a construir scores a partir de distancias/conteos.
- **`UtilityBrain<Animal>`**: cada `EvalInterval` (0,25 s) puntúa todos los candidatos y conmuta al mejor si supera al activo por `SwitchMargin` (histéresis anti-parpadeo); cada frame ejecuta el `Tick` del activo. Llama `Enter` al cambiar.
- **Comportamientos actuales** (todos baseline = `AmbientLocomotionBehavior`, `Score` constante): peces → ambient (gaits swim/rest) + `FleeBehavior` (`Score` = `FleeWeight · AnimalScoring.CameraFalloff(...)`; cerca de la cámara supera al paseo → acelera y se aleja; lejos cae a 0 → vuelve a pasear). Aves → ambient (walk) + `FlyBehavior` (puntúa por cercanía de cámara). Zorro → ambient (idle/walk/run por gaits+dwell, **sin cámara**) + `HuntBehavior` (Score dirigido a presas vía blackboard, gana cuando hay un ave cerca).
- **Aceleración del nado**: los comportamientos fijan `Locomotion.SpeedScale` cada frame (1 al pasear, >1 al huir). El aleteo de la cola se acelera solo porque `Fish.OnLocomotionUpdate` lo modula con la velocidad real.
- **Percepción sin acoplar y cacheada**: el blackboard `AnimalContext` (refrescado 1×/frame por `Animal._Process`) obtiene cámara y presas con API de Godot (`GetViewport().GetCamera3D()`, `GetNodesInGroup`), no con tipos del proyecto; los behaviors leen de él (`AnimalScoring.CameraFalloff`, `Context.NearestPrey`) sin re-percibir → portabilidad intacta y sin escaneos redundantes.

**Añadir un comportamiento nuevo** (p. ej. `JumpBehavior`, `EatBehavior`): crear una clase que implemente `IUtilityBehavior<Animal>` con su `Score`/`Enter`/`Tick`, y añadirla al array que `AnimalSystem` pasa al `UtilityBrain<Animal>`. Nada más cambia.

**Reutilizar el framework para un agente que no sea `Animal`** (p. ej. un `Human` en otro sistema del proyecto o en otro proyecto): implementar `IUtilityBehavior<Human>` en cada comportamiento e instanciar `new UtilityBrain<Human>(behaviors)`; `utility-ai/` no requiere ningún cambio.

**Al crecer**: con muchos comportamientos/features conviene externalizar los pesos en una tabla (como el `WeightTable`/`FeatureVector` del proyecto de referencia); hoy van como campos públicos de cada comportamiento.
