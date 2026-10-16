# Contratos de SoulBlocks (Fase 1: Cimientos)

> Histórico: describe la etapa con mapa generado por seed (MapGen, recogida de almas, Rayhit).
> El juego actual usa un mundo hecho a mano; ver el README.

Congelados desde `og/cimientos`. Cambiarlos despues de este merge es trabajo del orquestador,
no de una cuadrilla de fase 2/3. Si falta algo: pedirlo en el reporte final, no improvisar un
contrato nuevo en tu rama.

## Patron de autoregistro

Cada sistema se registra por tipo, sin lista central:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void Register() => GameRoot.Register<MiSistema>();
```

Cada vez que carga la escena "SoulBlocks" (primer Play, `GameRoot.Restart(...)`, un PlayMode test
que la carga) `GameRoot` crea un "Game Root" nuevo dentro de esa escena y le agrega todos los
sistemas registrados. Sin `DontDestroyOnLoad`: al recargar, el root viejo muere con la escena.
`GameRoot.Start()` construye el mundo y lanza `MapBuilt` cuando todos los sistemas ya pasaron
por `OnEnable`.

Reglas para cada sistema:
- Suscribirse a `GameEvents` en `OnEnable` y desuscribirse en `OnDisable` (sobrevive a Restart).
- `Hud.Register` en `OnEnable`, `Hud.Unregister` en `OnDisable`.
- Inicializar el estado del mundo al recibir `MapBuilt`, no en `Start`.
- Estado estatico propio: limpiarlo en `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.

`GameRoot.Attach<T>()` sigue disponible para componentes creados a demanda (ej. `MapBuilder`).
`GameRoot.BootWith(layout, run)` (tests con escena propia) crea el root, agrega los sistemas
registrados y construye con ese layout, sin detectar anclajes.

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
  una espaciados `Balance.TileSize`, el primero pegado al borde de la isla, una prision T1 al
  final (`branchEnd = true`), 4 puntos de almas (`baseCount = 5`), un refugio a mitad de la
  rama de Exploracion.
- **`MapLayoutFixtures.Simple()`**: misma forma que el stand-in, anchors fijos en el origen
  (+Z, +X, -X), `hubCenter = (0,0,0)`. Usado por los tests de todas las cuadrillas.

## Runtime (`Assets/SoulBlocks/Runtime/`, asmdef `SoulBlocks.Runtime`, namespace `SoulBlocks`)

- **`GameEvents`**: un `event Action<...>` + `Raise...` estatico por cada señal cruzada
  (lista completa en `GameEvents.cs`: `MapBuilt, EnteredHub, LeftHub, ExpeditionStarted,
  ExpeditionEnded, ExpeditionPlanned, EnergyChanged, EnergyWarning, SoulsCollected,
  CargoChanged, SoulsDeposited, SoulsReleased, WisdomChanged, PrisonDamaged, PrisonPhaseCompleted,
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

## Reglas de cooperacion entre cuadrillas (agregadas por el orquestador)

- `SoulsReleased(Vector3 position, int count)`: quien libera almas sueltas (prisiones, altares)
  solo lanza el evento; `og/expedicion` las crea como `SoulPickup`, las persiste en
  `Run.groundSouls` hasta que se recogen.
- Expedicion: `og/expedicion` es dueña de `Run.inExpedition`, `expeditionIndex`, `energy`,
  `cargoSouls`, `reserveSouls` y de lanzar `ExpeditionStarted/Ended`. El `EnteredHub` inicial
  al bootear no es fin de expedicion.
- Prisiones: `og/prisiones` es dueña de `Run.prisons`, `brokenByTier`, `totalPrisonsBroken`,
  suma fragmentos a `Run.wisdom` (+ `WisdomChanged`). Portales NO son suyos (`og/mundo`, Fase 3).
- Progresion: `og/progresion` es dueña de `Run.upgrades`, `Run.unlocks` de nodos Wisdom,
  `treeStage`, y de los modificadores de `StatSheet` (fuente `"upgrade.<id>"`).
- Mapa: `og/mapa` es dueña de `Run.discoveredTiles` y de los refugios (lanza
  `ShelterEntered/Exited`); `og/expedicion` solo escucha.
- Rangos de `Hud.Register(order, ...)`: expedicion 100-199, prisiones 200-299, habilidades
  300-399, progresion 400-499, mapa 500-599, fase 3 600+.
- Tests PlayMode: cargar "SoulBlocks", esperar `MapBuilt`, leer posiciones de
  `GameRoot.Instance.Layout` y teletransportar. Nunca asumir la forma del stand-in (el mapa real
  la cambia). Tests propios en `Tests/EditMode/<Feature>/` y `Tests/PlayMode/<Feature>/`.

## APIs publicas que dejo la Fase 2 (usar, no reimplementar)

- `ExpeditionController.Instance`: `EndExpedition(ExpeditionEndReason)` (portales), `DepositCargo()`
  (entrega en refugios), `static Func<SoulSpawnLayout,int> SpawnCountFor` (densidad variable).
  Almas recogibles: `SoulPickup : ISoul`; para crear almas sueltas usar
  `GameEvents.RaiseSoulsReleased(pos, n)`.
- `PrisonSystem`: `static Func<int,float> DamageMultiplierFor` (prision debilitada),
  `static event Action<PrisonLayout,Vector3> Broken` (recompensas extra). Reglas puras en
  `PrisonRules`, tabla en `Balance.Prisiones`.
- Habilidades (componentes en el Game Root): `DashAbility.TryDash(dir)`, `MagnetAbility.TryMagnet()`,
  `RayhitAbility.Fire(aimPoint)`. Rayhit en cadena se activa con `UnlockIds.Cadena` o con
  `"rayhit_cadena"` en `Run.activeBlessings`.
- `ProgressionSystem.TryBuy(id)`; datos en `UpgradeCatalog.All/Get/Tree`, reglas en `ProgressionRules`
  (`CoreComplete`, `Cost`, `WhyCannotBuy`, `ApplyStats`).
- Mapa: `MapBuilder.TileObject(index)`, refugios lanzan `ShelterEntered/Exited(id)`,
  `TileDiscovery` llena `Run.discoveredTiles`.
- Modificadores temporales de stats: `GameRoot.Instance.Stats.SetModifier("blessing.<id>" |
  "anomaly.<id>" | "reward.<id>", stat, add, mul)` y `ClearSource(...)` al terminar la expedicion,
  seguido de `GameEvents.RaiseStatsChanged()`.

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
