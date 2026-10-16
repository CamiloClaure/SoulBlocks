# Almas y arena de poderes

En WorldBuilding las almas recolectables son amarillas y tienen núcleo luminoso y halo translúcido pulsante, sin colas ni estelas. El prefab es `Assets/SoulBlocks/Prefabs/SpiritGuiding/WanderingSpirit.prefab`; también lo usan los árboles nutribles.

La población fuera de los caminos se coloca cada 6 metros, en dos franjas a cada lado. Respeta todos los caminos dibujados y tiene un límite de 384 almas para esta escena. `PathPopulation` permite ajustar separación, número de franjas y límite.

Con la antorcha encendida, las almas se acercan. Cuando la llevas en la mano, siguen al jugador por detrás en un pequeño grupo flotante, a distintas distancias, sin girar a su alrededor. Puedes caminar y picar acompañado por ellas. F coloca/recoge la antorcha; T enciende/apaga su luz. Al devolverla al altar, las almas se entregan y suman energía espiritual como antes.

Los árboles nutribles son ahora rectángulos de 1,4 × 4,2 × 0,3 m. Empiezan grises y permanecen amarillos una vez revividos durante toda la partida. Su límite de generación se mantiene: tres almas propias simultáneas por árbol, una cada cinco segundos. El reinicio explícito del laboratorio o recargar la escena restablece el prototipo.

## Nuevo recorrido

`Camino de prueba de poderes` sale lateralmente desde el inicio del camino principal y termina en `Arena de prueba · 72 cristales`. Se puede editar con los mismos puntos de AuthoredPath que el camino original. Sus cristales están colocados en la escena: cinco colores y tres tamaños. Cuatro árboles nutribles rodean la arena.

Las mejoras están en `Altar de bloques y talentos`, delante del nuevo árbol blanco. Pulsa E: cinco símbolos circulares salen del suelo y se abre el menú. Al cerrar se ocultan de nuevo. Las mejoras siguen siendo gratuitas en la prueba. Selecciona con la rueda y activa con clic derecho. `Restaurar cristales de prueba` repone tanto los 15 del laboratorio original como los 72 de la arena.

También hay hasta 64 almas repartidas por el vacío, fuera de las franjas que acompañan a los caminos. Conservan la atracción de la antorcha.

