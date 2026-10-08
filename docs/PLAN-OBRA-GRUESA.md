# SoulBlocks: plan de obra gruesa de mecánicas

## Context

`D:\Games\SoulBlocks` (Unity 6000.6.0f1, URP, Input System) hoy tiene: isla principal, Wisdom Tree, jugador con idle/walk, cámara orbital y una mecánica de bloques (agarrar bloque, colocarlo en tramo oscuro). Un chat de diseño previo criticó el concepto (caminar, recoger almas, romper cristales, dos árboles de mejoras, final definido) y propuso correcciones para cada debilidad, más ideas propias del usuario (energía espiritual, tiers de cristal, carga limitada, pasivas, dash/imán/rayhit, mapa por seed) y aleatoriedad controlada.

Objetivo: consolidar todo en una definición final de mecánicas y construir la **obra gruesa**: todas las mecánicas funcionando de punta a punta, sin animaciones ni modelos nuevos (primitivas, colores, texto, IMGUI). Los acabados (arte, animación, audio, UI final, cosméticos) quedan fuera. Ejecución: ramas git en paralelo, agentes Sonnet como cuadrillas, Claude Opus como orquestador (jefe de obra).

### Decisiones cerradas (usuario)
- Mecánica de bloques: **se elimina**. Caminos generados por seed, abiertos. Código archivado con tag git.
- Árbol de mejoras: **núcleo obligatorio + especializaciones opcionales**.
- Verificación: **solo el orquestador** compila y corre tests (Unity batchmode, en serie, al integrar). Agentes escriben código + tests, no abren Unity.
- Base: descartar cambio sin commit en `SoulBlocks.unity`, mergear `feat-random-blocks` a `develop`, integración sale de `develop`.

### Decisiones por defecto (tomadas del chat, ajustables)
- Límite de almas = **carga transportada** por expedición (100 inicial). La **reserva** del árbol no tiene límite.
- Energía en 0 = **retorno automático** al árbol **conservando la carga**. Avisos a 25% y 10%.
- Carga llena = se sigue explorando y dañando prisiones; almas extra quedan en el suelo.
- "Cristales" se renombran: el objeto es **Prisión cristalina**, el recurso es **Fragmento de Wisdom**.

---

## 1. Definición final de mecánicas (GDD consolidado)

### 1.1 Bucle principal
1. En el árbol: depositar carga en reserva, energía al máximo, comprar mejoras, ver 3 objetivos + anomalía anunciada, elegir objetivo (opcional).
2. Salir por una rama: la energía empieza a bajar.
3. En el camino: recoger almas (carga), dañar prisiones con Rayhit, usar dash/imán, descubrir refugios, huecos y conexiones.
4. Volver: a pie, por portal (si existe) o retorno automático al agotar energía.
5. Cada etapa del árbol central transforma el mundo. La última abre la expedición final. Romper el Núcleo = victoria.

### 1.2 Recursos
| Recurso | Fuente | Uso | Reglas |
|---|---|---|---|
| Almas | Puntos de aparición en ramas, liberadas al romper prisiones, altares | Árbol de Almas (eficacia inmediata) | Carga máx por expedición (100 base). Reserva ilimitada. Valor fijo por alma (sin azar). |
| Fragmentos de Wisdom | Romper prisiones (base garantizada por tier + parcial por fase), objetivos cumplidos | Árbol de Wisdom (reglas nuevas, etapas del árbol) | Sin límite. Las compras obligatorias se financian solo con recompensas garantizadas. |

### 1.3 Energía espiritual
- Baja 1/s fuera del árbol. Base 25 s (tunable). No baja dentro de un **refugio**.
- Aviso visual a 25% y 10%. A 0: teletransporte al árbol, carga conservada y depositada.
- Recarga completa al entrar al árbol.

### 1.4 Mapa por seed
- Hub = isla principal existente + árbol. 3 ramas salen de 3 bordes de la isla (polilíneas en grilla de 2 m, con giros, sin autointersección).
- La seed decide: qué dirección recibe cada **especialidad**, longitudes, giros, refugios, huecos, puntos de almas, patrones y propiedades de prisiones. Mapa estable dentro de la partida. Seed visible, copiable e ingresable.

| Rama | Largo inicial | Contenido | Limita sobre todo |
|---|---|---|---|
| Almas | 30-40 m | Almas densas cerca, T1 | Carga |
| Prisiones | 60-80 m | Grupos T1-T3 en patrones, prisión de extremo | Rayhit |
| Exploración | 100-140 m | Refugios, huecos de dash, anomalías y altares más frecuentes | Energía |
| Profundas (Raíz III) | +60 m por rama | T4, almas valiosas | Todo |

- Elementos con candado (`requiresUnlock`): segmentos profundos, puentes entre extremos de ramas, portales. Aparecen al desbloquearse (sin animación, aparición instantánea).
- Huecos de dash: solo en atajos u oportunidades opcionales, nunca en el camino obligatorio.
- **Validador** (regenera con seed+1 hasta 50 intentos, guarda seed efectiva): determinismo, sin solapes, almas alcanzables con energía base >= 1.5x costo de la primera mejora, primera T1 alcanzable ida y vuelta con energía base, cada gate de etapa alcanzable con el poder disponible en esa etapa, distancias entre refugios acotadas, huecos <= distancia de dash disponible.
- Descubrimiento: tiles visitados quedan marcados (mapa con Tab).

### 1.5 Prisiones cristalinas
| Tier | Color + forma placeholder | Fases | HP | Rayhit req. | Fragmentos |
|---|---|---|---|---|---|
| T1 | Amarillo, cubo | 1 | 2 | nv 0 | 1 |
| T2 | Verde, cilindro | 2 | 6 | nv 1 | 3 |
| T3 | Azul, cápsula | 3 | 16 | nv 2 | 8 |
| T4 | Rojo, esfera | 3 | 35 | nv 3 | 20 |
| Núcleo | Blanco, esfera XL + anillo | 3 | 90 | nv 4 | victoria |

- Daño Rayhit por nivel: `[1, 2, 4, 7, 12]`. Debajo del nivel requerido: daño 0 y aviso "requiere Rayhit nv X". Regla de diseño: <= 3 impactos por fase al nivel previsto.
- **Daño persiste** entre expediciones. Cada fase completada da fragmentos parciales + ráfaga de almas.
- **Previsión** al apuntar: "ruptura inmediata" / "N impactos" / "requiere Rayhit nv X". Símbolo de tier además del color.
- Propiedades secundarias (seed, visibles con un hijo primitivo): **Resonante** (propaga 50% del daño al grupo), **Núcleo expuesto** (tras el primer impacto recibe x2). Nunca en un tier que exija algo inalcanzable.
- Patrones de grupo (diseñados, no libres): línea, arco, compacto, conectado.
- Consecuencias al romper: fragmentos + almas liberadas (quedan en el suelo si la carga está llena) + avance de gate de etapa + la prisión de extremo de rama abre **portal** al árbol (si Raíz I).

### 1.6 Habilidades activas (controles centralizados en `Keys`)
| Habilidad | Tecla | Regla base | Antídoto a la debilidad del chat |
|---|---|---|---|
| Dash | Shift | 1 carga inicial (2 con hito), 6 m, recarga 3 s por carga | Desliza por bordes, se detiene antes de sólidos, cruza huecos si el destino tiene soporte. Marcador de destino previsto. Huecos/atajos premian reservar cargas. |
| Imán | Q | Radio 8 m, 3 s, cooldown 12 s | Atrae solo lo que cabe, cercanas primero; el resto queda visible. HUD muestra capacidad libre. Recogida básica cómoda sin imán (radio 1.5 m). |
| Rayhit | LMB mantener = apuntar, soltar = lanzar | 1 carga, 8 m, el jugador viaja en línea a ~40 m/s y daña cada prisión atravesada | Cargas se recuperan recogiendo almas (1 por 25) + regen mínima por tiempo (20 s). Trayectoria con preview que atraviesa varias prisiones. Impacto mal alineado sigue sirviendo. Termina en el último punto con soporte. |

### 1.7 Árboles de mejoras
**Árbol de Almas (eficacia, niveles, costo geométrico x1.5, primera compra ~40 almas)**: carga (+50), energía (+8 s), velocidad (+8%), radio de recogida (+0.5 m), dash (distancia; hito: 2da carga), imán (radio, duración), Rayhit (nivel = daño/umbral; longitud). Cada 3 niveles hay un **hito perceptible**. La tienda muestra el **beneficio estimado** ("+X m de alcance", "T2: 3 impactos -> 2").

**Árbol de Wisdom (reglas)**
| Nodo | Tipo | Gate | Efecto en el mundo |
|---|---|---|---|
| Raíz I | Núcleo | Romper 1 T1 | Portales en extremos de rama rotos. Árbol etapa 1. |
| Raíz II | Núcleo | Romper 1 T2 | Puentes entre extremos de ramas (circuito, adiós callejones). Etapa 2. |
| Raíz III | Núcleo | Romper 1 T3 | Aparecen segmentos profundos con T4. Etapa 3. |
| Despertar | Núcleo | Romper 1 T4 | Abre la expedición final (Núcleo). Etapa 4. |
| Entrega en refugios | Opcional | - | Depositar carga en refugios sin volver. |
| Brote | Opcional | - | Almas reaparecen cerca del árbol y se recogen solas (automatización). |
| Revelación | Opcional | - | Mapa muestra densidad, anomalías y propiedades de prisiones. |
| Rayhit en cadena | Opcional | - | Tras el último impacto salta a la prisión más cercana sin tocar. |
| Bendición extra | Opcional | - | 4 opciones en vez de 3, o re-roll. |

Etapas del árbol: placeholder = cambio instantáneo de emisión/escala en `WisdomTreeVisual`.

### 1.8 Aleatoriedad (oportunidades, nunca reglas)
| Qué | Cómo | Garantía anti-frustración |
|---|---|---|
| Estructura del mapa | Seed (1.4) | Validador |
| Densidad de almas por expedición | Puntos fijos, densidad variable; pista en el árbol de la rama más activa | Mínimo garantizado por expedición |
| 1 anomalía por expedición | Concentración de almas / prisión debilitada / corriente que amplifica dash; anunciada antes de salir | Opcional, no desaparece antes de alcanzarla |
| Objetivos de expedición | 3 ofrecidos (recolectar en rama X, fracturar prisión P, descubrir refugio/conexión R); elegir 1; bonus al cumplir | Siempre alcanzables con poder actual |
| Extra al romper | Base garantizada + extra pequeño sorteado (almas, energía, efecto temporal) | Compras obligatorias no dependen del extra |
| Bendiciones | Al romper prisión de extremo: 3 efectos temporales, elegir 1 (dash extra, imán amplio, Rayhit encadenado); duran la expedición | Solo opciones útiles para habilidades actuales |
| Altares de riesgo | Desvío anunciado: gastar X energía -> depósito de almas | Costo visible antes de entrar |

**Nunca aleatorio**: daño de Rayhit, costos, duración de energía, valor de una recogida normal.

### 1.9 Persistencia entre expediciones y partida
Se conserva: daño a prisiones, almas liberadas en el suelo, portales/puentes/segmentos abiertos, refugios y tiles descubiertos, progreso de objetivos. Partida guardada en JSON (`Application.persistentDataPath`). Menú: nueva partida (seed aleatoria o ingresada) / continuar.

### 1.10 Final
Comprar Despertar -> aparece el Núcleo (al final de una rama profunda elegida por seed). Romperlo -> pantalla de victoria con seed, tiempo, expediciones, prisiones rotas. Opción de seguir jugando para completar opcionales.

### Fuera de alcance (acabados)
Modelos, animaciones, VFX, audio, UI final (UI Toolkit), cosméticos/narrativa aleatoria, localización, build de plataforma.

---

## 2. Arquitectura y contratos (clave para paralelizar sin conflictos)

Riesgos de paralelismo: escena `.unity` de 5 MB, `.meta` generados por Unity, archivos compartidos. Reglas:

1. **Nadie edita la escena salvo Fase 0.** Todo se construye en runtime: `GameRoot` se crea con `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` si la escena es `SoulBlocks`.
2. **Sistemas autoregistrados**: cada sistema tiene su propio `[RuntimeInitializeOnLoadMethod] static void Boot() => GameRoot.Attach<X>();`. Cero líneas compartidas.
3. **Partials por feature**: `static partial class Balance` (un archivo `Balance.<Feature>.cs` por rama), `Hud.Register(order, Action)` para paneles IMGUI propios.
4. **Contratos congelados en Fase 1**: `RunState` (todos los campos compartidos), `GameEvents` (todos los eventos), `StatId` (enum completo), `MapLayout` (tipos de datos), interfaces `IRayTarget`, `ISoul`, `SoulRegistry`, `UnlockIds`, `Keys`, `SeededRng.Stream(seed, "nombre")` (sub-streams para que agregar un sistema no cambie el azar de otro). Cambiar un contrato después: solo el orquestador.
5. **Agentes no crean `.meta`** ni tocan `ProjectSettings/`, `Packages/` ni escenas. El orquestador importa en batchmode y commitea los `.meta`.
6. Lógica pura en `Core` con EditMode tests; MonoBehaviours finos en `Runtime`.

```
Assets/SoulBlocks/
  Core/        SoulBlocks.Core.asmdef     (RunState, GameEvents, Balance, Stats, SeededRng, MapLayout, MapGen, Economy, Prisons, Upgrades, Save)
  Runtime/     SoulBlocks.Runtime.asmdef  (GameRoot, Hud, Keys, Placeholder, SoulPlayer, SoulCamera, WisdomTreeVisual, vistas por sistema)
  Editor/      SoulBlocks.Editor.asmdef   (SoulBlocksSetup existente)
  Tests/EditMode, Tests/PlayMode          (asmdefs con nunit)
```

Reutilizar:
- `SoulPlayer.StepMovement(input, dt)` + `readInput=false` (`Assets/SoulBlocks/Runtime/SoulPlayer.cs`) para bots en PlayMode tests (patrón ya usado en las validaciones).
- `SoulPlayer.Supported` (capas 8|9): los tiles de camino van en capa 8.
- Detección de bordes de isla de `BuildRoad` (`Assets/SoulBlocks/Editor/BlockMechanicSetup.cs:171`, recuperable con `git show archive/block-mechanic:Assets/SoulBlocks/Editor/BlockMechanicSetup.cs`) para calcular los 3 anclajes de ramas. Anclajes probados: Norte `Vector3.forward` en x=-5, Este `Vector3.right` en z=-12, Oeste `Vector3.left` en z=-30. Árbol en (-12, 0, -3).
- `SoulBlocksSetup` "Validate Minimal Scene" se mantiene.
- Placeholders: `GameObject.CreatePrimitive` + material URP Lit cacheado por color (patrón `PickableBlock.SetSelected` con `MaterialPropertyBlock`), `TextMesh` para etiquetas, `LineRenderer` para previews.

---

## 3. Fases (obra gruesa)

Integración: rama `obra-gruesa`. Cada agente trabaja en worktree propio: `D:\Games\SoulBlocks-wt\<slug>` (`/mnt/d/Games/SoulBlocks-wt/<slug>`), creado con `git worktree add ... -b og/<slug> obra-gruesa`. (El aislamiento de worktree del Agent tool no sirve aquí: la sesión vive en otro repo.)

### Fase 0: Limpieza del terreno y replanteo (orquestador, ~45 min)
1. Usuario cierra Unity Editor (batchmode necesita el lock del proyecto).
2. `git restore Assets/Scenes/SoulBlocks.unity`; merge `feat-random-blocks` -> `develop`; tag `archive/block-mechanic`; crear `obra-gruesa`.
3. Escena: limpiada con script editor one-shot en batchmode (el backup `SoulBlocks_BeforeBlockMechanic.unity` también referenciaba `BlockGenerator`, no servía). Raíces finales: Directional Light, Main Camera, Player, Main Island, Wisdom Tree. Borrados `SoulBlocks_BeforeBlockMechanic.unity`, `SoulBlocks_Playable.unity`.
4. Borrar `BlockGenerator.cs`, `BlockInteraction.cs`, `PathSlot.cs`, `PickableBlock.cs`, `BlockMechanicSetup.cs`, `Prefabs/YellowCube.prefab`, materiales de bloque, `BLOCK-MECHANIC.md`, `Validation/block-mechanic-*.txt`.
5. Agregar `"com.unity.test-framework": "1.8.0"` a `Packages/manifest.json` (ya resuelto como builtin).
6. Batchmode import + compilación; 0 errores; commit. Copiar este plan a `docs/PLAN-OBRA-GRUESA.md`.

### Fase 1: Cimientos (1 agente Sonnet, ~1.5 h + 20 min integración)
`og/cimientos`: asmdefs (Core, Runtime, Editor, Tests), todos los contratos de la sección 2, `GameRoot` + autoregistro, `Hud`, `Keys`, `Placeholder`, `SeededRng`, `Stats` (base + modificadores por `StatId`), `MapLayout` + `MapLayoutFixtures.Simple()`, `MapBuilder` mínimo (tiles -> cubos capa 8), `HubZone` (trigger alrededor del árbol, eventos EnteredHub/LeftHub), hooks en `SoulPlayer` (`SpeedMultiplier` desde Stats, `TryMoveSupported`, `MoveUnchecked`, `Teleport`), `docs/CONTRATOS.md`. Tests: determinismo de `SeededRng`, Stats, EditMode "escena sin scripts faltantes". Al integrarse, **contratos congelados**.

### Fase 2: Estructura (5 agentes en paralelo, ~3 h + ~1.5 h integración)
| Rama | Alcance | Tests mínimos |
|---|---|---|
| `og/mapa` | Generador por seed (1.4), especialidades, validador + regeneración, anclajes desde bordes de isla, `MapBuilder` completo (giros, huecos, refugios, segmentos/puentes con candado ocultos), descubrimiento de tiles | Misma seed = mismo hash; 500 seeds válidas; seeds extremas; reachability |
| `og/expedicion` | Energía (drain, refugios, avisos, auto-retorno), carga/reserva, `SoulPickup : ISoul`, recogida básica 1.5 m, depósito en árbol, almas en el suelo con carga llena, inicio/fin de expedición | Energía/carga puras; PlayMode: salir, agotar, volver con carga |
| `og/prisiones` | Tiers, fases, HP persistente, `Forecast`, propiedades (resonante, núcleo expuesto), `PrisonView : IRayTarget`, recompensas garantizadas + hook de extra, ráfaga de almas, gates de etapa, portal de extremo | Tabla de impactos por tier/nivel; persistencia; resonancia |
| `og/habilidades` | Dash, Imán, Rayhit (1.6), previews, cargas, recarga por almas + tiempo; Rayhit en cadena detrás de `UnlockIds` | Destino de dash con borde/hueco/sólido; imán respeta capacidad; Rayhit multi-impacto con fake `IRayTarget` |
| `og/progresion` | Datos de ambos árboles (1.7), compra, gates, núcleo vs opcional, aplicación a Stats, tienda IMGUI en el árbol, beneficio estimado, hitos | Costos; gates; Stats tras compra; final requiere solo núcleo |

Orden de merge: mapa -> expedicion -> prisiones -> habilidades -> progresion (compilar + tests tras cada uno).

### Fase 3: Instalaciones (5 agentes en paralelo, ~2.5 h + ~1.5 h integración)
| Rama | Alcance |
|---|---|
| `og/azar-expedicion` | 3 objetivos + bonus, anomalía anunciada (3 tipos), densidad variable + pista en el árbol, mínimo garantizado |
| `og/azar-recompensas` | Extra al romper, bendiciones (3 elige 1, 4 con nodo), altares de riesgo |
| `og/mundo` | Efectos de Wisdom: portales, puentes, segmentos profundos, entrega en refugios, Brote, Revelación; etapas visuales en `WisdomTreeVisual` |
| `og/persistencia-ui` | Save/load JSON de `RunState`, menú nueva partida/continuar con seed, mapa (Tab) con descubiertos, pausa |
| `og/final` | Despertar -> Núcleo en rama profunda, expedición final, victoria + estadísticas, seguir jugando |

### Fase 4: Inspección de obra (2 agentes en paralelo + orquestador, ~2 h)
| Rama | Alcance |
|---|---|
| `og/simulacion` | Simulación headless (EditMode) con bot codicioso sobre 200 seeds: tiempo a primera compra, a cada Raíz, al final; ninguna seed bloqueada; reporte `Validation/balance.txt`; ajuste de `Balance` si algo cae fuera de rango |
| `og/humo` | PlayMode smoke e2e: bot sale, recoge, rompe T1, compra, usa portal, guarda/carga; con seed fija |

Orquestador: merge final `obra-gruesa` -> `develop` tras checklist verde.

---

## 4. Protocolo de orquestación

**Prompt base de cada agente** (Sonnet, background, lanzados juntos por fase):
- Worktree y rama exactos; leer `docs/PLAN-OBRA-GRUESA.md` (su sección) y `docs/CONTRATOS.md`.
- Solo tocar archivos de su feature + su `Balance.<Feature>.cs`. No escena, no `ProjectSettings`, no `Packages`, no `.meta`, no contratos (si falta algo: pedirlo en el reporte).
- Sin animaciones ni modelos: primitivas, colores, `TextMesh`, `LineRenderer`, IMGUI vía `Hud.Register`.
- Lógica pura en Core con tests NUnit; no puede compilar: escribir C# conservador, estilo del repo (namespace `SoulBlocks`, clases `sealed`), comentarios `ponytail:` en atajos deliberados, sin guion largo en textos.
- Commits convencionales en su rama. Reporte final: archivos, tests, supuestos, pedidos de contrato.

**Ciclo de integración (por rama, en serie)**:
```bash
UNITY="/mnt/c/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe"
git -C /mnt/d/Games/SoulBlocks merge --no-ff og/<slug>
"$UNITY" -batchmode -nographics -quit -projectPath 'D:\Games\SoulBlocks' -logFile 'D:\Games\SoulBlocks\Logs\og-compile.log'
grep -E "error CS" /mnt/d/Games/SoulBlocks/Logs/og-compile.log
"$UNITY" -batchmode -nographics -projectPath 'D:\Games\SoulBlocks' -runTests -testPlatform EditMode -testResults 'D:\Games\SoulBlocks\Logs\editmode.xml' -logFile 'D:\Games\SoulBlocks\Logs\og-editmode.log'
"$UNITY" -batchmode -projectPath 'D:\Games\SoulBlocks' -runTests -testPlatform PlayMode -testResults 'D:\Games\SoulBlocks\Logs\playmode.xml' -logFile 'D:\Games\SoulBlocks\Logs\og-playmode.log'
git -C /mnt/d/Games/SoulBlocks add -A Assets && git commit -m "chore(meta): import og/<slug>"
```
Falla: errores cortos se arreglan en `obra-gruesa`; errores de lógica vuelven al agente con `SendMessage` (mismo contexto), corrige en su worktree, re-merge. Al cerrar cada fase: borrar worktrees, crear los de la siguiente desde `obra-gruesa` actualizada.

---

## 5. Verificación

- Por merge: 0 `error CS`, EditMode y PlayMode en verde (XML en `Logs/`).
- Fase 2: 500 seeds válidas y deterministas; tabla de impactos por tier coincide con 1.5.
- Fase 4: `Validation/balance.txt` sin seeds bloqueadas; smoke e2e verde; "Validate Minimal Scene" sigue en PASS.
- Playtest del usuario (15-20 min, seed fija) con las 3 señales del chat:
  1. ¿Podés explicar qué querés conseguir después y por qué?
  2. ¿Una mejora importante cambió tu recorrido o tus acciones?
  3. ¿Seguís interesado después de descubrir la ruta más rentable?
  Más: moverse, recoger y dashear se sienten bien sin mejoras; energía y carga frenan en momentos distintos.

## Estimación total
Unas 10-12 h de reloj: Fase 0 45 min, Fase 1 ~2 h, Fase 2 ~4.5 h, Fase 3 ~4 h, Fase 4 ~2 h. La integración en serie es el cuello de botella (~10 min de Unity batchmode por merge).
