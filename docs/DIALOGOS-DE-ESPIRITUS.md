# Espíritus de colores y diálogo provisional

Junto al árbol encontrarás **Dialogue Spirits**, cinco espíritus de colores.
Son esferas luminosas amarilla, azul, verde, celeste y roja. Estos NPC de prueba se
quedan en su lugar; la antorcha no los recolecta.

Acércate y pulsa **R** para hablar. La caja inferior muestra el nombre con su color,
el texto y el número de frase. **R, Enter o Espacio** avanzan; **Esc** cierra.
También puedes usar los botones Continuar/Terminar y Cerrar. La conversación pausa
el mundo, libera el cursor y bloquea el movimiento; al cerrar restaura el juego.
El texto largo se puede desplazar dentro de la caja.

## Dar diálogo a un duplicado

1. Fuera de Play, selecciona un espíritu y duplícalo con Ctrl+D.
2. Mueve el duplicado.
3. En el componente **Spirit Dialogue**, cambia **Speaker Name**.
4. En **Diálogo: edita cada frase en esta instancia**, despliega **Lines**.
5. Modifica **Text** de cada elemento. Aumenta el tamaño de la lista para añadir frases.
6. Guarda la escena y entra en Play para hablar con él.

Los cambios quedan en la instancia que seleccionaste. No pulses Apply al prefab si
quieres que el diálogo sea exclusivo del duplicado. Editar el prefab cambia el valor
predeterminado para sus instancias sin modificaciones propias.

Prefabs: `Assets/SoulBlocks/Prefabs/Dialogue/`:
YellowSpirit, BlueSpirit, GreenSpirit, CyanSpirit y RedSpirit.
Cada uno contiene **Model** para reemplazar su esfera por un modelo futuro.

El prototipo usa conversaciones lineales; todavía no tiene elecciones, misiones,
condiciones de progreso ni recompensas de diálogo. Las frases vacías se omiten.
