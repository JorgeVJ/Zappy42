
# ⚡ Zappy: Un Ecosistema de Supervivencia y Evolución

**Zappy** es un proyecto de simulación multijugador masiva donde el objetivo no es jugar, sino **crear vida inteligente**. En este ecosistema, diversas tribus de Inteligencias Artificiales (IA) compiten por el dominio del mundo de **Trantor**, un tablero toroidal (infinito en sus bordes) lleno de recursos y peligros.

---

## ¿De qué trata el proyecto?

Imagina un tablero lleno de recursos donde diferentes "tribus" de personajes deben sobrevivir. Lo curioso es que **ningún humano controla a estos personajes**. Cada jugador es, en realidad, un agente de IA que se conecta a nuestro servidor y toma decisiones basadas en lo que ve y siente.

El proyecto se divide en tres pilares principales:

1. **El Servidor (C++):** El cerebro del juego. Gestiona las reglas, el paso del tiempo y la comunicación entre todos los participantes.
2. **Los Clientes (IA, C++):** Los jugadores. Deben buscar comida para no morir y recolectar minerales para evolucionar. No usamos servicios externos; cada comportamiento, desde la recolección de comida hasta la estrategia de grupo, nace de nuestro código.
3. **El Visualizador (Godot, C#):** Nuestra ventana al mundo. Una interfaz gráfica que permite a los humanos observar la competencia en tiempo real.

---

### Estado del Proyecto

Actualmente, el proyecto se encuentra en **fase de desarrollo**. Estamos puliendo la lógica de comunicación del servidor y diseñando los assets gráficos en Godot para que la experiencia sea lo más inmersiva posible.


---



## El Juego: Supervivencia en Trantor

Imagina un mundo donde tus únicos sentidos son una visión parcial y un oído que no sabe quién habla. Ese es el reto de nuestras IA.


### 📜 Las Reglas del Mundo

Para ganar en Zappy, los agentes de IA deben seguir unas reglas básicas pero estrictas:

* **Mundo Infinito:** El mapa es una llanura sin relieve. Si un jugador sale por la derecha, reaparece por la izquierda; si sube por arriba, aparece por abajo.
* **Hambre constante:** Cada agente comienza con energía limitada. Esta se va consumiendo con el tiempo, si el personaje no encuentra comida a tiempo, muere.
* **Recursos Preciosos:** Además de comida, el mapa genera aleatoriamente 6 tipos de piedras preciosas necesarias para evolucionar: *linemate, deraumere, sibur, mendiane, phiras* y *thystame*.


---


### 👁️ El Reto de la Privación Sensorial

Lo que hace que **Zappy** sea un desafío de programación extremo es lo poco que saben las IA sobre su entorno:

* **Visión limitada:** Los agentes no ven todo el mapa. Solo conocen lo que tienen delante, lo que les obliga a explorar y memorizar el entorno. Una IA de nivel 1 solo ve su casilla y las 3 casillas frente a ella. Solo al evolucionar su campo de visión se expande. 


* **Identidad Desconocida:** Cuando una IA ve a otra, no sabe si es un aliado o un enemigo. Todos los "Trantorianos" se ven iguales.


* **Gritos en la Oscuridad:** Los agentes pueden emitir mensajes (*broadcast*) a todo el mapa. Sin embargo, el receptor solo recibe el mensaje y una dirección del 1 al 8 (según de dónde venga el sonido), pero **nunca sabe quién lo envió** ni **en donde se encuentra**.

<div align="center"><img  alt="Sound Transmission Diagram" src="https://github.com/user-attachments/assets/e1769f57-bcbf-4e20-bdad-f5818d8af24d" /></div>


### 🏆 El Objetivo: La Elevación

Para ganar, un equipo debe lograr que **6 de sus integrantes alcancen el nivel máximo (Nivel 8)**.
Subir de nivel requiere un **Ritual de Elevación**:

1. Reunir una cantidad exacta de piedras preciosas en una casilla.

2. Tener a un número específico de jugadores de su mismo nivel en esa misma casilla trabajando juntos.

**El problema:** ¿Cómo coordinas a 6 jugadores para que se encuentren en el mismo punto del mapa si no saben dónde están ni quién es quién? Aquí es donde brilla el código de comunicación que hemos diseñado.

---

## 🛠️ Arquitectura y Desarrollo

Esta sección detalla cómo hemos construido el cerebro y el cuerpo de este proyecto utilizando **C++**, **C#** y **Godot**.

### El Servidor (C++)

Es el juez supremo del juego. Gestiona las conexiones TCP, el paso del tiempo y valida que nadie rompa las reglas de Trantor. Está diseñado para ser extremadamente eficiente, manejando múltiples IA simultáneamente sin bloqueos.

### El Cliente IA (C++, IA Desarrollo In-house)

Nuestras IA no son servicios externos, son algoritmos diseñados desde cero por nosotros para tomar decisiones complejas bajo incertidumbre.

* **Singleton:** Para la gestión única de recursos y estados críticos del cliente.
* **Patrón Blackboard:** Actúa como una memoria centralizada donde la IA anota sus objetivos, nivel de hambre y descubrimientos para decidir su siguiente paso.
* **Grafos:** Utilizados para la navegación y optimización de rutas hacia los recursos detectados.
* **Utility-Based Decision System (Sistema de puntuación de acciones):** Cada agente genera acciones potenciales (Bids) que son evaluadas mediante funciones de utilidad dinámicas basadas en el estado del Blackboard (hambre, exploración, recursos, etc.), seleccionando siempre la acción con mayor puntuación.

### El Visualizador Gráfico (C#, Godot)

Para que los humanos podamos disfrutar de la competencia, hemos desarrollado un cliente gráfico en **Godot** y **C#**. Este se conecta al servidor y traduce los datos técnicos en una representación visual fluida donde se pueden ver los rituales, los movimientos y las muertes de los personajes en tiempo real.

#### 🚀 Compilar el Visualizador en Linux: `make gui`

**Requisitos**

* **Linux** (el `Makefile` aborta en otros sistemas operativos; en Windows usa **WSL**).
* **Godot 4.6 .NET/mono** — la versión estándar **no** sirve, el proyecto es C#.
* `curl` o `wget`, únicamente si todavía no tienes el SDK de .NET 8.

**Compilar**

```bash
make gui
```

Esto hace dos cosas:

1. `gui-dotnet`: usa el **.NET SDK 8** del sistema si ya está instalado; si no, lo descarga e instala **sin sudo** en `Monitor/.dotnet`.
2. Importa los recursos del proyecto y compila la solución C# en modo *headless*.

Por defecto se busca el binario de Godot en `~/godot-mono/godot`. Si lo tienes en otra ruta, pásala por variable:

```bash
make gui GODOT=/ruta/a/godot-mono
```

**Lanzar la GUI** (una vez compilada)

```bash
~/godot-mono/godot --path ./Monitor --mock
```

```bash
~/godot-mono/godot --path ./Monitor -h 127.0.0.1 -p 4242
```

El primer comando arranca en modo demo con el servidor simulado interno; el segundo se conecta a un servidor Zappy real (`-h` host, `-p` puerto).

**Limpiar**

```bash
make gui-fclean
```

Borra `Monitor/.godot`, `Monitor/.dotnet` y `Monitor/dotnet-install.sh`.

#### 🪟 Compilar el Visualizador en Windows (sin `make`)

El `Makefile` solo funciona en Linux, pero el proyecto Godot se compila y exporta igual desde `cmd` o PowerShell.

**Requisitos**

* **Godot 4.6 .NET/mono** para Windows (la versión estándar **no** sirve).
* **.NET SDK 8** en el PATH: `dotnet --list-sdks` debe mostrar una entrada `8.x`.
* Solo para exportar: las **export templates** de Godot 4.6 (variante *mono*).

**Ruta al binario de Godot**

Para trabajar por línea de comandos usa el binario **`_console.exe`**; el otro no engancha la consola y no verás ninguna salida.

```bat
set GODOT=C:\ruta\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64_console.exe
```

```powershell
$env:GODOT = "C:\ruta\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64_console.exe"
```

**Compilar** (equivalente a `make gui`), desde la raíz del repositorio:

```bat
"%GODOT%" --path .\Monitor --headless --import
```

```bat
"%GODOT%" --path .\Monitor --headless --build-solutions --quit
```

En PowerShell hay que invocar con el operador de llamada `&`:

```powershell
& $env:GODOT --path .\Monitor --headless --import
```

```powershell
& $env:GODOT --path .\Monitor --headless --build-solutions --quit
```

La salida compilada queda en `Monitor\.godot\mono\temp\bin\Debug\zappy.dll` (dentro de `.godot`, que está ignorado por git).

**Ejecutar**

```bat
"%GODOT%" --path .\Monitor --mock
```

```bat
"%GODOT%" --path .\Monitor -h 127.0.0.1 -p 4242
```

Para jugar puedes usar el `.exe` sin `_console`.

**Exportar el ejecutable independiente**

El preset `gfx` (Windows Desktop, x86_64) ya está definido en `Monitor/export_presets.cfg`:

```bat
"%GODOT%" --path .\Monitor --headless --export-release "gfx" gfx.exe
```

* La ruta relativa se resuelve respecto al proyecto, así que el resultado queda en `Monitor\gfx.exe` y **sobrescribe el `gfx.exe`/`gfx.pck` versionados**. Pasa una ruta absoluta si prefieres exportar a otra carpeta.
* El preset no embebe el `.pck`: hay que **distribuir `gfx.exe` junto a `gfx.pck`**.
* Si faltan las plantillas verás este error; instálalas desde el editor en *Editor → Gestionar plantillas de exportación → Descargar e instalar*:

  ```
  ERROR: Cannot export project with preset "gfx" due to configuration errors:
  No se encontró una plantilla de exportación en la ruta:
  %APPDATA%\Godot\export_templates\4.6.stable.mono\windows_release_x86_64.exe
  ```

El ejecutable exportado acepta los mismos flags:

```bat
.\Monitor\gfx.exe --mock
```

```bat
.\Monitor\gfx.exe -h 127.0.0.1 -p 4242
```

---

## 👥 El Equipo de Desarrollo

| Integrante | Rol Principal | GitHub |
| --- | --- | --- |
| **Alvaro Jimenez** | **Server** (Lógica de red, reglas y tiempo) | [Perfil de GitHub](https://github.com/alvjimen) |
| **Jorge Vasquez** | **Graphic Client** (Motor Godot e Interfaz) | [Perfil de GitHub](https://github.com/JorgeVJ) |
| **David Aviles** | **Client IA** (Cerebro de la IA, Interprete de comandos y Estrategia) | [Perfil de GitHub](https://github.com/Karsp) |

---
