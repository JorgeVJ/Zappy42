
# ⚡ Zappy: El Desafío de la Evolución Artificial

**Zappy** es un proyecto de simulación multijugador masiva donde el objetivo no es jugar, sino **crear vida inteligente**. En este ecosistema, diversas tribus de Inteligencias Artificiales (IA) compiten por el dominio del mundo de **Trantor**, un tablero toroidal (infinito en sus bordes) lleno de recursos y peligros.

---

## 🎮 El Juego: Supervivencia en Trantor

Imagina un mundo donde tus únicos sentidos son una visión parcial y un oído que no sabe quién habla. Ese es el reto de nuestras IA.

### 🗺️ Geografía y Supervivencia

* **Mundo Infinito:** El mapa es una llanura sin relieve. Si un jugador sale por la derecha, reaparece por la izquierda; si sube por arriba, aparece por abajo.


* **Hambre Incesante:** Cada IA comienza con energía limitada. Deben recolectar *nourriture* (comida) constantemente; si se quedan sin ella, mueren.


* **Recursos Preciosos:** Además de comida, el mapa genera aleatoriamente 6 tipos de piedras preciosas necesarias para evolucionar: *linemate, deraumere, sibur, mendiane, phiras* y *thystame*.



### 👁️ El Reto de la Privación Sensorial

Lo que hace que **Zappy** sea un desafío de programación extremo es lo poco que saben las IA sobre su entorno:

* **Visión Limitada:** Una IA de nivel 1 solo ve su casilla y las 3 casillas frente a ella. Solo al evolucionar su campo de visión se expande.


* **Identidad Desconocida:** Cuando una IA ve a otra, no sabe si es un aliado o un enemigo. Todos los "Trantorianos" se ven iguales.


* **Gritos en la Oscuridad:** Las IA pueden emitir mensajes (*broadcast*) a todo el mapa. Sin embargo, el receptor solo recibe el mensaje y una dirección del 1 al 8 (según de dónde venga el sonido), pero **nunca sabe quién lo envió**.



### 🏆 El Objetivo: La Elevación

Para ganar, un equipo debe lograr que **6 de sus integrantes alcancen el nivel máximo (Nivel 8)**.
Subir de nivel requiere un **Ritual de Elevación**:

1. Reunir una cantidad exacta de piedras preciosas en una casilla.


2. Tener a un número específico de jugadores de su mismo nivel en esa misma casilla trabajando juntos.


3. **El problema:** ¿Cómo coordinas a 6 jugadores para que se encuentren en el mismo punto del mapa si no saben dónde están ni quién es quién? Aquí es donde brilla el código de comunicación que hemos diseñado.

---

## 🛠️ Arquitectura y Desarrollo

Esta sección detalla cómo hemos construido el cerebro y el cuerpo de este proyecto utilizando **C++** y **Godot**.

### El Servidor (C++)

Es el juez supremo del juego. Gestiona las conexiones TCP, el paso del tiempo y valida que nadie rompa las reglas de Trantor. Está diseñado para ser extremadamente eficiente, manejando múltiples IA simultáneamente sin bloqueos.

### El Cliente IA (C++ - Desarrollo In-house)

Nuestras IA no son servicios externos; son algoritmos diseñados desde cero por nosotros para tomar decisiones complejas bajo incertidumbre.

* **Patrón Blackboard:** Actúa como una memoria centralizada donde la IA anota sus objetivos, hambre y descubrimientos para decidir su siguiente paso.
* **Grafos:** Utilizados para la navegación y optimización de rutas hacia los recursos detectados.
* **Singleton:** Para la gestión única de recursos y estados críticos del cliente.

### El Visualizador Gráfico (Godot)

Para que los humanos podamos disfrutar de la competencia, hemos desarrollado un cliente gráfico en **Godot**. Este se conecta al servidor y traduce los datos técnicos en una representación visual fluida donde se pueden ver los rituales, los movimientos y las muertes de los personajes en tiempo real.

---

## 👥 El Equipo de Desarrollo

| Integrante | Rol Principal | GitHub |
| --- | --- | --- |
| **Alvaro Jimenez** | **Server** (Lógica de red, reglas y tiempo) | [Perfil de GitHub](https://github.com/alvjimen) |
| **Jorge Vasquez** | **Graphic Client** (Motor Godot e Interfaz) | [Perfil de GitHub](https://github.com/JorgeVJ) |
| **David Aviles** | **Client IA** (Cerebro de la IA, Interprete de comandos y Estrategia) | [Perfil de GitHub](https://github.com/Karsp) |

---
