# Contratos de SoulBlocks (Fase 1: Cimientos)

Congelados desde `og/cimientos`. Cambiarlos despues de este merge es trabajo del orquestador,
no de una cuadrilla de fase 2/3. Si falta algo: pedirlo en el reporte final, no improvisar un
contrato nuevo en tu rama.

## Patron de autoregistro

Cada sistema se agrega solo, sin lista central:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void Boot() { if (GameRoot.ShouldBoot) GameRoot.Attach<MiSistema>(); }
```

`GameRoot.Attach<T>()` crea (una sola vez) el GameObject "Game Root" con el componente
`GameRoot`, y devuelve el componente `T` existente o lo agrega. `ShouldBoot` es verdadero solo
si la escena activa se llama "SoulBlocks". `Boot()` corre una sola vez por sesion (no por cada
`SceneManager.LoadScene`): `GameRoot` sobrevive a `GameRoot.Restart(...)` via `DontDestroyOnLoad`
y se suscribe a `SceneManager.sceneLoaded` para reconstruir el mundo (`Layout`, `Run`, `Stats`,
tiles) en cada recarga posterior; la primera construccion ocurre en su propio `Start()`.

## Patron partial (Balance, RunState, UnlockIds)

`Balance`, `RunState` y `UnlockIds` son `static/sealed partial class`. Cada feature que necesite
sus propios campos o tunables agrega su propio archivo (`Balance.<Feature>.cs`,
`RunState.<Feature>.cs`, `UnlockIds.<Feature>.cs`) en namespace `SoulBlocks`, nunca edita el
archivo de otra cuadrilla.

## Core (`Assets/SoulBlocks/Core/`, asmdef `SoulBlocks.Core`, namespace `SoulBlocks`)

Logica pura, sin `MonoBehaviour`, sin `Physics`, sin Input System. Puede usar `Vector3`,
`Mathf`, `JsonUtility`.

- **`SeededRng`**: PRNG propio (SplitMix64), nunca `System.Random`/`UnityEngine.Random`.
  `ctor(ulong|int seed)`; `static SeededRng Stream(int seed, string name)` (deriva un stream
  independiente mezclando el seed con FNV-1a de `name`: agregar un sistema nuevo no corre el
  azar de otro); `int Next(min, maxExclusive)`; `float NextFloat()` en `[0,1)`;
  `float Range(min, max)`; `bool Chance(p)`; `T Pick<T>(IList<T>)`; `void Shuffle<T>(IList<T>)`.
- **`Balance`** (`Balance.Core.cs`): campos `public static float` (no `const`, ajustables por
  balance pass). `TileSize=2`, `TileThickness=.25`, `HubPickupTreeRange=4`, y la base de cada
  `StatId` (ver tabla en `StatId.cs` / `Balance.Core.cs`).
- **`StatId`** (enum, congelado): `MoveSpeed, CargoCapacity, EnergyMax, PickupRadius,
  DashCharges, DashDistance, DashRechargeSeconds, MagnetRadius, MagnetDuration, MagnetCooldown,
  RayLevel, RayLength, RayCharges, RaySoulsPerCharge, RayRechargeSeconds, BlessingOptions`.
- **`StatSheet`** (clase pura): `float Get(StatId)` = `(base + suma de add) * producto de mul`
  sobre todas las fuentes; `int GetInt(StatId)` redondeado; `void SetModifier(string source,
  StatId, float add, float mul = 1f)` (reemplaza el modificador previo de esa fuente para ese
  stat, no se acumulan); `void ClearSource(string source)`; `event Action Changed`.
- **`RunState`** (`[Serializable] sealed partial class`): todo el estado compartido persistido,
  serializable con `JsonUtility` (listas, nunca `HashSet`/`Dictionary`). Campos principales:
  `seed, effectiveSeed, expeditionIndex, playTimeSeconds, inExpedition, victory, reserveSouls,
  cargoSouls, wisdom, energy, treeStage, unlocks, upgrades, prisons, brokenByTier[5],
  discoveredTiles, groundSouls, plan (ExpeditionPlan), activeBlessings, totalSoulsCollected,
  totalPrisonsBroken`. Helpers: `HasUnlock`, `AddUnlock`, `UpgradeLevel`, `SetUpgradeLevel`,
  `Prison(id)` (get-or-create). Un `new RunState()` nunca tiene listas nulas. Clases anidadas
  (mismo archivo): `UpgradeLevel, PrisonRecord, GroundSoul, ExpeditionPlan, ObjectiveRecord,
  AnomalyRecord`.
- **`UnlockIds`**: consts `Raiz1, Raiz2, Raiz3, Despertar, Entrega, Brote, Revelacion, Cadena,
  BendicionExtra` (valores `"wisdom.*"`).
- **`ExpeditionEndReason`** (enum): `OnFoot, Portal, EnergyDepleted, Final`.
- **`PrisonForecast`** (struct) + **`ForecastKind`** (enum `Breaks, Hits, RequiresLevel,
  Broken`): `Kind`, `hits`, `requiredLevel`, `string Describe()` (texto en castellano: "ruptura
  inmediata" / "N impactos" / "requiere Rayhit nv X" / "rota").
- **`Core/Map/MapLayout.cs`**: enums `BranchSpecialty, PrisonTier, PrisonProp [Flags],
  PrisonPattern`; clases de datos `MapLayout, BranchLayout, TileLayout, SoulSpawnLayout,
  PrisonLayout, ShelterLayout, GapLayout, ConnectionLayout, AltarLayout, PortalLayout,
  MapGenInput, MapAnchor`. Todas `[Serializable]`, listas inicializadas.
- **`MapGen.Generate(MapGenInput)`**: implementacion actual es un *stand-in temporal*
  (marcado `ponytail:`) que la cuadrilla `og/mapa` reemplaza por completo: 3 ramas rectas, una
  por anchor recibido, especialidad por indice (`Souls, Prisons, Exploration`), 10 tiles cada
  una espaciados `Balance.TileSize` empezando un tile afuera del anchor, una prision T1 al
  final (`branchEnd = true`), 4 puntos de almas (`baseCount = 5`), un refugio a mitad de la
  rama de Exploracion.
- **`MapLayoutFixtures.Simple()`**: misma forma que el stand-in, anchors fijos en el origen
  (+Z, +X, -X), `hubCenter = (0,0,0)`. Usado por los tests de todas las cuadrillas.

## Runtime (`Assets/SoulBlocks/Runtime/`, asmdef `SoulBlocks.Runtime`, namespace `SoulBlocks`)

- **`GameEvents`**: un `event Action<...>` + `Raise...` estatico por cada señal cruzada
  (lista completa en `GameEvents.cs`: `MapBuilt, EnteredHub, LeftHub, ExpeditionStarted,
  ExpeditionEnded, ExpeditionPlanned, EnergyChanged, EnergyWarning, SoulsCollected,
  CargoChanged, SoulsDeposited, WisdomChanged, PrisonDamaged, PrisonPhaseCompleted,
  PrisonBroken, UnlockGranted, UpgradePurchased, StatsChanged, TreeStageChanged,
  ShelterEntered, ShelterExited, TileDiscovered, DashUsed, MagnetUsed, RayhitFired,
  ObjectiveCompleted, BlessingChosen, GameWon`). `ResetAll()` limpia todos los eventos; se
  llama solo en `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` (al iniciar el
  proceso/dominio, no por cada escena).
- **`GameRoot`** (`MonoBehaviour sealed`): `static Instance`; `Run (RunState), Stats
  (StatSheet), Layout (MapLayout), Player (SoulPlayer), Tree/Island (Transform), ViewCamera
  (Camera)`. `static T Attach<T>()`; `static int? PendingSeed; static RunState PendingLoad;
  static void Restart(int? seed, RunState load)` (fija pendientes y recarga la escena activa).
  Construye el mundo (encuentra Player/Tree/Island, crea o restaura `Run`, detecta 3 anchors
  desde los colliders de la isla -logica de `BuildRoad`: Norte +Z en x=-5, Este +X en z=-12,
  Oeste -X en z=-30-, `Layout = MapGen.Generate(...)`, `Run.effectiveSeed`, energia al maximo,
  `MapBuilder.Build(...)`, `GameEvents.RaiseStatsChanged()`, `GameEvents.RaiseMapBuilt(...)`).
  `static GameRoot BootWith(MapLayout, RunState)`: arranque minimo para PlayMode tests de otras
  cuadrillas, sin deteccion de anchors ni dependencia de la isla real.
- **`MapBuilder`**: crea el root "Generated Map"; un cubo por tile (`TileSize x TileThickness x
  TileSize`, superficie superior en `tile.position.y`, capa 8, con collider, color neutro
  oscuro); los tiles con `requiresUnlock` no vacio arrancan inactivos y se activan al recibir
  `UnlockGranted` (o de entrada si `Run.HasUnlock` ya es verdad). `GameObject
  TileObject(int index)`. No construye refugios/huecos/altares/portales: eso es de cada
  cuadrilla dueña de esa feature.
- **`HubZone`**: cada frame hace un raycast hacia abajo desde el jugador; "en el hub" = el
  collider golpeado es hijo del transform `Island`. Dispara `EnteredHub`/`LeftHub` solo en la
  transicion. `bool InHub`.
- **`Hud`** (IMGUI): `static void Register(int order, Action draw)` /
  `Unregister(Action draw)`; `static void Toast(string, float seconds = 2f)` (pila de mensajes
  arriba al centro); `static void WorldLabel(Vector3 world, string text)` (usar solo dentro de
  un callback registrado). Un unico `OnGUI` dibuja los callbacks por `order`, luego los toasts,
  y una linea de debug arriba a la izquierda con el seed efectivo.
- **`Keys`** (estatico): `Blocked` (la UI lo pone en `true` mientras hay un modal abierto).
  `DashPressed` (Shift), `MagnetPressed` (Q), `InteractPressed` (E), `MapHeld` (Tab),
  `PausePressed` (Escape), `RayAimHeld`/`RayReleased` (click izquierdo), `MousePosition`. Todo
  getter booleano devuelve `false` sin foco o bloqueado.
- **`Placeholder`** (estatico): `GameObject Make(PrimitiveType, Color, string name, Transform
  parent, Vector3 position, Vector3 scale, bool keepCollider = false, float emission = 0f)`
  (un unico material URP Lit compartido, color/emision via `MaterialPropertyBlock`); `TextMesh
  Label(Transform parent, string text, Vector3 localOffset, Color color, float size = .25f)`
  (fuente `LegacyRuntime.ttf`); `void SetColor(GameObject, Color, float emission = 0f)`.
- **`SoulPlayer`** (hooks agregados, el resto del archivo sigue igual): `public float
  SpeedMultiplier = 1f` (multiplica `moveSpeed` en `StepMovement`); `public bool
  MovementLocked` (ignora el input pero la gravedad sigue aplicando); `public bool
  TryMoveSupported(Vector3 delta)` (el sweep compartido, devuelve `false` si algun paso tuvo
  que recortarse); `public void MoveUnchecked(Vector3 delta)`; `public void Teleport(Vector3
  position)`; `public static bool IsSupportedAt(Vector3 point)`. `StepMovement` ahora llama a
  `TryMoveSupported` en vez de duplicar el sweep.
- **`PlayerStatsBinder`** (autoregistrado): en `GameEvents.StatsChanged` y al activarse, fija
  `Player.SpeedMultiplier = Stats.Get(StatId.MoveSpeed) / Balance.MoveSpeed` (el `moveSpeed`
  del inspector sigue siendo la base).
- **`ISoul`** + **`SoulRegistry.All`**: contrato para cualquier alma recogible
  (`Position, Value, Available, MoveTo, Collect`). Lista global limpiada en
  `SubsystemRegistration`.
- **`IRayTarget`** + **`RayTargetRegistry.All`**: contrato para cualquier objetivo de Rayhit
  (`PrisonId, Position, Radius, Forecast(rayLevel), ApplyRayHit(rayLevel)` devuelve el dano
  aplicado). Lista global limpiada en `SubsystemRegistration`.

## Quien es dueño de que (fases siguientes, ver `PLAN-OBRA-GRUESA.md`)

| Cuadrilla | Archivos/sistemas que posee |
|---|---|
| `og/mapa` | `MapGen.Generate` real, `Balance.Mapa.cs`, `MapBuilder` completo (giros, huecos, refugios, segmentos con candado) |
| `og/expedicion` | Energia, carga/reserva, `SoulPickup : ISoul`, deposito, `Balance.Expedicion.cs` |
| `og/prisiones` | Tiers/fases/HP, `PrisonView : IRayTarget`, resonancia, `Balance.Prisiones.cs` |
| `og/habilidades` | Dash/Iman/Rayhit, previews, `Balance.Habilidades.cs` |
| `og/progresion` | Arboles de Almas/Wisdom, compra, aplicacion a `StatSheet`, `Balance.Progresion.cs` |
| `og/azar-expedicion`, `og/azar-recompensas`, `og/mundo`, `og/persistencia-ui`, `og/final`, `og/simulacion`, `og/humo` | Fase 3/4, ver tabla del plan |

## Regla

Los contratos de este documento estan **congelados** desde el merge de `og/cimientos`. Si una
cuadrilla de fase 2/3 necesita un campo, evento o tipo que no esta aqui: lo pide en su reporte
final, no lo agrega por su cuenta ni edita estos archivos.
