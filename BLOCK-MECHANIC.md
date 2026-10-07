# SoulBlocks: bloques y caminos

Proyecto: `D:\Games\SoulBlocks`.

- Camina con WASD o las flechas.
- El bloque disponible más cercano se selecciona automáticamente y se ilumina al estar a menos de 2.5 metros.
- Mantén E para agarrarlo y arrastrarlo mientras caminas.
- Suelta E junto al siguiente tramo oscuro para colocarlo.
- Cada tramo amarillo requiere un bloque amarillo y que el tramo anterior esté abierto.
- El bloque colocado se transforma en una losa sólida; puedes caminar sobre ella.
- Fuera del camino, soltar E deja el bloque sobre la isla si hay espacio. Si no hay una posición segura, vuelve al lugar donde lo agarraste.
- El botón derecho y la rueda conservan los controles de cámara.

La escena de ejemplo tiene tres caminos de seis tramos y 30 bloques. Tres bloques aparecen cerca del jugador; los demás se distribuyen por la isla evitando superposición y bordes sin apoyo.

## Archivos

`Assets/SoulBlocks/Runtime/`:

- `BlockGenerator.cs`: distribución de bloques y validación del suelo.
- `PickableBlock.cs`: color, selección, arrastre y estado colocado.
- `BlockInteraction.cs`: búsqueda del más cercano, tecla E y colocación.
- `PathSlot.cs`: color requerido, orden y desbloqueo de cada tramo.
- `SoulPlayer.cs`: reconoce la isla y las losas colocadas como suelo.

`Assets/SoulBlocks/Editor/BlockMechanicSetup.cs` configura la escena y proporciona el menú `Tools > SoulBlocks > Validate Block Mechanic` para comprobar la mecánica.

`PickableBox.cs` fue renombrado a `PickableBlock.cs` conservando su GUID para mantener la referencia del prefab.

La cantidad se ajusta en el componente `BlockGenerator`, campo `Block Count`. Los alcances se ajustan en `BlockInteraction`.

El progreso se reinicia al volver a entrar en Play.
