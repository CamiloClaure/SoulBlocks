# Terreno editable, árbol central y altar

Abre `Assets/Scenes/WorldBuilding.unity`.

## Moldear el suelo

Selecciona **Isla central · terreno editable** en la jerarquía, o usa **Tools > SoulBlocks > Seleccionar terreno editable**. En el Inspector, las herramientas de Terrain permiten elevar/bajar, suavizar y pintar el suelo. El terreno mide 48 × 48 m, recortado como una isla irregular de unos 40 m de diámetro, tiene resolución de alturas 513 y tres capas: Pradera, Tierra y Claro del árbol.

Es una base guardada en `Assets/SoulBlocks/WorldBuilding/Terrain/TerrenoEditable.asset`. No se reconstruye al pulsar Play. Los cambios de relieve y pintura que hagas fuera de Play se conservan como edición del proyecto. Los caminos dibujados siguen siendo objetos AuthoredPath independientes: si cambias mucho el relieve junto a ellos, ajusta sus puntos para mantener la continuidad y evitar que el suelo tape el camino.

El suelo de baldosas y el suelo lateral están desactivados. Fuera de la isla no hay terreno: los caminos salen hacia el vacío y conservan sus plataformas de prueba al final. La base rocosa bajo la isla es decorativa; la superficie superior sigue siendo moldeable. Los huecos del Terrain delimitan la isla y se pueden editar con las herramientas de agujeros. El Terrain tiene su propio collider en la capa de suelo. El grupo **Decoración del mundo** tiene apartados para vegetación, rocas/detalles y agua futuros. No se han añadido lago ni hierba animada todavía. Terrain admite detalles, árboles y texturas; las futuras animaciones dependerán de los modelos/materiales que añadamos.

## Árbol blanco

El nuevo árbol ocupa el centro del área principal. Es un bloque blanco de 4 × 10 × 4 m. Su prefab es `Assets/SoulBlocks/Prefabs/SpiritGuiding/CentralSpiritTree.prefab`.

Reemplaza el contenido de **Model** por el FBX y conserva la raíz, `CentralSpiritTree` y el collider; ajusta este último al modelo final. Puedes asignar un Animator y un parámetro entero `Stage` para representar las etapas del árbol. La lógica del árbol de talentos, sus compras y sus efectos se conserva y ahora apunta a este árbol. El antiguo modelo está archivado y desactivado en la escena.

## Altar de bloques y talentos

Acércate al pequeño altar delante del árbol y pulsa **E**. Los cinco símbolos circulares salen del suelo durante 0,85 s y luego se abre el menú de mejoras. Al cerrar con **E / Esc**, vuelven a ocultarse. Las habilidades y pasivas ya desbloqueadas siguen funcionando fuera del menú.

En el altar puedes mejorar los bloques, mejorar la ropa, restaurar los cristales de prueba o abrir **Talentos del árbol**, que conserva el tablero anterior. Las mejoras de bloques siguen siendo gratuitas para el test; los talentos mantienen sus costes y requisitos.

Prefab: `Assets/SoulBlocks/Prefabs/SpecialBlocks/TreeUpgradeAltar.prefab`. Tiene un **Model** para el altar y cinco símbolos provisionales con anillo y marcas de color. Estos se pueden sustituir visualmente conservando las referencias `Symbols` del componente `TreeUpgradeAltar`.

El altar de la antorcha continúa siendo el lugar donde depositas las almas, junto al árbol; es distinto del pequeño altar de mejoras.


## Mejorar velocidad con el árbol antiguo

En el altar pulsa E y luego **Mejorar velocidad en el árbol de talentos**. Se abre el tablero antiguo con Velocidad seleccionado. Su requisito sigue siendo Energía nivel 1; el botón Ver Energía lleva directamente a ese nodo. Usa las almas entregadas con la antorcha para comprarlos. Velocidad comienza costando 50 almas, aumenta 0,48 m/s por nivel y admite 6 niveles, con costes crecientes. Los valores viven en Balance.Progresion.cs. El panel muestra la velocidad actual, incluyendo los efectos activos.
