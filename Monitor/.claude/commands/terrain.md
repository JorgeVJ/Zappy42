# Terrain — Contexto completo del sistema de terreno

Skill de referencia para cualquier modificación del terreno en el Zappy Monitor. Lee este archivo antes de tocar cualquier cosa relacionada con el mundo 3D, coordenadas o recursos sobre el mapa.

---

## Archivos críticos

| Archivo | Rol |
|---|---|
| `Terrain.cs` | Generación del mesh, posicionamiento de recursos, constante TILE_SIZE |
| `terrain.tscn` | Escena con ShaderMaterial — contiene `shader_parameter/tile_size` |
| `terrain.gdshader` | Shader que dibuja líneas de grid usando `fract(world_pos / tile_size)` |
| `Connection.cs` | Usa `Terrain.TILE_SIZE` en `pnw` y `enw` para posicionar jugadores/huevos |
| `Player.cs` | Usa `Terrain.TILE_SIZE` en `SetTilePos()` para mover jugadores |
| `Resource.cs` | Instanciada por `Terrain.UpdateTileResources()` al cambiar inventario de tile |

---

## Constante TILE_SIZE

```csharp
// Terrain.cs
public const float TILE_SIZE = 2.0f;
```

**Esta constante controla TODA la escala del mundo.** Al cambiarla:

1. El mesh se regenera correctamente (ya la usa en `GenerateTerrainMesh`)
2. El shader se sincroniza automáticamente en runtime (línea en `GenerateTerrainMesh`):
   ```csharp
   if (terrainMesh.GetActiveMaterial(0) is ShaderMaterial mat)
       mat.SetShaderParameter("tile_size", TILE_SIZE);
   ```
3. **⚠️ `terrain.tscn` tiene `shader_parameter/tile_size` hardcodeado** — actualizarlo manualmente si cambia TILE_SIZE:
   ```
   shader_parameter/tile_size = 10.0   ← debe coincidir con TILE_SIZE
   ```
4. `Connection.cs` y `Player.cs` acceden a `Terrain.TILE_SIZE` directamente → se actualizan solos

### Posicionamiento de entidades: SIEMPRE en el centro del tile

```csharp
// Patrón correcto para cualquier entidad en tile (x, y):
float worldX = x * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f;
float worldZ = y * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f;
```

Esto aplica a: jugadores (`pnw`, `SetTilePos`), huevos (`enw`), recursos (`UpdateTileResources`).

---

## Generación del Heightmap

```csharp
// GenerateHeightMap() en Terrain.cs
heightMap = new float[Width + 1, Height + 1];  // ← una esquina más que tiles
noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
noise.Frequency = NoiseScale;  // [Export] default 0.08f
heightMap[x, y] = noise.GetNoise2D(x, y) * HeightScale;  // [Export] default 3f
```

**Rango de alturas:** `-HeightScale` a `+HeightScale` (con HeightScale=3 → -3 a +3 unidades)

---

## Estructura del Mesh (por tile)

Cada tile (x, y) genera 4 vértices y 2 triángulos:

```
v0 (x,   h[x,y],   z  )    v1 (x+1, h[x+1,y],   z  )
v2 (x,   h[x,y+1], z+1)    v3 (x+1, h[x+1,y+1], z+1)

Triángulo 1: v0 → v1 → v2   (esquina superior-izquierda)
Triángulo 2: v1 → v3 → v2   (esquina inferior-derecha)
```

(coordenadas en unidades de TILE_SIZE: `x * TILE_SIZE`, etc.)

**El centro del tile (0.5, 0.5 normalizado) cae en el Triángulo 2 (v1, v3, v2).**

Cálculo baricéntrico del centro en T2:
- α(v1) = 0.5, β(v3) = 0, γ(v2) = 0.5

→ Altura correcta del centro:
```csharp
float h = (heightMap[x + 1, y] + heightMap[x, y + 1]) / 2f;
// ⚠️ NO usar promedio de 4 esquinas: daría error de hasta HeightScale unidades
```

---

## UpdateTileResources — Recursos sobre el mapa

Se llama desde el evento `Tile.Inventory.Changed`, que dispara cada vez que `Connection.cs` procesa un mensaje `bct`.

```csharp
private void UpdateTileResources(int x, int y)
{
    // 1. Liberar nodos anteriores
    foreach (var r in tileResources[(x, y)]) r.QueueFree();
    tileResources[(x, y)].Clear();

    // 2. Altura del tile (fórmula correcta para T2)
    float h = (heightMap[x + 1, y] + heightMap[x, y + 1]) / 2f;
    Vector3 center = new Vector3(x * TILE_SIZE + TILE_SIZE / 2f, h, y * TILE_SIZE + TILE_SIZE / 2f);

    // 3. Un nodo Resource por tipo con cantidad > 0, con offset pseudoaleatorio dentro del tile
    foreach (var kvp in tiles[x, y].Inventory.AllOrdered)
    {
        if (kvp.Value <= 0) continue;
        var offset = GetResourceOffset(x, y, kvp.Key);
        var resource = resourceScene.Instantiate<Resource>();
        resource.Position = center + new Vector3(offset.X, 0.05f, offset.Y);
        AddChild(resource);
        resource.SetResourceType(kvp.Key);
        tileResources[(x, y)].Add(resource);
    }
}
```

**Offset pseudoaleatorio dentro del tile** (C9): sembrado por `(x, y, tipo)` mediante `RandomNumberGenerator`, en el rango `±ResourcePlacementRange` (0.7, en unidades de mundo). Al ser determinista por semilla, no cambia entre actualizaciones de inventario del mismo tile (sin parpadeos), pero sí varía entre tiles y tipos de recurso:
```csharp
private static Vector2 GetResourceOffset(int x, int y, Resource.ResourceType type)
{
    uint seed = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)((int)type * 83492791);
    var rng = new RandomNumberGenerator();
    rng.Seed = seed;
    return new Vector2(
        rng.RandfRange(-ResourcePlacementRange, ResourcePlacementRange),
        rng.RandfRange(-ResourcePlacementRange, ResourcePlacementRange)
    );
}
```

---

## GetTileFromPosition — Raycasting a tiles

```csharp
public Tile GetTileFromPosition(Vector3 pos)
{
    int x = Mathf.FloorToInt(pos.X / TILE_SIZE);  // ← DIVIDE por TILE_SIZE
    int y = Mathf.FloorToInt(pos.Z / TILE_SIZE);
    return GetTile(x, y);  // retorna null si fuera de rango
}
```

---

## El Shader (terrain.gdshader)

```glsl
uniform float tile_size = 10.0;   // sincronizado desde C# en GenerateTerrainMesh
uniform float line_width = 0.05;

void fragment() {
    vec2 grid = fract(world_pos.xz / tile_size);
    float line = max(
        step(grid.x, line_width) + step(1.0 - grid.x, line_width),
        step(grid.y, line_width) + step(1.0 - grid.y, line_width)
    );
    ALBEDO = mix(ground_color.rgb, line_color.rgb, line);
}
```

Líneas de grid = bordes donde `fract(coord / tile_size) ≈ 0` o `≈ 1`.  
**`line_width` controla el grosor:** aumentar para líneas más gruesas; 0.05 es el valor actual.

### Sincronización shader ↔ C#
- **Runtime:** automática via `mat.SetShaderParameter("tile_size", TILE_SIZE)` en `GenerateTerrainMesh()`
- **Editor/Default:** `terrain.tscn` línea `shader_parameter/tile_size = 10.0` — actualizar manualmente si cambia TILE_SIZE

---

## Checklist al modificar TILE_SIZE

- [ ] Cambiar el valor en `Terrain.cs` (`public const float TILE_SIZE = X.0f`)
- [ ] Actualizar `terrain.tscn`: `shader_parameter/tile_size = X.0`
- [ ] Verificar que el MockServer usa coordenadas de tile válidas (0 a Width-1 / Height-1)
- [ ] Probar click en tile → panel de inventario muestra el tile correcto

## Checklist al modificar la generación del heightmap

- [ ] `HeightScale` y `NoiseScale` son `[Export]` → ajustables desde el inspector Godot
- [ ] Si se cambia la fórmula de altura del centro, actualizar `UpdateTileResources`
- [ ] La colisión trimesh se regenera automáticamente en `GenerateTerrainMesh()`

## Checklist al añadir nuevos tipos de entidad sobre el mapa

- [ ] Posición = `x * Terrain.TILE_SIZE + Terrain.TILE_SIZE / 2f` (centro del tile)
- [ ] Si el Y debe seguir el terreno, calcular la altura con la fórmula T2:
  `float h = (heightMap[x+1, y] + heightMap[x, y+1]) / 2f`
  (requiere que el código tenga acceso a `Terrain` o que `Terrain` exponga un método `GetTileHeight(int x, int y)`)
