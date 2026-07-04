# screenshot — Verificación visual con ScreenshotService

Skill de referencia para **ver el monitor renderizado** (equipamiento, orbes, glows, terreno, UI)
sin pedir capturas manuales al usuario. Usa el nodo `ScreenshotService`
(`entities/shared/ScreenshotService.cs`, instanciado en `game.tscn` bajo `Game`).

---

## Qué hace ScreenshotService

- **Auto-captura** cada `CaptureInterval` segundos (def. 2 s, `[Export]`): sobrescribe
  `res://.captures/latest.png` con el framebuffer ya renderizado (post-proceso/glow incluidos).
  Esta es la vía para inspección autónoma — el archivo siempre refleja el estado actual.
- **Tecla F12** (uso manual del usuario, requiere foco de ventana): guarda `latest.png` +
  una copia con timestamp `shot_yyyyMMdd_HHmmss.png`.
- `.captures/` está en `.gitignore` — no se commitea.
- Solo escribe en `res://` corriendo **sin empaquetar** (editor o `--path`), que es el caso
  de desarrollo de este proyecto.

---

## Flujo recomendado (bucle autónomo)

1. **Compilar primero** para detectar errores C# rápido:
   ```powershell
   dotnet build "C:\Users\desarrollo\Documents\Zappy42\Monitor\zappy.csproj" -c Debug -v minimal
   ```

2. **Lanzar el monitor** sin empaquetar (NO `--headless`, no renderiza). Con
   `UseMockServer=true` (default en `Connection.cs`) arranca standalone, sin servidor Zappy real:
   ```powershell
   $exe = "C:\Users\desarrollo\Downloads\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe"
   $proj = "C:\Users\desarrollo\Documents\Zappy42\Monitor"
   $p = Start-Process -FilePath $exe -ArgumentList @("--path", $proj) -PassThru
   ```

3. **Esperar** a que `.captures\latest.png` exista/se actualice (la primera captura tarda
   unos segundos en aparecer tras el arranque).

4. **Leer** la imagen con la herramienta Read — lee PNG directamente, sin pasos extra:
   ```
   C:\Users\desarrollo\Documents\Zappy42\Monitor\.captures\latest.png
   ```

5. **Cerrar el proceso** de Godot al terminar:
   ```powershell
   Stop-Process -Id $p.Id -Force
   ```

---

## Notas

- Si se necesita una captura puntual tras un cambio (p. ej. nuevo equipamiento), el `Timer`
  de 2 s la actualiza sola — no hace falta tocar nada del lado de Godot.
- F12 es solo para el usuario (requiere ventana enfocada); Claude debe depender de la
  auto-captura.
- Para interacción (clicar tiles, probar selección/UI), usar el MCP `computer-use` —
  cubre lo interactivo; `ScreenshotService` cubre la verificación visual del render.
