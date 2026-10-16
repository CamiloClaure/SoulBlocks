# SoulBlocks

Incremental de exploración en Unity 6000.6.0f1 (URP, Input System). Desde la isla del árbol
central salís por caminos dibujados a mano, guiás almas con la antorcha, picás cristales y
prisiones, volvés, mejorás y el mundo se transforma hasta despertar al Núcleo.

Estado: **obra gruesa**. Las mecánicas funcionan de punta a punta con placeholders
(primitivas, colores, IMGUI) y algunos modelos (el mago, la antorcha, el pico).

## Jugar

1. Abrí la carpeta en Unity Hub con **Unity 6000.6.0f1**.
2. Abrí `Assets/Scenes/SoulBlocks.unity` (la única escena) y dale Play.
3. Menú inicial: continuar la partida guardada o empezar una nueva.

| Acción | Control |
|---|---|
| Moverse | WASD / flechas |
| Girar cámara | Mouse |
| Mostrar el cursor (botones del HUD) | Alt (mantener; también se libera en menús) |
| Zoom de cámara | Teclas + / − |
| Centrar cámara detrás del jugador | Click de la rueda |
| Dash | Shift |
| Picar (cristales y prisiones) | Mantener click izquierdo, hacia el centro de la pantalla |
| Activar bloque especial | 1 a 5 (amarillo, azul, verde, celeste, rojo); rueda: ver descripción |
| Antorcha | F: tomar, colocar o devolver al altar · T: encender o apagar |
| Hablar con un espíritu | R |
| Interactuar (altar del árbol, altares de riesgo, entrega en refugios) | E |
| Mapa | Tab (mantener) |
| Pausa (guardar, controles, nueva partida) | Esc |
| Pantalla completa | F1 |
| Cambiar modelo del personaje (mago / clásico) | F2 |

La partida se guarda sola al terminar cada expedición, al comprar y al salir.

## El ciclo

- **Isla**: el árbol blanco, el altar de mejoras (E) y el altar de la antorcha. Mientras estás
  sobre la isla no gastás energía.
- **Expediciones**: al pisar un camino fuera de la isla empieza una expedición y la energía
  baja. Los refugios (incluidos el laboratorio y la arena) la pausan. Si se agota, volvés al árbol.
- **Ramas**: el camino principal lleva al laboratorio de bloques (rama de prisiones), el camino
  de poderes a la arena (rama de almas) y el camino de exploración sale hacia el oeste. Raíz II
  abre el puente entre ramas, Raíz III el tramo profundo después del laboratorio y Despertar el Núcleo.
- **Almas**: deambulan junto a los caminos. La antorcha encendida las llama; te siguen hasta
  el altar (o un refugio con Entrega) y ahí suman a la reserva. Carga, Alcance de la antorcha y
  Abundancia mejoran cuántas te siguen, desde qué distancia y cuántas aparecen.
- **Pico**: el mismo golpe pica cristales (sueltan fragmentos) y prisiones. Su color
  (amarillo → rojo) es el nivel que piden las prisiones: T1 amarillo, T2 azul, T3 verde, T4
  celeste, Núcleo rojo. Se mejora con fragmentos en el altar del árbol.
- **Prisiones**: al romperse liberan almas y Wisdom; las de final de rama ofrecen una bendición
  y, con Raíz I, abren un portal de vuelta a la isla.

Guías de diseño y de edición en `docs/`:

- [Construir caminos y la expedición](docs/WORLD-BUILDING.md)
- [Terreno, árbol central y altar](docs/TERRENO-Y-ALTAR.md)
- [Antorcha y almas](docs/ANTORCHA-Y-ALMAS.md), [almas y arena](docs/ALMAS-Y-ARENA.md)
- [Bloques especiales](docs/BLOQUES-ESPECIALES.md), [cristales](docs/CRISTALES.md),
  [fragmentos](docs/FRAGMENTOS-DE-CRISTAL.md), [pico y balance](docs/PICO-Y-BALANCE.md)
- [Diálogos de espíritus](docs/DIALOGOS-DE-ESPIRITUS.md), [dirección de la progresión](docs/DISENO-DE-PROGRESION.md)
- Históricos: [plan de la obra gruesa](docs/PLAN-OBRA-GRUESA.md) y [contratos](docs/CONTRATOS.md),
  de la etapa con mapa generado por seed.

## Estructura

- `Assets/Scenes/SoulBlocks.unity`: el mundo entero, construido a mano.
- `Assets/SoulBlocks/Core/`: lógica pura sin MonoBehaviour (energía, prisiones, mejoras, azar,
  guardado, el `MapLayout`). Tunables en `Balance.*.cs`.
- `Assets/SoulBlocks/Runtime/`: sistemas en escena, uno por carpeta de dominio (`Map`,
  `Expedition`, `Prisons`, `Crystals`, `SpiritGuiding`, `SpecialBlocks`, `UI`...). Los sistemas
  globales se registran solos en `GameRoot`; `Map/SceneLayout` arma el mapa desde los caminos
  (`AuthoredPath`) y los marcadores (`PrisonSite`, `ShelterSite`, `PortalSite`,
  `RiskAltarSite`, `UnlockGate`) de la escena.
- `Assets/SoulBlocks/Editor/`: herramientas de edición (puntos de camino, terreno, importador del mago).
- `Assets/SoulBlocks/{Materials,Prefabs,Meshes,Shaders,Terrain}/`: assets, en subcarpetas por dominio.
  `Resources/` solo guarda lo que el código carga por nombre.
- `Assets/SoulBlocks/Tests/`: EditMode y PlayMode, en las mismas carpetas de dominio
  (incluye un smoke test que juega una partida completa).

Convenciones: nombres de archivos, carpetas, código y objetos de escena en inglés y sin
espacios en archivos; textos del juego y documentación en español. Formato de C# en
`.editorconfig` (aplicable con `dotnet format whitespace --folder Assets/SoulBlocks`).

## Tests

Con el Editor cerrado (batchmode necesita el lock del proyecto):

```bash
UNITY="C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
"$UNITY" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml
"$UNITY" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml
```

Las capturas que toman algunos tests quedan en `Logs/Captures/` (fuera del repo).

## Origen

Proyecto mínimo extraído de CamiloClaure/Lighting-Blocks (isla, jugador y animaciones). La
mecánica anterior de bloques quedó archivada en el tag `archive/block-mechanic`. Los assets de
origen conservan su autoría y términos de uso.
