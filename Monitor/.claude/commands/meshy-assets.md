# meshy-assets — Assets 3D (Meshy AI) del proyecto Zappy Monitor

Skill de referencia para generar o actualizar modelos GLB con Meshy AI
**específicos de este proyecto**: rutas exactas, nombres de archivo que el
código espera y estado actual (qué existe, qué falta).

## CLI `meshy`

Para instalación, configuración de la API key y referencia completa de comandos
y flags (`generate`, `text-to-image`, `image-to-3d`, `status`, `list`...), leer:

```
C:\Users\desarrollo\tools\meshy-client\README.md
```

El ejecutable `meshy` está en el `PATH`, así que los comandos de abajo se
ejecutan directamente desde la raíz de este proyecto
(`C:\Users\desarrollo\Documents\Zappy42\Monitor`).

---

## Convención de estilo visual

Todos los GLB existentes son **low-poly, vista isométrica, fondo
blanco/transparente, con PBR**. Mantener ese estilo en los prompts nuevos,
añadiendo un sufijo similar a:

```
, low-poly, isometric, white background
```

---

## Recursos del mundo (`entities/resources/models/`)

`Resource.cs` (`entities/resources/Resource.cs:39,46`) carga automáticamente
`res://entities/resources/models/{ResourceType}.glb` si existe (escala fija
`0.15f`); si no existe, usa una esfera coloreada semitransparente como
fallback. **El nombre de archivo debe coincidir exactamente (case sensitive)**
con el valor del enum `Resource.ResourceType` (`entities/resources/Resource.cs:6-15`).

| Tipo (enum) | Archivo esperado | Estado | Color fallback |
|---|---|---|---|
| `Nourriture` | `Nourriture.glb` | ❌ pendiente | verde |
| `Linemate` | `Linemate.glb` | ✅ | gris |
| `Deraumere` | `Deraumere.glb` | ✅ | azul |
| `Sibur` | `Sibur.glb` | ✅ | naranja |
| `Mendiane` | `Mendiane.glb` | ✅ | magenta |
| `Phiras` | `Phiras.glb` | ✅ | amarillo |
| `Thystame` | `Thystame.glb` | ✅ | rojo |

Ejemplo para generar el que falta:

```cmd
meshy generate --prompt "<descripción>, low-poly, isometric, white background" --name Nourriture --output-dir entities/resources/models
```

No requiere ningún cambio de código: `Resource.cs` lo detecta automáticamente
en el siguiente reload de la escena.

---

## Equipamiento del Shaman (`entities/player/models/equipments/`)

Cada pieza se referencia desde `ShamanEquipmentConfig.cs`
(`entities/player/ShamanEquipmentConfig.cs`) como
`new EquipmentSlot(boneName, Eq + "<archivo>.glb", offsets?)`, donde
`Eq = "res://entities/player/models/equipments/"`.

`EquipmentManager.cs` (`managers/EquipmentManager.cs`) tolera GLBs faltantes
(loguea un aviso y sigue sin romper la escena), así que se puede dejar la
entrada en `ShamanEquipmentConfig.cs` antes de generar el asset.

**Huesos disponibles:** `neck`, `Head` / `headfront`, `RightHand`,
`LeftShoulder`, `RightShoulder`, `LeftForeArm`, `RightForeArm`.

| Archivo | Nivel(es) | Hueso | Estado |
|---|---|---|---|
| `collar_bone.glb` | 2, 3, 4 | `neck` | ❌ pendiente |
| `skull_mask.glb` | 3, 4, 5, 6, 7 | `Head` | ✅ |
| `staff_basic.glb` | 4, 5 | `RightHand` | ✅ |
| `collar_gem.glb` | 5, 6, 7 | `neck` | ❌ pendiente |
| `staff_orb.glb` | 6, 7 | `RightHand` | ❌ pendiente |
| `shoulder_bone.glb` | 6, 7 | `LeftShoulder` + `RightShoulder` | ❌ pendiente |
| `horns.glb` | 7 | `Head` | ❌ pendiente |

Ejemplo:

```cmd
meshy generate --prompt "<descripción>, low-poly, isometric, white background" --name collar_bone --output-dir entities/player/models/equipments
```

Tras generar el GLB:
1. Probar in-game (subir el nivel del jugador o usar `MockServer.cs`).
2. Ajustar `Offsets` (Position, RotationDeg, Scale) en
   `ShamanEquipmentConfig.cs` — usar los offsets ya definidos para
   `staff_basic.glb` y `skull_mask.glb` en el nivel 4 como referencia de
   formato (posición/rotación en grados/escala por hueso).

---

## Modelo principal del jugador (`entities/player/models/Shaman.glb`)

⚠️ **No regenerar con Meshy.** Es un esqueleto bípedo con `AnimationPlayer` y
los clips `idle`, `walking`, `running`, `spell_cast`, `collect_object`,
`pick_up_pocket` (usados por `ShamanAnimationController.cs`). El pipeline
`image-to-3d` de Meshy no genera rigs ni animaciones compatibles, por lo que
este modelo queda fuera del alcance de esta skill.

---

## Flujo recomendado

1. Buscar referencias visuales (o pedirlas al usuario) y mostrar candidatas.
2. Si la referencia es limpia (objeto único, fondo plano, sin texto) → usarla
   directamente con `meshy image-to-3d --image-path/--image-url --name <slug>
   --output-dir <ruta de la tabla correspondiente>`.
3. Si no es limpia → `meshy text-to-image --prompt "..." --name <slug>`,
   aprobar la imagen con el usuario, y luego `meshy image-to-3d`.
4. Recargar la escena en Godot y verificar visualmente: escala (recursos
   fijos a `0.15f`), orientación y, para equipamiento, los `Offsets` por hueso.
