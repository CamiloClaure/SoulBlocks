# Cristales de picado

El prefab `Assets/SoulBlocks/Prefabs/Crystals/Crystal.prefab` tiene una instancia guardada
en `Assets/Scenes/SoulBlocks.unity`, llamada `Crystal Yellow - Tree`, junto al árbol.
No se genera al jugar. Su caja mide 0,55 × 0,90 × 0,55 m: la mitad de la altura
del controlador del jugador (1,80 m).

Acércate y mantén el clic izquierdo apuntando hacia el cristal para picar en bucle.
Al soltar, termina el golpe en curso sin iniciar otro. Cada picado dura
0,55 s; el volumen de contacto se activa entre 0,22 y 0,32 s. Cada cristal recibe
como máximo un golpe por picado, aunque tenga varios colliders. Al impactar, jugador
y cristal se ponen rojos brevemente. El amarillo pequeño desaparece al tercer golpe.
Las prisiones del mapa no usan este volumen: el mismo clic pica la más cercana por alcance
(`PickAbility`), a su propio ritmo (`StatId.PickInterval`).

Mientras mantienes el clic, la dirección de picado queda fijada desde el primer golpe.
Puedes girar la cámara y moverte: A/D desplazan al jugador de lado respecto a esa
dirección, sin girar el cuerpo ni los siguientes golpes. El volumen de impacto acompaña
la posición del jugador. Suelta y vuelve a pulsar para elegir una dirección nueva.

## Color, tamaño y vida

En el componente `BreakableCrystal`, `Color` define la vida base:
Yellow = 3, Blue = 5, Green = 6, Celeste = 7, Red = 9, Violet = 8. `Size` multiplica dimensiones y vida:
Small ×1, Medium ×2, Large ×3. El destello rojo de impacto no cambia la vida máxima.
Configura estas propiedades antes de Play. Escalar manualmente el Transform no altera la vida;
usa `Size` para cambiar ambos valores juntos.

La zona de prueba contiene tres tamaños de los cinco colores especiales. Los azules
empiezan ocultos; usa el bloque azul para revelarlos. Las pasivas y bombas pueden
añadir daño, por lo que el amarillo pequeño puede romperse antes de tres picados.
Más detalles en [BLOQUES-ESPECIALES.md](BLOQUES-ESPECIALES.md).

## Reemplazar por Blender

Abre el prefab, elimina el hijo `Placeholder - replace with Blender model` dentro de
`Model` y coloca allí tu modelo importado. Ajusta su transformación local para ocupar
una caja unitaria centrada en su origen; `Model` aplica el tamaño y la elevación desde
el suelo. Conserva `Model`, el componente `BreakableCrystal` y el BoxCollider del objeto
raíz. Usa materiales URP con `_BaseColor` para conservar el cambio de color al golpear.
No hacen falta scripts en el modelo importado.

El jugador tiene `CrystalMiningAttack`, un hijo `Mining Hitbox` y un pico provisional
en la mano derecha. La animación de picado se calcula sobre torso y brazo después del
Animator, sin requerir un clip externo. Selecciona el jugador para ver el alcance en
los gizmos de escena. Esta primera versión reinicia el cristal al recargar la escena;
su ruptura todavía no se incluye en el guardado de la partida.
