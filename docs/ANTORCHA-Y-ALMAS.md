# Antorcha y almas errantes (prueba)

Abre WorldBuilding. Junto al árbol está el cilindro **Altar** y una antorcha flotante.

- **F** cerca de la antorcha: tomarla. Se enciende y acompaña la mano izquierda, dejando libre la derecha para picar.
- **F** mientras la llevas: colocarla flotando delante del jugador, sobre suelo firme.
- **F** cerca de una antorcha colocada: recogerla nuevamente.
- **F** llevando la antorcha cerca del altar: devolverla a su lugar flotante.
- **T**: encender/apagar mientras la llevas o estás cerca de ella.
- **E** conserva la tienda del árbol; la antorcha usa F para evitar conflictos.

Las almas son esferas luminosas moradas con una estela corta. Merodean fuera del
camino a una velocidad máxima de 4 m/s; su movimiento libre evita entrar en él.
Coloca la antorcha encendida con F: el radio empieza en 4 m y aumenta 2 m cada 3 s
hasta 12 m (12 s en total). El anillo y el HUD muestran el alcance y la próxima
expansión. Al llegar al máximo aparece «LLAMADO COMPLETO» y un aviso para recogerla.
Puedes picar mientras esperas. Recogerla antes también es posible; volver a colocarla
inicia un llamado nuevo desde 4 m. Apagarla o abrir un menú pausa el crecimiento.

En la mano su alcance para atraer almas nuevas es de 4 m. Las almas atraídas
siguen la luz a 9 m/s, incluso mientras caminas. Una vez atraídas, mantienen el
seguimiento hasta 18 metros para no perderlas fácilmente al moverte. Si superas
esa distancia o apagas la luz, vuelven a merodear. No se recogen por contacto ni con
el imán: las conduces al altar.

Con la antorcha devuelta y encendida en el altar, las almas cercanas ascienden y
se desvanecen. Cada una suma una sola alma a la reserva espiritual existente.
El contador de antorcha indica cuántas guiaste en esta prueba. No hay mejoras de luz,
reposición nocturna ni persistencia entre sesiones en la escena de construcción.

## Prefab y modelo de Blender

`Assets/SoulBlocks/Prefabs/SpiritGuiding/GuidingTorch.prefab`

Reemplaza el contenido de **Model - reemplazar por FBX** por tu FBX. El origen es el
punto de agarre y el eje Y apunta hacia arriba. El mango provisional mide unos
0,48 m. Conserva el objeto raíz y el componente GuidingTorch, la luz y el indicador
 de alcance. Ajusta Hand Offset si necesitas corregir el agarre.

Las almas y el altar están bajo **Altar y almas errantes** en Hierarchy. El suelo
lateral es una malla fija creada para este trazado; no se adapta automáticamente
si después mueves el camino. Puedes mover las almas individualmente fuera de Play.

En WorldBuilding se desactivaron las antiguas esferas recolectables y las almas
sueltas liberadas por cristales. Cada cristal roto suelta 3 fragmentos por nivel de
tamaño; al recogerlos se suman al contador independiente de cristales. Las almas entregadas se acumulan
en la reserva espiritual que ya consume la tienda del árbol para sus mejoras.
El gasto de cristales en el árbol y los picos sigue pendiente. La escena anterior
SoulBlocks conserva sus sistemas antiguos.

Actualización: las almas son amarillas, tienen halo etéreo sin estelas y siguen por detrás al jugador cuando lleva la antorcha. Población ampliada y arena de poderes: [ALMAS-Y-ARENA.md](ALMAS-Y-ARENA.md).
