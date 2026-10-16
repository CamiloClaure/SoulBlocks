# Almas y arena de poderes

Las almas son amarillas y tienen núcleo luminoso y halo translúcido pulsante, sin colas ni estelas. El prefab es `Assets/SoulBlocks/Prefabs/SpiritGuiding/WanderingSpirit.prefab`; también lo usan los árboles nutribles.

**Path Population** (`PathPopulation`) las coloca junto a todos los caminos activos, en dos franjas a cada lado: un puesto cada 6 m a densidad 1. La densidad es `StatId.SoulDensity`: empieza en 0,5 (un puesto cada 12 m) y **Abundancia** la sube. Al empezar cada expedición se reponen los puestos vacíos; no se borra ninguna alma. Se pueden ajustar separación, número de franjas y límite. Los caminos detrás de un desbloqueo se pueblan cuando se abren.

Con la antorcha encendida, las almas se acercan. Cuando la llevas en la mano, siguen al jugador por detrás en un pequeño grupo flotante, a distintas distancias, sin girar a su alrededor. Puedes caminar y picar acompañado por ellas. Ver [ANTORCHA-Y-ALMAS.md](ANTORCHA-Y-ALMAS.md).

Los árboles nutribles son ahora rectángulos de 1,4 × 4,2 × 0,3 m. Empiezan grises y permanecen amarillos una vez revividos durante toda la partida. Su límite de generación se mantiene: tres almas propias simultáneas por árbol, una cada cinco segundos. El reinicio explícito del laboratorio o recargar la escena restablece el prototipo.

## Arena de poderes

**Power Test Path** (la rama de almas) sale lateralmente desde el inicio del camino principal y termina en **Power Arena**, que es también un refugio. Se puede editar con los mismos puntos de AuthoredPath que el camino original. Sus cristales están colocados en la escena: cinco colores y tres tamaños. Cuatro árboles nutribles rodean la arena.

Las mejoras están en el altar delante del árbol blanco (ver [TERRENO-Y-ALTAR.md](TERRENO-Y-ALTAR.md)). `Restaurar cristales de prueba` repone tanto los 15 del laboratorio como los 72 de la arena.

También hay almas repartidas por el vacío (64 a densidad 1), fuera de las franjas que acompañan a los caminos. Conservan la atracción de la antorcha.

