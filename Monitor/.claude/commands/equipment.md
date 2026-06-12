# Equipment — Sistema de equipamiento del Shaman

Skill de referencia para el sistema de equipamiento por nivel del jugador
(Shaman). Lee este archivo antes de añadir/quitar piezas de equipo, cambiar
offsets, añadir brillos (`GlowEffect`) o tocar las orbes orbitales sobre la
cabeza.

---

## Archivos críticos

| Archivo | Rol | Genérico/Específico |
|---|---|---|
| `managers/EquipmentManager.cs` | Adjunta/limpia escenas e hijos a huesos de un `Skeleton3D`; cachea `PackedScene` | Genérico |
| `core/EquipmentSlot.cs` | Struct: `(BoneName, ScenePath, Offsets?, Children?)` — una pieza por hueso | Genérico |
| `core/EquipmentChild.cs` | Struct: `(ScenePath, Offsets?, GlowEffect?)` — modelo GLB hijo de otra pieza (ej. gema en bastón) | Genérico |
| `core/OrbitingPivot.cs` | `Node3D` que rota sobre su eje Y a velocidad constante | Genérico |
| `core/OrbSpec.cs` | Struct: `(Offsets, Color, GlowEffect)` — una orbe procedural alrededor de un `OrbitingPivot` | Genérico |
| `core/GlowOrb.cs` | `MeshInstance3D`: esfera procedural translúcida + rim + `GlowEffect` (sin GLB) | Genérico |
| `core/GlowEffect.cs` | Struct: `(Color, EnergyMultiplier)` — aplica emisión a los materiales de un `Node3D` | Genérico |
| `core/Offsets.cs` | Struct: `(Position, RotationDeg, Scale)` para cualquier attachment | Genérico |
| `entities/player/ShamanEquipmentConfig.cs` | Mapea nivel (1-7) → `List<EquipmentSlot>` + grupo de orbes orbitales | **Específico de Zappy** |
| `entities/player/Player.cs` (`ApplyEquipment()`) | Llama a `ApplyLoadout()` y `AttachOrbitingGroup()` en `_Ready()`/`SetLevel()` | Específico |
| `entities/player/models/equipments/*.glb` | Assets 3D (ver `/meshy-assets`) | Específico |

Para reusar el sistema en otro proyecto: copiar todos los archivos "Genérico"
sin cambios y crear un nuevo `XxxEquipmentConfig.cs` con los huesos/assets del
nuevo esqueleto.

---

## Flujo general

1. `Player._Ready()` y `Player.SetLevel(level)` llaman a `ApplyEquipment()`.
2. `ApplyEquipment()`:
   ```csharp
   equipmentManager.ApplyLoadout(modelNode, ShamanEquipmentConfig.GetLoadout(Level));
   equipmentManager.AttachOrbitingGroup(modelNode, "Head", ShamanEquipmentConfig.OrbitPivotOffsets, ShamanEquipmentConfig.OrbitRotationSpeedDeg, ShamanEquipmentConfig.GetOrbitingGems(Level));
   ```
3. `ApplyLoadout(owner, slots)`:
   - `ClearAll()` — libera **todos** los `BoneAttachment3D` registrados (piezas normales + grupo orbital).
   - Para cada `EquipmentSlot`, llama a `AttachToBone(owner, slot.BoneName, slot.ScenePath, slot.Offsets, slot.Children)`.
4. `AttachToBone(...)`:
   - Busca el `Skeleton3D` con `FindSkeleton3D` (recursivo).
   - Verifica que el hueso existe (`skeleton.FindBone(boneName) != -1`); si no, loguea y no rompe la escena.
   - Instancia la escena GLB (`ResolveScene`, con caché), la añade como `BoneAttachment3D.AddChild`, resetea `Transform` y aplica `Offsets` (posición/rotación en grados→rad/escala).
   - Para cada `EquipmentChild` en `slot.Children`, llama a `AttachChild(inst, child)` (gema hija del bastón, etc.).
   - Registra el `BoneAttachment3D` en `attachments[boneName]` para que `ClearAll()`/`RemoveAttachments()` lo limpien después.
5. `AttachChild(parent, child)`:
   - Instancia `child.ScenePath`, lo añade como hijo normal de `parent`, aplica `child.Offsets` (relativos al espacio local del padre, no al hueso).
   - Si `child.Glow` no es null, llama a `child.Glow.Value.ApplyTo(childInst)` (ver sección Glow más abajo).
6. `AttachOrbitingGroup(owner, boneName, pivotOffsets, rotationSpeedDeg, orbs)`:
   - Igual que `AttachToBone` pero crea un `OrbitingPivot` (rota sobre Y) en vez de instanciar una escena.
   - Para cada `OrbSpec` en `orbs`, llama a `AttachOrb(pivot, spec)`.
   - Si `orbs` es `null`/vacío, no adjunta nada (p.ej. niveles 1-3 sin orbes) y devuelve `null`.
   - También se registra en `attachments[boneName]`, así que `ApplyLoadout()`/`ClearAll()` también lo limpian en cada cambio de nivel.
7. `AttachOrb(parent, spec)`:
   - Crea un `GlowOrb { OrbColor = spec.Color, Glow = spec.Glow }`, lo añade como hijo de `parent` (el pivote) y aplica `spec.Offsets`.

---

## Structs de configuración

### `Offsets`
```csharp
new Offsets(position: Vector3, rotationDeg: Vector3, scale: Vector3)
```
`RotationDeg` se da en **grados** (se convierte a radianes internamente).
Helper: `Offsets.Rotation(x, y, z)` → posición `(0,0,0)`, escala `(1,1,1)`.

### `EquipmentSlot`
```csharp
new EquipmentSlot(boneName, scenePath, offsets? = null, children? = null)
```
Una pieza GLB anclada a un hueso del `Skeleton3D`, con hijos opcionales
(`EquipmentChild`).

### `EquipmentChild`
```csharp
new EquipmentChild(scenePath, offsets? = null, glow? = null)
```
Modelo GLB hijo de una pieza ya instanciada (offsets relativos al **espacio
local del padre**, no al hueso). Usado para las gemas del `Staff.glb`
(`GemLvl1/2/3`).

### `GlowEffect`
```csharp
new GlowEffect(color: Color, energyMultiplier: float = 1.0f)
```
`ApplyTo(root)` recorre `root` y todos sus hijos; para cada `MeshInstance3D`
duplica el material activo de cada superficie y le activa
`EmissionEnabled = true`, `Emission = color`,
`EmissionEnergyMultiplier = energyMultiplier`. Genérico — sirve para
cualquier `Node3D` (equipamiento, recursos, huevos...).

Ejemplo de uso (gema de nivel máximo, ver `Gem3Glow`):
```csharp
private static readonly GlowEffect Gem3Glow = new(new Godot.Color(0.25f, 0.85f, 1f), 2.5f);
private static readonly List<EquipmentChild> GemLvl3 = new()
{
    new(Eq + "Staff_Gem_Lvl3.glb", GemOffsets, Gem3Glow),
};
```

### `OrbSpec` + `GlowOrb` + `OrbitingPivot` — orbes brillantes sobre la cabeza

```csharp
new OrbSpec(offsets: Offsets, color: Color, glow: GlowEffect)
```
Define una orbe **sin GLB**: un `GlowOrb` (esfera `SphereMesh` radio 1,
`StandardMaterial3D` translúcido + `Unshaded` + `RimEnabled`) al que se le
aplica `glow` (mismo `GlowEffect` que el resto del proyecto) para el brillo
emisivo. `offsets.Scale` controla el tamaño final de la esfera;
`offsets.Position`/`RotationDeg` su posición/ángulo alrededor del pivote.

`OrbitingPivot` es un `Node3D` genérico que rota continuamente sobre su eje Y
(`RotationSpeedDeg` grados/segundo); cualquier hijo (las `GlowOrb`) orbita con
él como grupo.

---

## `ShamanEquipmentConfig` — configuración específica de Zappy

`Eq = "res://entities/player/models/equipments/"`. Huesos disponibles:
`Head`/`headfront`, `RightHand`, `LeftShoulder`, `RightShoulder`,
`LeftForeArm`, `RightForeArm`.

### Loadout por nivel (`GetLoadout(level)` → `Level1..Level7`)

| Nivel | Piezas |
|---|---|
| 1 | (ninguna) |
| 2 | `Staff.glb` en `RightHand` (`StaffOffsets`) |
| 3 | `skull_mask.glb` en `Head` + `Staff.glb` con `GemLvl1` |
| 4 | `skull_mask.glb` en `Head` (`SkullMaskOffsets`) + `Staff.glb` con `GemLvl1` |
| 5 | `skull_mask.glb` en `Head` + `Staff.glb` con `GemLvl2` |
| 6 | `skull_mask.glb` + `Staff.glb` con `GemLvl2` + `shoulder_bone.glb` x2 (hombros) |
| 7 | `skull_mask.glb` + `Staff.glb` con `GemLvl3` (brilla, `Gem3Glow`) + hombros + `horns.glb` en `Head` |

Las gemas del bastón (`GemLvl1/2/3`, todas con `Eq + "Staff_Gem_LvlN.glb"` +
`GemOffsets`) **reemplazan** a la anterior, no son acumulativas.

### Orbes orbitales sobre la cabeza (`GetOrbitingGems(level)` → `OrbitGems2`/`OrbitGems3`)

| Nivel | Orbes |
|---|---|
| 1-3 | `null` → `AttachOrbitingGroup` no adjunta nada |
| 4-5 | `OrbitGems2` — 2 `OrbSpec` (posiciones opuestas, `OrbColor`/`OrbGlow` arcano) |
| 6-7 | `OrbitGems3` — 3 `OrbSpec` (distribuidas en triángulo) |

- `OrbitPivotOffsets`: posición/rotación/escala del `OrbitingPivot` respecto
  al hueso `Head`.
- `OrbitRotationSpeedDeg = 60f`: una vuelta completa cada 6 s.
- `OrbColor` / `OrbGlow`: color base y `GlowEffect` compartidos por **todas**
  las orbes (look arcano único, cian/azul-púrpura). Cambiar aquí afecta a
  todas las orbes de golpe.
- Posición/ángulo/escala de cada `OrbSpec` y `OrbitPivotOffsets` son
  **placeholders** — requieren ajuste visual in-editor (subir el nivel del
  jugador o usar `MockServer.cs` y comprobar el resultado sobre la cabeza).

---

## Checklist: añadir/cambiar una pieza de equipo por nivel

- [ ] ¿El GLB existe en `entities/player/models/equipments/`? Si no, ver
      `/meshy-assets` para generarlo (`EquipmentManager` tolera GLBs
      faltantes: loguea y sigue).
- [ ] Añadir/editar la entrada `new EquipmentSlot(boneName, Eq + "archivo.glb", offsets?, children?)`
      en el `List<EquipmentSlot>` del nivel correspondiente (`Level1`..`Level7`).
- [ ] Si la pieza necesita offsets propios, crear una constante `Offsets`
      siguiendo el formato de `StaffOffsets`/`SkullMaskOffsets`.
- [ ] Si es una gema/hijo de otra pieza, usar `EquipmentChild` con offsets
      relativos al **espacio local del padre** (ver `GemOffsets`).
- [ ] Probar in-game subiendo el nivel del jugador (o `MockServer.cs`) y
      ajustar `Offsets` hasta que se vea bien.

## Checklist: añadir brillo (`GlowEffect`) a una pieza existente

- [ ] Crear una constante `GlowEffect` con el color/energía deseados (ver
      `Gem3Glow` como referencia).
- [ ] Pasarla como tercer argumento al `EquipmentChild` correspondiente
      (`new(scenePath, offsets, glow)`).
- [ ] Ajustar `EnergyMultiplier` in-editor — valores ~2-3 dan un brillo
      notable sin sobreexponer.

## Checklist: cambiar las orbes orbitales (color, nº, posición)

- [ ] Color/brillo global → editar `OrbColor`/`OrbGlow` en
      `ShamanEquipmentConfig.cs` (afecta a todas las orbes de todos los
      niveles).
- [ ] Posición/órbita/tamaño de una orbe concreta → editar el `Offsets`
      correspondiente dentro de `OrbitGems2`/`OrbitGems3`.
- [ ] Pivote (altura/posición sobre la cabeza) → `OrbitPivotOffsets`.
- [ ] Velocidad de giro → `OrbitRotationSpeedDeg` (grados/segundo).
- [ ] Cambiar el nº de orbes por nivel o los niveles en los que aparecen →
      editar `GetOrbitingGems(level)` y las listas `OrbitGems2`/`OrbitGems3`.
- [ ] No requiere ningún asset de Meshy — todo es procedural (`GlowOrb`).
