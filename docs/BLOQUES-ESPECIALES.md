# Bloques especiales: prueba de nutrición

En WorldBuilding están disponibles los cinco bloques. Las teclas 1, 2, 3, 4 y 5 activan directamente amarillo, azul, verde, celeste y rojo. La rueda permite consultar la descripción; el clic derecho ya no activa poderes. Todas las pasivas funcionan aunque otro bloque esté seleccionado. E en el altar junto al árbol abre las mejoras del prototipo.

Mantener clic izquierdo repite el picado. El cuerpo, el pico y su volumen de contacto siguen la dirección horizontal de la cámara durante el golpe. Puedes seguir caminando y desplazándote de lado.

| Color | Activo | Pasivo |
| --- | --- | --- |
| Amarillo | Ilumina una zona y revive todos los árboles nutribles dentro del radio | Un golpe acertado puede dar luz al jugador durante 5 s; al acercarse a 3 m de un árbol gris, lo revive |
| Azul | Pulsa inmediatamente y cada segundo durante su duración; sortea la nutrición de cada cristal cercano sin dañarlo | Cada cristal alcanzado por el pico puede recibir un aura azul |
| Verde | Zona de velocidad para movimiento, picado y recargas | Cada 10 golpes inicialmente, posibilidad de velocidad temporal |
| Celeste | Ayudantes temporales que pican y recogen fragmentos | Cada 10 golpes inicialmente, posibilidad de invocar un ayudante |
| Rojo | Bomba con cuenta atrás de 2,5 s | Posibilidad de explosión por golpe; los ayudantes hacen la mitad de daño |

## Amarillo y árboles

Hay once árboles de prueba colocados en WorldBuilding: dos junto al árbol principal, tres a lo largo del camino, dos en el laboratorio y cuatro en la nueva arena de poderes. Son rectángulos grises que pasan a amarillo cuando están vivos. El activo mantiene nutridos los árboles de su zona; la luz pasiva se mueve con el jugador. Los árboles revividos permanecen encendidos durante la partida; no se apagan al terminar el activo o la luz pasiva.

Cada árbol vivo atrae espíritus a 7 m y genera uno cada 5 s, con un máximo de tres espíritus propios presentes a la vez. La antorcha tiene prioridad de atracción: permite recogerlos y llevarlos al altar. Cuando esas almas se entregan o desaparecen, el árbol puede volver a generar. El reinicio explícito del laboratorio devuelve los árboles a su estado gris.

Prefab: `Assets/SoulBlocks/Prefabs/SpiritGuiding/NourishableTree.prefab`. Duplica instancias y colócalas donde tengan sentido en tu mundo. No se generan árboles automáticamente. En `NourishableTree` se ajustan el intervalo, el máximo y el radio de atracción. Para Blender, sustituye el rectángulo dentro de `Model`; conserva el componente y el collider de la raíz. Un Animator dentro de Model puede tener un parámetro bool `Alive` para las futuras animaciones de vida.

## Azul y recompensa

Los cristales ya son visibles; azul no invierte los colores ni revela cristales ocultos del laboratorio. El aura es un anillo azul luminoso que permanece hasta romper o reiniciar el cristal. La nutrición no se acumula con varios golpes ni modifica la vida.

Dos sorteos separados:

1. Golpear o recibir un pulso puede preparar el cristal: 20% inicialmente, hasta 50% con ropa.
2. Romper un cristal preparado tiene 50% de probabilidad de dar fragmentos extra.

El extra inicial es +50%, y aumenta 25 puntos por nivel de azul, hasta +150%. Se redondea el extra hacia arriba: un cristal pequeño normalmente suelta 3 fragmentos y, con el bonus inicial, suelta 5. Fallar el segundo sorteo mantiene la recompensa normal. Solo se sortea y se sueltan fragmentos una vez; hay que recogerlos para sumarlos al inventario.

## Mejoras y límites

- Amarillo, azul y verde: radio de 5 a 9 m; duración del activo de 8 a 16 s.
- Verde y celeste: intervalo pasivo de 10, 8, 6, 4 y 2 golpes.
- Verde: velocidad ×1,35 a ×1,95; pasiva de 1 a 3 s.
- Celeste: 1 a 3 ayudantes durante 12 a 28 s; máximo 4 activos.
- Rojo: daño de 4 a 12; radio de 2 a 4,4 m.
- Recarga de activos: 5 s, salvo celeste (10 s). Verde acelera las recargas.
- Ropa: mejora la probabilidad de activación del 20% al 50%. El sorteo final del bonus azul permanece en 50%.

Los golpes al aire no activan pasivas. Un golpe que toca varios cristales cuenta una vez para amarillo, rojo, verde y celeste, pero cada cristal tiene su propio sorteo azul. Los ayudantes pueden nutrir cristales y revivir árboles cercanos por sus golpes; no invocan más ayudantes. Las explosiones no generan nuevas activaciones pasivas.

Restaurar el laboratorio también apaga los árboles y retira sus almas generadas. Los cambios de nutrición y las almas del prototipo no se guardan entre sesiones.
