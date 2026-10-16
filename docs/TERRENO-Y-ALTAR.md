# Terreno editable, árbol central y altar

Todo está en `Assets/Scenes/SoulBlocks.unity`.

## Moldear el suelo

Selecciona **Main Island > Island Terrain** en la jerarquía, o usa **Tools > SoulBlocks >
Seleccionar terreno de la isla**. En el Inspector, las herramientas de Terrain permiten
elevar/bajar, suavizar y pintar el suelo. El terreno mide 48 × 48 m, recortado como una isla
irregular de unos 40 m de diámetro, con resolución de alturas 513 y tres capas: Meadow, Soil y
Tree Clearing.

Sus datos están en `Assets/SoulBlocks/Terrain/IslandTerrainData.asset` (las capas, en la misma
carpeta). No se reconstruye al pulsar Play: los cambios de relieve y pintura que hagas fuera de
Play quedan en el proyecto. Los caminos son objetos `AuthoredPath` aparte: si cambias mucho el
relieve junto a ellos, ajusta sus puntos para que el suelo no tape el camino.

Todo lo que cuelga de **Main Island** cuenta como la isla: ahí no se gasta energía y al salir
empieza una expedición. Fuera de la isla no hay terreno; los caminos salen hacia el vacío. La
base rocosa (**Island Rock Base**) es decorativa. Los huecos del Terrain delimitan la isla y se
editan con las herramientas de agujeros. El Terrain tiene su propio collider en la capa Ground.
**World Decoration** tiene apartados para vegetación, rocas y agua futuros.

## Árbol blanco

**CentralSpiritTree** ocupa el centro de la isla. Es un bloque blanco de 4 × 10 × 4 m; su prefab
es `Assets/SoulBlocks/Prefabs/SpiritGuiding/CentralSpiritTree.prefab`.

Reemplaza el contenido de **Model** por el FBX y conserva la raíz, el componente
`CentralSpiritTree` y el collider (ajústalo al modelo final). Puedes asignar un Animator con un
parámetro entero `Stage` para las etapas del árbol (Raíz I a Despertar).

## Altar de bloques y talentos

Acércate al pequeño altar delante del árbol (**TreeUpgradeAltar**) y pulsa **E**. Los cinco
símbolos circulares salen del suelo durante 0,85 s y se abre el menú de mejoras. Al cerrar con
**E / Esc** vuelven a ocultarse. Las habilidades y pasivas desbloqueadas siguen funcionando
fuera del menú.

En el altar puedes mejorar el pico con fragmentos, mejorar los bloques y la ropa, restaurar los
cristales de prueba o abrir **Talentos del árbol** (el tablero de mejoras con almas y Wisdom).
Las mejoras de bloques siguen siendo gratuitas en la prueba; los talentos mantienen sus costes y
requisitos.

Prefab: `Assets/SoulBlocks/Prefabs/SpecialBlocks/TreeUpgradeAltar.prefab`. Tiene un **Model**
para el altar y cinco símbolos provisionales con anillo y marcas de color, que se pueden
sustituir conservando las referencias `Symbols` del componente `TreeUpgradeAltar`.

El altar de la antorcha (**Torch Altar Area**) es distinto: ahí devuelves la antorcha y se
entregan las almas.

## Mejorar velocidad con el tablero

En el altar pulsa E y luego **Mejorar velocidad en el árbol de talentos**: se abre el tablero
con Velocidad seleccionado. Su requisito es Energía nivel 1; el botón Ver Energía lleva a ese
nodo. Velocidad empieza costando 50 almas, suma 0,48 m/s por nivel y admite 6 niveles, con
costes crecientes (valores en `Balance.Progression.cs`). El panel muestra la velocidad actual,
incluidos los efectos activos.
