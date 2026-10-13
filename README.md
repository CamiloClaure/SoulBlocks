# SoulBlocks

Incremental de exploración en Unity 6000.6.0f1 (URP, Input System). Salís del árbol central por caminos generados por seed, recogés almas, picás prisiones cristalinas, volvés, mejorás y el mundo se transforma hasta despertar al Núcleo.

Estado: **obra gruesa**. Todas las mecánicas funcionan de punta a punta con placeholders (primitivas, colores, IMGUI). Sin animaciones ni modelos nuevos. Diseño completo y plan de construcción en [`docs/PLAN-OBRA-GRUESA.md`](docs/PLAN-OBRA-GRUESA.md); contratos entre sistemas en [`docs/CONTRATOS.md`](docs/CONTRATOS.md).

## Jugar

1. Abrí la carpeta en Unity Hub con **Unity 6000.6.0f1**.
2. Abrí `Assets/Scenes/SoulBlocks.unity` y dale Play.
3. Menú inicial: continuar, nueva partida (seed aleatoria o ingresada) o jugar con el mundo actual.

| Acción | Control |
|---|---|
| Moverse | WASD / flechas |
| Girar cámara | Mantener click derecho y mover el mouse |
| Zoom de cámara | Rueda |
| Dash | Shift |
| Imán de almas | Q |
| Picar cristal | Click izquierdo al lado del cristal |
| Interactuar (tienda del árbol, altares, entrega en refugios) | E |
| Mapa | Tab (mantener) |
| Pausa (guardar, copiar seed, nueva partida) | Esc |

La partida se guarda sola al terminar cada expedición, al comprar y al salir.

## Estructura

- `Assets/SoulBlocks/Core/`: lógica pura sin MonoBehaviour (mapa por seed, energía, carga, prisiones, árboles de mejoras, azar, guardado, simulación). Tunables en `Balance.*.cs`.
- `Assets/SoulBlocks/Runtime/`: sistemas en escena. Cada uno se registra solo en `GameRoot` (no hay que tocar la escena).
- `Assets/SoulBlocks/Tests/`: EditMode y PlayMode (incluye un smoke test que juega una partida completa).
- `tools/CoreHarness/`: compila `Core/` fuera de Unity y valida mapas en segundos.
- `Validation/balance.txt`: ritmo de una partida simulada sobre 200 seeds (lo escribe un test EditMode).

## Tests

Con el Editor cerrado (batchmode necesita el lock del proyecto):

```bash
UNITY="C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
"$UNITY" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml
"$UNITY" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml
```

Loop rápido para lógica de `Core` (requiere .NET SDK; ~2 s para 200 seeds):

```bash
cd tools/CoreHarness
dotnet run -c Release -- 500            # anclajes sintéticos
dotnet run -c Release -- 500 realistic  # anclajes fuera de grilla
```

`JsonUtility` y otras llamadas internas del motor compilan en el harness pero fallan al ejecutarse fuera de Unity.

## Origen

Proyecto mínimo extraído de CamiloClaure/Lighting-Blocks (isla, Wisdom Tree, jugador y animaciones). La mecánica anterior de bloques quedó archivada en el tag `archive/block-mechanic`. Los assets de origen conservan su autoría y términos de uso.
