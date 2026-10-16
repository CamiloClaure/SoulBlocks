# Construir el camino primario

1. Abre `Assets/Scenes/WorldBuilding.unity` (o Tools > SoulBlocks > Open World Building).
2. En Hierarchy selecciona **Mundo manual > Camino primario**. Pulsa F con el cursor sobre Scene para encuadrarlo.
3. Fuera de Play, pulsa una esfera amarilla P0, P1… y arrastra sus flechas. X/Z cambian el recorrido; Y cambia la altura. La curva pasa por esos puntos.
4. En Inspector ajusta **Width** (ancho), **Thickness** (grosor) y, si necesitas más suavidad, **Samples Per Segment**.
5. Usa **Añadir punto al final**, **Insertar después del seleccionado** o **Eliminar punto seleccionado**. Ctrl+Z deshace los cambios.
6. Mueve también **Destino provisional** cuando cambies el final del camino; la plataforma es un objeto independiente.
7. Guarda la escena con Ctrl+S, entra en Play y recorre el camino. Sal de Play antes de continuar editando.

El recorrido inicial sale cerca del árbol, cruza el borde de la isla, hace una S y sube suavemente a una plataforma. Es una guía editable: puedes cambiar cada punto para dar significado a tu mundo.

Los puntos se guardan en la escena. El suelo y el MeshCollider se reconstruyen a partir de esos puntos al abrirla y al jugar; no usan seed ni eligen rutas aleatorias. La escena original SoulBlocks conserva su funcionamiento anterior.

En esta prueba no se generan mapas, cristales o recompensas procedurales, no comienza la expedición al salir de la isla y no se sobrescribe el guardado de partida. La construcción se guarda como escena, no desde el menú de pausa.

## Límites de esta primera herramienta

- Mantén escala (1,1,1) en Camino primario y ajusta el ancho desde Inspector.
- Evita puntos coincidentes, curvas que se crucen y pendientes verticales. La herramienta no corrige automáticamente pendientes o cruces; debes probarlos con el jugador.
- El suelo usa la capa Ground (8), compatible con el movimiento y la cámara existentes.
- Las bifurcaciones, iluminación por tramos y representación de caminos manuales en el minimapa quedan para una siguiente etapa.
- Para crear otra ruta puedes duplicar Camino primario y editar sus puntos. Usa una plataforma para unir rutas.

## Camino con cristales amarillos

El camino se amplió conservando el trazado previo y añadiendo curvas hasta una plataforma final.
Longitud aproximada: 222 m. Hay 24 cristales amarillos colocados en la escena, bajo
`Mundo manual > Cristales amarillos del camino`: 18 pequeños y 6 medianos.
Puedes mover cada cristal individualmente. No se recolocan automáticamente al editar
los puntos del camino y todavía no existe reposición nocturna.

## Población de todos los caminos y prueba de bloques

Al entrar en Play, **Poblacion de caminos dibujados** lee todos los componentes
AuthoredPath activos. No modifica el trazado ni usa seed. Distribuye almas por
ambos lados aproximadamente cada 10 m y añade grupos de tres cristales amarillos
cada 14 m, evitando superponerlos con los cristales colocados a mano. Hay un límite
editable de 120 almas y 120 cristales añadidos. Estos objetos aparecen al jugar;
para aplicar cambios a tus caminos sal de Play, edita, guarda y vuelve a jugar.

Las almas flotan fuera de los caminos, incluso donde aún no has modelado terreno.
La antorcha puede atraerlas desde allí. El suelo lateral anterior sigue siendo una
malla fija: la población no construye nuevo terreno alrededor de las rutas.

Al final del Camino primario está **Prueba de cinco bloques**, sobre una plataforma
amplia con 15 cristales de cinco colores y tres tamaños. Rueda selecciona el bloque,
clic derecho usa el activo y E dentro de esa zona abre mejoras y restauración.
Los cinco están desbloqueados y sus pasivas activas en esta prueba. Las mejoras
siguen siendo gratuitas; el desbloqueo progresivo por nivel del árbol no se aplica
al laboratorio. Los ayudantes celestes ahora también recogen fragmentos de cristal.

La plataforma de prueba permanece donde fue colocada: si cambias el final del
camino, mueve también la plataforma y la zona de prueba para mantener la conexión.

## Terreno y árbol central
WorldBuilding usa ahora un Terrain editable, con el suelo antiguo desactivado, un árbol blanco provisional y un altar que revela cinco símbolos para mejorar bloques y acceder a talentos. Consulta [TERRENO-Y-ALTAR.md](TERRENO-Y-ALTAR.md).
