# work-on-trello — Ejecutar tarjetas del backlog de forma autónoma

Skill de workflow: recorre el tablero **Zappy Monitor** (ver `/trello-board` para IDs, listas,
etiquetas y reglas de movimiento — **leerla siempre primero**, es la fuente de verdad para
IDs/convenciones) y ejecuta tarjetas de **Por hacer**, cada una en su propia rama creada a
partir de **`Player-Models&Animations`**.

> El repo git real es `C:\Users\desarrollo\Documents\Zappy42` (Monitor es una subcarpeta).
> Las ramas y worktrees se crean a ese nivel.

---

## 1. Selección de tarjetas

Antes de elegir qué tarjetas trabajar:

1. Listar **Por hacer**, **En curso** y **Test** (`trello cards --list <id>`).
2. Para cada tarjeta candidata en **Por hacer**, leer su descripción completa (campo `desc`
   vía API — el CLI `cards` no lo imprime; usar `Invoke-RestMethod` a
   `/1/cards/{id}?fields=name,desc&key=...&token=...` con las credenciales de
   `C:\Users\desarrollo\tools\trello-client\.env`).
3. **Descartar** una tarjeta candidata si su sección "Archivos" se solapa de forma relevante
   con los archivos/áreas que toca una tarjeta en **En curso** o **Test** (riesgo de conflicto
   de merge grave). Ejemplos de áreas sensibles activas:
   - B5 (Test): `entities/player/Player.cs` (`PlayCollect`/`PlayDrop`), `network/Connection.cs`
     (handlers `pgt`/`pdr`), `.claude/ARCHITECTURE.md`.
   - Cualquier refactor grande (`C2`, `C6`) que reescriba `Connection.cs` por completo es alto
     riesgo si hay tarjetas activas que tocan handlers concretos — evaluar caso por caso.
4. **Saltar** tarjetas que:
   - Requieran instalar dependencias nuevas (el usuario pidió no instalar nada).
   - Requieran confirmación explícita antes de una acción destructiva/irreversible (p. ej. C5:
     borrar binarios versionados con `git rm --cached`) — preguntar al usuario primero.
   - El propio `CLAUDE.md` indique no hacerlas salvo condición concreta (p. ej. C4: el typo
     `UnHightlight` solo se corrige si se refactoriza la interfaz completa).
   - Dependan de assets/clips nuevos que no existen aún (generarlos con Meshy es una tarea
     aparte, salvo que el usuario la incluya explícitamente).
5. Con el resto, priorizar por etiqueta (`P0` > `P1` > `P2`) y por scope acotado (tarjetas con
   "Archivos" concretos y "Criterio de aceptación" claro se ejecutan mejor de forma autónoma
   que refactors transversales).

---

## 2. Workflow por tarjeta

Para cada tarjeta seleccionada:

1. **Mover a "En curso"**: `trello move-card --card <idCard> --list 6a272cc2a361c002e2bbc9bd`.
2. **Elegir nombre de rama**: `task/<CÓDIGO>-<slug-corto-en-inglés>` (p. ej.
   `task/B9-speed-sgt-init`, `task/C3-entity-manager-base`), siguiendo el patrón de ramas
   existentes (`task/B5-player-animations`, `task/D3-dynamic-positioning`).
3. **Enviar un agente** (Agent tool, `subagent_type: general-purpose`, sin `isolation`) que:
   - Cree un **worktree nuevo** a partir de `Player-Models&Animations` (no de la rama actual):
     ```powershell
     git -C "C:\Users\desarrollo\Documents\Zappy42" worktree add -b task/<CÓDIGO>-<slug> "C:\Users\desarrollo\Documents\Zappy42-worktrees\<CÓDIGO>-<slug>" Player-Models&Animations
     ```
   - Implemente el cambio descrito en la tarjeta (sección "Qué hacer") dentro de
     `Monitor/` en ese worktree, respetando `CLAUDE.md` (convenciones C#, no GDScript, etc.)
     y, si aplica, actualice `.claude/ARCHITECTURE.md` (solo las secciones afectadas).
   - **No haga `git commit` ni `git push`** — deja el working tree con los cambios sin
     commitear. Tampoco instale paquetes/dependencias nuevas.
   - Devuelva un resumen: archivos tocados, criterio de aceptación cumplido, cómo se validó
     (build/lectura de código — sin servidor real, usar `MockServer.cs` si aplica).
4. **Revisar el diff** del worktree (`git -C <worktree> diff`) antes de comitear.
5. **Commit local** (yo, la sesión principal, no el agente) en esa rama/worktree:
   ```powershell
   git -C "C:\Users\desarrollo\Documents\Zappy42-worktrees\<CÓDIGO>-<slug>" add -A
   git -C "C:\Users\desarrollo\Documents\Zappy42-worktrees\<CÓDIGO>-<slug>" commit -m "<mensaje>"
   ```
   No hacer `push`. Mensaje de commit: `<CÓDIGO>: <resumen breve>` siguiendo el estilo de los
   commits existentes (p. ej. `B5: Disparar animaciones de recoger/soltar recursos (pgt/pdr)`).
6. **Cerrar el worktree** (la rama y el commit quedan intactos, solo se elimina el directorio
   de trabajo):
   Hacerlo **siempre** tras el commit y **antes** de mover la tarjeta a "Test" — no dejar
   worktrees activos al terminar una tarjeta.
7. **Mover la tarjeta a "Test"**: `trello move-card --card <idCard> --list 6a27310e546b0e82ee1d092b`.
   Si el "Qué hacer" lo justifica, actualizar la descripción (`update-card --desc`) con notas de
   implementación, pero sin reescribir el formato existente.

---

## 3. Reglas generales

- **Nunca** mover tarjetas a "Hecho" — eso lo hace el usuario tras revisar.
- **Nunca** hacer `git push`, `git rebase`, ni tocar la rama actual del usuario
  (`task/B5-player-animations` u otra que tenga checkeada).
- **No instalar** paquetes, herramientas ni dependencias (ni `nuget`, ni `dotnet add package`,
  ni similares). Si una tarjeta lo requiere, saltarla y avisar al usuario.
- Cada tarjeta vive en su propio worktree/rama — así varias tarjetas pueden trabajarse en
  paralelo (varios agentes) sin pisarse entre sí ni con la rama activa del usuario.
- Si una tarjeta resulta más grande de lo esperado a mitad de implementación (scope creep) o
  el agente detecta que sí choca con una tarjeta en Test/En curso, pausar, dejar la tarjeta en
  "En curso" y reportar al usuario en vez de forzar un commit a medias.
- Al terminar la sesión, resumir al usuario: tarjetas movidas a Test, ramas/worktrees creados
  y rutas, y qué tarjetas quedaron descartadas (con motivo) para que decida si seguir.

---

## 4. Referencia rápida de IDs (duplicado de `/trello-board` para conveniencia)

| Lista | idList |
|---|---|
| Por hacer | `6a272cc2848c9f5bab3ab986` |
| En curso | `6a272cc2a361c002e2bbc9bd` |
| Test | `6a27310e546b0e82ee1d092b` |
| Hecho | `6a272cc3153a0d242d644652` |

Para credenciales/desc de tarjetas vía API REST, usar `.env` de
`C:\Users\desarrollo\tools\trello-client\.env` (`TRELLO_KEY`, `TRELLO_TOKEN`).
