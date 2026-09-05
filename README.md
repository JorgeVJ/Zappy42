
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

---

## 👥 El Equipo de Desarrollo

| Integrante | Rol Principal | GitHub |
| --- | --- | --- |
| **Alvaro Jimenez** | **Server** (Lógica de red, reglas y tiempo) | [Perfil de GitHub](https://github.com/alvjimen) |
| **Jorge Vasquez** | **Graphic Client** (Motor Godot e Interfaz) | [Perfil de GitHub](https://github.com/JorgeVJ) |
| **David Aviles** | **Client IA** (Cerebro de la IA, Interprete de comandos y Estrategia) | [Perfil de GitHub](https://github.com/Karsp) |

---

## Architecture Decision Records

- [ADR 0002: Refactor del Utility-Based Decision System](docs/adr/0002-utility-decision-system-refactor.md)

---
