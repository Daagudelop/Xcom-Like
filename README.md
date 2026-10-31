# Proyecto táctico por turnos (Unity)

Prototipo de juego táctico en grid con dos modos de juego:

- **Exploración:** control de escuadrón estilo RTS (selección múltiple, movimiento en formación, inventario).
- **Combate:** turnos por equipos, movimiento por casillas sobre el NavMesh y habilidades por entidad.

Usa **NavMesh** para el movimiento, **Cinemachine 3.1.7** para la cámara y el **Input System** en la cámara. El resto de scripts usa el Input clásico (`Input.GetKey...`).

---

## Estructura de `Assets`

```
Assets/
├── Scripts/
│   ├── GameManager.cs            Estados globales del juego
│   ├── TurnManager.cs            Turnos por equipo y selección en combate
│   ├── Entity.cs                 Unidad de combate (stats, acciones, equipo)
│   ├── GridPointer.cs            Puntero sobre el grid
│   ├── Utilities.cs              Utilidades de NavMesh y celdas
│   ├── CameraMovement.cs         Cámara (Cinemachine + Input System)
│   ├── ObjectPooler.cs           Pool de objetos
│   ├── Habilities/
│   │   ├── MovementOnGrid/GridPathFinding.cs   Habilidad de movimiento en grid
│   │   └── GridShapes/                         Formas de ataque (ScriptableObjects)
│   ├── Squad/                    Modo exploración: escuadrón
│   └── Inventory/                Inventario por soldado
├── Prefabs/                      TestEntity, cámaras y habilidad MovementOnGrid
├── Scenes/                       SampleScene, New Scene (+ NavMesh horneado)
├── Samples/Cinemachine/3.1.7/    Ejemplos oficiales de Cinemachine
└── InputSystem_Actions.inputactions
```

---

## Núcleo del juego

### `GameManager`
Singleton con la máquina de estados global.

- Estados (`GameState`): `Setup`, `InExplorationMode`, `InBattle`.
- Eventos estáticos `OnStateEnter` y `OnStateExit`.
- `ChangeState(...)`, y atajos `ChangeToBattle()` / `ChangeToExploration()` (también como *Context Menu* en el inspector).

### `TurnManager`
Singleton que gestiona los turnos **por equipo** (`TeamsEnum`: `Ally`, `player`, `Enemy`).

- Al entrar en `InBattle` arma los equipos con los objetos de tag `Entity` y empieza el turno del jugador.
- `NextTurn()` cierra el turno del equipo actual, pasa al siguiente en la cola y avisa a las entidades (`Entity.OnBattle`).
- Eventos `OnStateEnter` / `OnStateExit` por equipo.
- **Acciones programadas por turno:**
  - `ScheduleOnTurnStart(n, acción)` y `ScheduleOnTurnEnd(n, acción)`: se ejecutan una vez dentro de *n* turnos.
  - `ScheduleRepeatingOnTurnStart(count, acción)` y `ScheduleRepeatingOnTurnEnd(count, acción)`: se repiten *count* veces.
- **Selección en combate:** clic izquierdo sobre una entidad jugable. Resalta con `Outline` y activa las habilidades de la entidad (`IAbility`).
- Define la interfaz `IAbility`: `SelectedPlayable`, `UnSelectedPlayable`, `SelectableAI`, `UnSelectableAI`.

### `Entity`
Unidad de combate.

- `team`, `cellSize` (por defecto 2), `playeable` y un `Outline` de selección.
- `stats`: diccionario `StatName → StatData` (`min`, `current`, `max` y listas de ecuaciones). Stats actuales: `Strength`, `dextery`, `Intelligence`, `MovementPoints`.
- `actions`: diccionario `ActionName → ActionsData` (prefab de la habilidad y `activeOnStart`). Acción actual: `MoveOnGrid`.
- `OnBattle(bool)`: al empezar el turno instancia las habilidades marcadas como `activeOnStart` y marca la entidad como jugable si es del jugador; al terminar, limpia las instancias.

### `GridPointer`
Singleton que mueve un puntero a la casilla bajo el ratón, limitado a las casillas válidas de la entidad seleccionada.

- Clic derecho sobre una casilla válida dispara `OnGridPointConfirmed`.
- `NormalizeKey` unifica las claves de `Vector3` (evita el `-0` y errores de precisión).

### `Utilities`
Funciones estáticas: ajustar posiciones al NavMesh (`SnapOnNavmesh`), redondeo a la cuadrícula (`RoundToGrid`), centrado en celdas (`CenterPosOnCell`) y comprobación de punto dentro de triángulo.

---

## Habilidades

### `GridPathFinding` (habilidad *MoveOnGrid*)
Implementa `IAbility`.

- `CreateMovementRegion(...)`: BFS sobre el NavMesh en 8 direcciones. Ortogonal cuesta 1 y diagonal 1,5; el alcance lo da `MovementPoints`. Rellena el diccionario de casillas alcanzables con su coste.
- Al seleccionar la entidad calcula la región y se suscribe al puntero. Al confirmar una casilla, mueve el `NavMeshAgent`, descuenta puntos de movimiento y los repone al final del turno.
- Dibuja las casillas alcanzables como gizmos si `debug` está activo.
- Contiene, comentado, un `CreateVisibilityRegion` (región de visión por raycasts) que quedó como base para visibilidad.

### Formas de ataque (`GridShapes`)
- **`AttackShape`** (*ScriptableObject*, menú `Combat/Attack Shape`): forma de un área de efecto.
  - Tipos (`ShapeType`): `Circle`, `Diamond`, `Square`, `Cone`, `Line`, `Cross`, `Ring`, `Custom`.
  - Parámetros: `size`, `width`, `includeOrigin`, `rotatesWithDirection` y `customOffsets`.
- **`ShapeResolver`**: convierte una forma en casillas reales del grid.
  - Rota la forma hacia el puntero (pasos de 45° o 90°) y la proyecta desde un origen.
  - Se queda solo con las casillas que existen en el grid, tolerando diferencias de altura.
- `Granade.asset` es una forma de ejemplo.

---

## Modo exploración: escuadrón (`Scripts/Squad`)

Solo funciona en `InExplorationMode` (el inventario también en `InBattle`).

| Script | Función |
|---|---|
| `Soldier` | Soldado con `NavMeshAgent` e `Inventory`. Gestiona selección, marca visual, caminar/correr y parar. Mantiene la lista estática `Soldier.All`. |
| `SquadSelection` | Selección estilo RTS (ver controles). Singleton, con evento `OnSelectionChanged`. |
| `SquadMovement` | Movimiento *point & click* del grupo en formación sobre el NavMesh. Asigna a cada hueco el soldado libre más cercano. |
| `SquadGameRules` | Reglas estáticas que indican qué se puede hacer en cada `GameState`. |
| `SquadDebugHUD` | HUD de depuración con `OnGUI`: seleccionados e inventario del primero. |

### Controles de exploración

| Acción | Control |
|---|---|
| Seleccionar | Clic izquierdo |
| Añadir o quitar de la selección | Shift o Ctrl + clic |
| Selección por caja | Arrastrar |
| Seleccionar todos los visibles | Doble clic sobre un soldado |
| Seleccionar a todo el escuadrón | Espacio |
| Guardar o recuperar grupo | Ctrl + 1..9 / 1..9 |
| Mover en formación | Clic derecho |
| Correr | Doble clic derecho o Shift + clic derecho |
| Detener | S |
| HUD de prueba: añadir ítem / pasar ítem al segundo seleccionado | T / Y |

---

## Inventario (`Scripts/Inventory`)

- **`ItemData`** (*ScriptableObject*, menú `Squad/Item`): nombre, icono, tipo (`Weapon`, `Armor`, `Consumable`, `Ammo`, `Misc`), `maxStack`, `weight` y descripción.
- **`Inventory`**: slots con límite de capacidad (`capacity`) y de peso (`maxWeight`, 0 = sin límite).
  - `Add` completa stacks existentes y crea nuevos; devuelve lo que no cupo.
  - `Remove`, `Has`, `Count`, `CurrentWeight` y el evento `OnChanged`.
  - `Transfer` pasa ítems a otro inventario (intercambio entre soldados).

---

## Cámara y utilidades

- **`CameraMovement`** (Cinemachine `OrbitalFollow` + Input System, acciones creadas por código):

  | Acción | Control |
  |---|---|
  | Mover | WASD o stick izquierdo |
  | Rotar | Q / E |
  | Zoom | Rueda del ratón |
  | Subir piso | Shift izquierdo |
  | Bajar piso | Ctrl izquierdo |

  El movimiento se alinea con el *forward* de la cámara real. Tiene parámetros de velocidad, aceleración, suavizado de giro y límites de altura por piso.
- **`ObjectPooler`**: pool genérico. Uso rápido: `ObjectPooler.Spawn(prefab, pos, rot)` y `ObjectPooler.Despawn(obj)` (también con retardo). Admite pools predefinidos en el inspector o por código con `CreatePool(...)`.

---

## Prefabs y escenas

- `TestEntity` (y `TestEntity (1)`): entidad de prueba (tag `Entity`, capa 6, equipo jugador, `cellSize` 2).
- `Habilities/MovementOnGrid`: prefab de la habilidad de movimiento.
- `Main Camera` y `FreeLook Camera`.
- `SampleScene`: escena principal, con NavMesh horneado en `Scenes/SampleScene/NavMesh-Plane.asset`. `New Scene` es una escena vacía.

---

## Dependencias

- Unity con **NavMesh** y **Input System**.
- **Cinemachine 3.1.7**.
- **SerializedDictionary** (`AYellowpaper.SerializedCollections`), usado por `Entity` y `TurnManager`.
- Componente **`Outline`** (tipo QuickOutline), usado para resaltar entidades.

---

## Cosas a tener en cuenta (estado actual del código)

- `GridPathFinding.SelectableAI()` y `UnSelectableAI()` lanzan `NotImplementedException`: la IA aún no está implementada.
- `TurnManager.Select` / `Deselect` llaman a las habilidades del **prefab** (`item.Value.prefab`) y no a las instancias en `ActionsBeingUsed`.
- En `TestEntity`, la acción `MoveOnGrid` tiene `activeOnStart` en `false`, así que no se instancia al empezar el turno.
- `Entity.OnBattle(false)` borra elementos de `ActionsBeingUsed` dentro de un `foreach`, lo que lanza una excepción en tiempo de ejecución.
- Hay dos sistemas de selección independientes: el de exploración (`SquadSelection`, sobre `Soldier`) y el de combate (`TurnManager`, sobre `Entity`).
