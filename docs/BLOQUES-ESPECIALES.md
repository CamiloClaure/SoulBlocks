# Zona de prueba de bloques especiales

La escena SoulBlocks contiene 15 cristales colocados cerca del árbol: amarillo, azul,
verde, celeste y rojo, cada uno pequeño, mediano y grande. Los tres azules empiezan
ocultos. Los cinco bloques del árbol están desbloqueados y sus pasivas funcionan
siempre, independientemente del seleccionado. El morado queda pendiente.

## Controles

- Rueda: seleccionar bloque en la barra inferior.
- Clic derecho: usar su activo, con recarga individual.
- Clic izquierdo sostenido: picar; conserva la dirección inicial y permite desplazarse.
- Mouse: girar cámara. Clic central: recentrar. Teclas + / −: distancia de cámara.
- E cerca del árbol: mejorar bloques, ropa o restaurar los 15 cristales.

Las mejoras son gratuitas en este prototipo. El menú también permite abrir las
mejoras originales del árbol. Los niveles se incluyen en el guardado de partida.

## Efectos

| Color | Activo | Pasivo |
| --- | --- | --- |
| Amarillo | Ilumina los tramos oscuros cercanos | Tirada de probabilidad cada 1,5 metros caminados |
| Azul | Invierte colores de superficies y cristales de la zona y revela los ocultos | Tirada al iluminar un tramo que estaba oscuro |
| Verde | Zona que acelera movimiento, picado y recargas | Tirada cada 10 golpes acertados; mejora de velocidad durante 1 segundo inicialmente |
| Celeste | Invoca ayudantes temporales que pican, recogen y entregan almas al árbol | Tirada cada 10 golpes acertados para invocar un ayudante |
| Rojo | Bomba cúbica que titila, muestra cuenta atrás de 2,5 segundos y explota | Tirada por golpe acertado para dañar cristales cercanos inmediatamente |

Un picado que alcanza varios cristales cuenta una sola vez. Los golpes al aire no
cuentan. Los contadores se conservan entre pulsaciones. Amarillo y azul también
funcionan con el desplazamiento de los ayudantes. Estos reciben la velocidad verde
y pueden activar rojo con la mitad del daño; no generan más ayudantes. El daño de
explosiones no activa otras explosiones.

## Valores iniciales y mejoras

Cinco niveles por bloque. La ropa aumenta la probabilidad del 20 % al 50 % en cinco
mejoras de 6 puntos porcentuales. Mejorar un bloque no aumenta esa probabilidad.

- Verde/celeste: intervalo de 10, 8, 6, 4 y 2 golpes según nivel.
- Rojo: daño de 4 a 12 y radio de 2 a 4,4 metros.
- Amarillo/azul/verde: radio de 5 a 9 metros y duración de 8 a 16 segundos.
- Verde: velocidad ×1,35 a ×1,95; pasiva de 1 a 3 segundos.
- Celeste: 1 a 3 ayudantes por activo, duración de 12 a 28 segundos; límite de 4 simultáneos.
- Recarga: 5 segundos, salvo celeste (10 segundos). Verde acelera las recargas.

La iluminación e inversión de superficies se prueban en los caminos oscuros
colocados junto al árbol; todavía no extienden ese comportamiento a todos los
caminos procedurales del mapa. La bomba sigue siendo un cubo provisional para
sustituirlo después por el efecto de energía.

## Archivos

`Core/SpecialBlocks/BlockRules.cs` contiene los valores. Los efectos y UI están en
`Runtime/SpecialBlocks/`. Los cinco prefabs están en `Prefabs/SpecialBlocks/`;
el cristal reutilizable está en `Prefabs/Crystals/Crystal.prefab`.
Todas estas rutas parten de `Assets/SoulBlocks/`.

`SpecialBlockLabSetup.Build` reconstruye la zona en el editor y reemplaza sus
objetos anteriores. No ejecutarlo sobre una zona personalizada sin guardar copia.
