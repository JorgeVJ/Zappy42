# Animals — Sistema de animales decorativos (peces)

Skill de referencia para el sistema de fauna decorativa del Zappy Monitor (actualmente: peces payaso nadando en el agua). Leer antes de modificar, ampliar o portar este sistema a otro proyecto.

---

## Diseño: autocontenido y portable

Este sistema se diseñó deliberadamente **independiente del resto del proyecto**, para poder copiarlo/pegarlo a otro proyecto Godot con heightmap, o eliminarlo por completo sin dejar rastro. Reglas que mantienen esa independencia:

- `AnimalSystem.cs` y `Fish.cs` **no referencian** `Terrain`, `Connection`, `TerrainSnap`, `EntityManager` ni ningún otro tipo del proyecto. Solo usan tipos de Godot (`Node3D`, `Skeleton3D`, etc.) y primitivas (`float[,] heightMap, int width, int height`).
- Todo vive dentro de `entities/animals/` — script de la entidad, script del sistema de colocación y el modelo `.glb`. No hay lógica repartida en `entities/terrain/` ni en `managers/`.
- No usa `PlacementFinder` (evita solapes entre decoraciones en tierra; no aplica a peces en agua) ni `EntityManager<T>` (eso es para entidades con ID de servidor — altas/bajas dinámicas; los peces se generan una sola vez junto con el terreno y no tienen ID).
- No depende de `Connection.ReplayInstant` ni de ningún global estático del proyecto.

## Archivos

| Archivo | Rol |
|---|---|
| `entities/animals/Fish.cs` | Entidad decorativa individual y **genérica**: recibe la ruta del `.glb` como parámetro, busca los huesos `Body`/`Tail` y los anima por código cada frame (sin `AnimationPlayer`, los modelos no traen clips). Sirve para cualquier especie con ese mismo rig. |
| `entities/animals/AnimalSystem.cs` | Sistema de colocación. Recibe el heightmap del terreno, calcula qué tiles quedan bajo el "nivel del mar" y reparte `FishCount` peces ahí al azar, eligiendo para cada uno un modelo al azar de `FishModels`. |
| `entities/animals/ClownFish.glb`, `entities/animals/SurgeonFish.glb` | Modelos con 2 huesos: `Body` y `Tail`. Sin animaciones propias. Mismo rig → intercambiables por la misma clase `Fish`. |

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

**Para portar el sistema a otro proyecto:** copiar la carpeta `entities/animals/` completa y añadir una llamada a `animalSystem.Generate(heightMap, width, height)` en cualquier punto donde ese proyecto tenga un `float[,] heightMap` con su ancho/alto — no requiere más que eso.

**Para eliminarlo por completo:** borrar `entities/animals/`, quitar el campo `_animalSystem`, la línea de `GetNodeOrNull` y la línea de `Generate(...)` en `Terrain.cs`, y quitar el nodo `AnimalSystem` + su `ext_resource` de `terrain.tscn`.

## Cómo decide dónde colocar los peces

`AnimalSystem.Generate` replica (no reutiliza) el mismo cálculo de nivel del mar que `entities/terrain/WaterSystem.cs` — `Mathf.Lerp(min, max, SeaLevelFraction)` sobre el heightmap — porque ambos sistemas son hermanos desacoplados en el `.tscn` y no deben depender uno del otro. Si se cambia `SeaLevelFraction`/`SeaLevelOffset` en `WaterSystem`, hay que replicar el cambio en `AnimalSystem` (son `[Export]` independientes, no están sincronizados automáticamente).

Pasos del algoritmo:
1. Calcula `seaY` con el heightmap.
2. Recorre todos los tiles `(x, y)` y se queda con los que tienen altura de tile (promedio de las 2 esquinas diagonales, misma fórmula que `Terrain.GetTileHeight`) por debajo de `seaY`.
3. Elige `FishCount` tiles al azar de esa lista (con repetición permitida si `FishCount` > nº de tiles de agua, se limita al tamaño de la lista) y coloca un `Fish.Create(pos, modelPath)` en el centro de cada tile elegido, a `seaY - SpawnYOffset` (un poco bajo la superficie). El `modelPath` se elige al azar de `FishModels` por cada pez → mezcla de especies.
4. Si no hay tiles de agua, no genera nada — no hay fallback.

## Parámetros `[Export]`

**`AnimalSystem`**
- `FishCount` (3–6 recomendado, por defecto 6, rango 0–20 en el inspector) — cantidad de peces a generar.
- `FishModels` — array de rutas `.glb` entre las que se elige al azar por cada pez. Para añadir una especie nueva: meter un `.glb` con huesos `Body`/`Tail` en `entities/animals/` y añadir su ruta a este array (no requiere tocar código). Si queda vacío, no se genera nada.
- `SpawnYOffset` — cuánto hunde cada pez bajo la superficie del agua.
- `SeaLevelFraction` / `SeaLevelOffset` — deben coincidir con los de `WaterSystem` si se quiere que los peces queden visualmente bajo el agua real.
- `TileSize` — debe coincidir con `Terrain.TILE_SIZE` (por defecto 2.0); no se referencia la constante del proyecto a propósito (ver "Diseño").

**`Fish`**
- `TailFrequency` / `TailAmplitudeDegrees` — velocidad y amplitud del aleteo de la cola (rotación en Y).
- `BodyFrequency` / `BodyAmplitudeDegrees` — balanceo del cuerpo, en contrafase respecto a la cola, amplitud menor.
- Cada instancia arranca con una fase aleatoria (`GD.Randf() * Mathf.Tau`) para que no naden sincronizados entre sí.

## Animación procedural de huesos

Los modelos no traen `AnimationPlayer`. `Fish._Ready()` busca el `Skeleton3D` recursivamente dentro del modelo instanciado, resuelve `FindBone("Body")` / `FindBone("Tail")` y guarda la pose de reposo (`GetBoneRest`). En `_Process`, cada frame compone una rotación sinusoidal sobre esa pose de reposo y la aplica con `SetBonePoseRotation`. Si un `.glb` cambia de nombres de hueso, actualizar los strings `"Body"`/`"Tail"` en `Fish.cs` — si `FindBone` devuelve `-1` el hueso simplemente no se anima (sin warnings, sin crash).

## Añadir una especie de pez nueva

Mientras el modelo comparta el rig de 2 huesos `Body`/`Tail`, **no hace falta tocar código**: meter el `.glb` en `entities/animals/` y añadir su ruta al array `FishModels` de `AnimalSystem` (en el inspector o en el valor por defecto del `[Export]`). La misma clase `Fish` lo anima. Las especies se mezclan al azar por posición.

## Convenciones para otros animales (no peces)

Si se añade fauna con otro rig o comportamiento (no un pez con huesos `Body`/`Tail`):
- Seguir el mismo patrón: `Node3D` simple, sin `ISelectable`/`IInventory`, factory estático `Create(...)`.
- Mantener el principio de no-dependencias: el nuevo script tampoco debe referenciar `Terrain`/`Connection`/etc.
- Si necesita un sistema de colocación distinto (p. ej. en tierra en vez de en agua), crear un sistema hermano de `AnimalSystem` en la misma carpeta, o generalizar `AnimalSystem` con un `[Export] enum Habitat` — preguntar al usuario antes de generalizar si no está claro qué conviene.
