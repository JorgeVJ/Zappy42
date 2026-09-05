# ADR 0002: Refactor del Utility-Based Decision System

**Fecha:** 2026-09-05
**Estado:** Draft / Under Refactoring

## Contexto y Planteamiento del Problema

Según el `README.md`, Zappy es un ecosistema multijugador de supervivencia y evolución donde los clientes IA compiten en Trantor, un mundo toroidal con recursos limitados, visión parcial, identidad ambigua entre agentes y comunicación incompleta mediante broadcast.

El cliente IA actual se apoya en una combinación de `Singleton`, `Blackboard`, grafos de navegación y un `Utility-Based Decision System` que evalúa acciones potenciales mediante puntuaciones dinámicas. Esta base es adecuada para un comportamiento emergente, pero presenta fricción arquitectónica cuando el objetivo pasa de “tomar una buena decisión” a “coordinar muchos agentes con objetivos simultáneos”: supervivencia, exploración, obtención de recursos y elevación.

El sistema necesita cambiar porque la lógica de decisión tiende a crecer de forma acoplada al estado global, a la memoria compartida y a las prioridades del momento. Eso dificulta mantenerlo, extenderlo a nuevos tipos de agente, depurarlo bajo incertidumbre y ajustar el equilibrio entre supervivencia inmediata y progreso hacia la elevación colectiva.

## Factores de Decisión

- Mantener el comportamiento del cliente legible y modificable sin convertir el sistema en una cadena de `if/else` difícil de mantener.
- Permitir que varios agentes compartan una base común sin perder especialización por rol o contexto.
- Priorizar correctamente supervivencia, exploración, recolección y coordinación para elevación.
- Adaptarse a información incompleta: visión limitada, identidad desconocida y mensajes ambiguos.
- Controlar el coste de memoria y cómputo al evaluar muchas acciones por tick.
- Facilitar depuración, ajuste de pesos y explicación educativa del comportamiento resultante.

## Opciones Consideradas

### 1. Reglas rígidas / árbol de decisiones manual

Ventaja: simple de implementar al principio.

Desventaja: crece mal, se vuelve frágil ante nuevos casos y tiende a mezclar percepción, memoria y ejecución en el mismo bloque lógico.

### 2. Máquina de estados finita clásica

Ventaja: buena para estados claros como buscar comida, explorar o coordinar elevación.

Desventaja: no resuelve bien prioridades continuas ni conflictos entre objetivos concurrentes; además, puede explotar en complejidad cuando aumenta el número de transiciones.

### 3. Behavior Trees

Ventaja: muy legibles y modulares.

Desventaja: funcionan bien para secuencias de acciones, pero no expresan tan naturalmente la competencia continua entre necesidades cuantificables como hambre, riesgo, oportunidad o valor estratégico.

### 4. GOAP / planificación simbólica

Ventaja: potente para encadenar acciones orientadas a metas.

Desventaja: más costoso y más difícil de calibrar en un entorno con observabilidad parcial, recursos dinámicos y decisiones frecuentes por tick.

### 5. Utility-Based Decision System refactorizado

Ventaja: permite comparar acciones heterogéneas en una escala común, ajustar prioridades con pesos, y responder de forma flexible al estado interno y al entorno.

Desventaja: requiere diseño cuidadoso de funciones de utilidad, normalización, desempates y prevención de oscilaciones.

## Decisión Adoptada y Arquitectura

La decisión es conservar el enfoque Utility-Based, pero reorganizarlo en una arquitectura más explícita y modular. En lugar de permitir que la decisión emerja de lógica dispersa, el sistema se estructurará en capas:

1. **Percepción y actualización del Blackboard**
   - El agente recibe información del mundo, del inventario, del hambre, de la posición relativa y de las comunicaciones.
   - Esa información se normaliza y se almacena en el Blackboard como estado consultable.

2. **Generación de candidatos de acción**
   - Cada agente genera un conjunto acotado de `Bids` o candidatos: comer, buscar recurso, moverse, explorar, coordinarse, invertir en elevación, esperar, etc.
   - Cada candidato representa una intención, no una ejecución directa.

3. **Cálculo de utilidad**
   - A cada candidato se le asigna una puntuación basada en el contexto actual.
   - La puntuación combina factores como hambre, urgencia, valor del recurso visible, proximidad, riesgo, coste de desplazamiento, utilidad para la elevación y probabilidad de éxito.

4. **Selección y ejecución**
   - Se elige la acción con mayor utilidad tras aplicar penalizaciones, umbrales o desempates.
   - La ejecución queda separada de la evaluación para mantener la lógica predecible.

5. **Estado de comportamiento**
   - El sistema de decisión convive con un estado explícito del agente para evitar decisiones erráticas.
   - Ejemplos de estado: `Survival`, `Exploration`, `ResourceAcquisition`, `ElevationCoordination`.
   - El estado no sustituye la utilidad; la filtra y la orienta.

### Modelo de puntuación

Una formulación sencilla es una suma ponderada normalizada:

`Utility(action) = Σ(wi * fi(context)) - penalties + bonuses`

Donde:
- `fi(context)` son señales normalizadas en rango comparable, por ejemplo `[0..1]`.
- `wi` son pesos ajustables por prioridad del agente o del rol.
- `penalties` reducen acciones de alto coste, riesgo o redundancia.
- `bonuses` premian sinergias como proximidad a un objetivo confirmado o una oportunidad de coordinación.

Para evitar que el sistema oscile entre opciones similares, se aplicarán mecanismos como:
- histéresis o margen mínimo entre la mejor y la segunda mejor acción;
- penalización por repetición reciente;
- prioridades duras para eventos críticos como inanición inminente;
- ventanas de memoria corta en el Blackboard para contexto reciente.

### Estructuras de datos recomendadas

- **Blackboard**: memoria centralizada de corto/medio plazo.
- **ActionBid**: candidato de acción con metadatos y utilidad calculada.
- **UtilityContext**: instantánea de variables relevantes para la evaluación.
- **BehaviorState**: estado lógico del agente para filtrar prioridades.
- **PriorityProfile**: conjunto de pesos ajustables según tipo de agente, nivel o rol.

### Gestión de estado

El estado debe ser explícito pero ligero. La regla general es:
- el Blackboard contiene hechos y observaciones;
- el estado contiene intención actual y restricción temporal;
- la utilidad decide qué hacer dentro de ese contexto.

Así se evita que la memoria termine actuando como un segundo sistema de estados implícito.

## Consecuencias y Compensaciones

### Beneficios

- Más claridad arquitectónica: la decisión deja de estar mezclada con la percepción y la ejecución.
- Mayor escalabilidad: añadir acciones o agentes nuevos requiere menos reescritura.
- Mejor ajuste fino: se pueden variar pesos sin romper la lógica base.
- Más capacidad didáctica: el sistema es fácil de explicar como combinación de patrones `Strategy` y `State`.
- Mejor adaptación al entorno incompleto y cambiante de Zappy.

### Costes y Riesgos

- El diseño de utilidades mal calibradas puede producir decisiones subóptimas o inestables.
- Normalizar señales requiere cuidado para que ninguna variable domine injustamente.
- Evaluar muchas acciones por tick puede aumentar el coste de CPU si no se limita el número de candidatos.
- Un exceso de flexibilidad puede hacer el sistema difícil de depurar si no se registran decisiones y pesos.

### Impacto en memoria y rendimiento

- El Blackboard debe limitar el tamaño de sus memorias temporales.
- Los cálculos de utilidad deben ser O(n) respecto al número de candidatos evaluados por tick.
- Conviene evitar estructuras pesadas por agente si el servidor simula muchos clientes simultáneos.

## Profundización Educativa

### Strategy

El patrón `Strategy` permite encapsular familias de comportamientos o cálculos de utilidad sin incrustarlos en un único bloque monolítico. En este contexto, cada forma de evaluar una situación puede verse como una estrategia intercambiable.

Ejemplo conceptual:
- estrategia de supervivencia cuando el hambre es crítica;
- estrategia de exploración cuando la información es escasa;
- estrategia de coordinación cuando existe una oportunidad de elevación.

### State

El patrón `State` ayuda a expresar en qué “modo” está el agente. No dice exactamente qué hacer, pero sí qué prioridades son válidas. Esto es útil porque el mismo entorno puede producir decisiones distintas según si el agente está sobreviviendo, recolectando o intentando elevarse.

### Matemática de la utilidad

La idea clave es transformar señales heterogéneas en una escala común. Por ejemplo:
- hambre alta debe aumentar el valor de acciones de supervivencia;
- distancia grande debe reducir el valor de un recurso lejano;
- una oportunidad de elevación puede superar otras opciones si el contexto la hace rentable.

En términos didácticos, la utilidad es una negociación entre necesidades urgentes y valor a largo plazo.

### A tener en cuenta

- No mezclar percepción, memoria, decisión y ejecución en el mismo nivel de abstracción.
- Diseñar prioridades explícitas antes de optimizar comportamiento emergente.
- Hacer que el sistema sea explicable ayuda tanto al mantenimiento como al aprendizaje del equipo.
- En entornos con información incompleta, la robustez importa más que la decisión “perfecta”.

## Estrategia de Migración

1. Extraer la percepción actual al Blackboard de forma explícita.
2. Enumerar todas las acciones candidatas que el cliente puede evaluar.
3. Definir un contrato común para calcular utilidad por acción.
4. Separar el estado del agente de la evaluación de utilidad.
5. Introducir pesos y normalización para hambre, riesgo, exploración y elevación.
6. Añadir límites de memoria y amortiguación para evitar oscilaciones.
7. Registrar decisiones para depuración y ajuste de parámetros.
8. Migrar los demás agentes sobre la misma base, permitiendo perfiles o pesos distintos.
9. Validar el comportamiento en escenarios de supervivencia, coordinación y elevación.
10. Ajustar la documentación final del ADR con los hallazgos de implementación reales.
