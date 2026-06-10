# trello-board — Tablero Trello del proyecto Zappy Monitor

Skill de referencia para saber **qué tablero usar** y **cómo están organizadas las tarjetas**
en este proyecto. Para los comandos del CLI (`trello ...`), ver la skill `/trello` (en
`C:\Users\desarrollo\tools\trello-client\.claude\commands\trello.md`). Esta skill solo documenta
los IDs y convenciones específicas de **Zappy Monitor**.

---

## Tablero

- **Nombre:** Zappy Monitor
- **URL:** https://trello.com/b/RZNoSgyL
- **idBoard:** `6a272cc185a149fa902cf09d`

### Listas

| Lista | idList |
|---|---|
| Por hacer | `6a272cc2848c9f5bab3ab986` |
| En curso | `6a272cc2a361c002e2bbc9bd` |
| Test | `6a27310e546b0e82ee1d092b` |
| Hecho | `6a272cc3153a0d242d644652` |

---

## Código de las tarjetas (nombre)

Cada tarjeta se nombra `<Código> · <título>`, donde `<Código>` es **categoría + número
secuencial dentro de esa categoría** (p. ej. `C9`, `B2`, `D5`). El número solo identifica el
orden de aparición en el `TASKS.md` original / orden de creación; al añadir una tarea nueva,
usar el siguiente número libre de su categoría.

| Categoría | Significado | Label asociada |
|---|---|---|
| `A` | Requisito del subject (visualización, protocolo, etc.) | `A — Subject` (blue) |
| `B` | Bug o feature incompleta | `B — Bugs/Incompletos` (purple) |
| `C` | Arquitectura / deuda técnica / refactor | `C — Arquitectura` (green) |
| `D` | Mejora visual / extra (animaciones, decorado...) | sin label de categoría — solo prioridad |

Además de la categoría, cada tarjeta lleva (cuando aplica) una etiqueta de **prioridad**.

---

## Etiquetas existentes en el tablero

| idLabel | Nombre | Color | Clave lógica (`--labels`) |
|---|---|---|---|
| `6a272cc34911d7a1eec3ca81` | A — Subject | blue | `A` |
| `6a272cc4f665156b5becd408` | B — Bugs/Incompletos | purple | `B` |
| `6a272cc43eb81f59c2ee118b` | C — Arquitectura | green | `C` |
| `6a272cc513c2b2cafbef907a` | P0 | red | `P0` |
| `6a272cc51de2251889ab50ca` | P1 | orange | `P1` |
| `6a272cc661527a5ca8f7c3d7` | P2 | yellow | `P2` |

> Hay además 6 etiquetas sin nombre (`6a272cc185a149fa902cf0c d/e/f...`, colores blue/green/
> orange/purple/red/yellow) creadas por defecto al crear el tablero — no se usan, ignorarlas.

---

## Formato de la descripción (`--desc`)

Las tarjetas A/B/C importadas desde `TASKS.md` usan secciones en negrita markdown:

```markdown
- **Archivos:** rutas/relevantes (con líneas si aplica)
- **Contexto:** por qué existe esta tarea / qué problema hay ahora
- **Qué hacer:** acción concreta a realizar
- **Criterio de aceptación:** cómo se valida que está resuelta
```

Las tarjetas `D*` (añadidas a mano, más recientes) usan un formato libre más corto: una línea
de prioridad + contexto, párrafo de "qué hacer", dependencias, y una línea final
`Archivos: ...` con las rutas tocadas. Cualquiera de los dos formatos es válido; para tareas
de arquitectura (`A/B/C`) preferir el formato con secciones en negrita por consistencia con
las existentes.

Al cerrar una tarea, mover la tarjeta a **Hecho** (`trello move-card --card <id> --list
6a272cc3153a0d242d644652`); si queda pendiente de verificación manual, usar **Test**.
