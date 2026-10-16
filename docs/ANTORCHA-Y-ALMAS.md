# Antorcha y almas errantes

Las almas solo se juntan con la antorcha: no se recogen por contacto. Junto al árbol está el
altar de la antorcha (**Torch Altar Area**) con la antorcha flotando sobre él.

- **F** cerca de la antorcha: tomarla. Se enciende y acompaña la mano izquierda, dejando libre
  la derecha para picar.
- **F** mientras la llevas: colocarla flotando delante del jugador, sobre suelo firme.
- **F** cerca de una antorcha colocada: recogerla de nuevo.
- **F** llevando la antorcha cerca del altar: devolverla a su lugar.
- **T**: encender/apagar mientras la llevas o estás cerca de ella.
- **E** queda para el altar del árbol; la antorcha usa F para evitar conflictos.

## Llamar y guiar

Las almas merodean fuera de los caminos a un máximo de 4 m/s y su movimiento libre evita
entrar en ellos. Con la antorcha en la mano, las almas a su alcance (**Alcance de la antorcha**,
4 m al empezar) la siguen a 9 m/s, por detrás del jugador, aunque camines o piques. Una vez que
te siguen, mantienen el seguimiento hasta 18 m; si te alejas más o apagas la luz, vuelven a
merodear.

Como mucho te siguen **Carga** almas a la vez (12 al empezar). Las demás esperan hasta que haya
lugar.

Colocada y encendida, la antorcha hace un llamado: el radio empieza en 4 m y crece 2 m cada 3 s
hasta 12 m. El anillo y el HUD muestran el alcance y la próxima expansión. Al llegar al máximo
aparece «LLAMADO COMPLETO» y un aviso para recogerla. Apagarla o abrir un menú pausa el
crecimiento; recogerla y volver a colocarla empieza un llamado nuevo.

## Entregar

Con la antorcha devuelta al altar, las almas cercanas ascienden y se desvanecen; cada una suma
un alma a la reserva. Con **Entrega en refugios** (Wisdom), E dentro de un refugio entrega en el
acto las almas que te siguen. Si la expedición termina por energía, las que te seguían se quedan
donde estaban.

## De dónde salen

- Junto a todos los caminos activos, en dos franjas a cada lado, y algunas en el vacío
  (`Path Population`). Al empezar cada expedición se reponen las que faltan; **Abundancia**
  acerca los puestos (más almas).
- Las prisiones liberan almas al completar cada fase; aparecen donde se rompieron.
- Los árboles nutribles vivos generan las suyas (ver [BLOQUES-ESPECIALES.md](BLOQUES-ESPECIALES.md)).
- La anomalía de almas de la expedición suelta un grupo en un tramo de la rama activa.

## Prefab y modelo de Blender

`Assets/SoulBlocks/Prefabs/SpiritGuiding/GuidingTorch.prefab`

Reemplaza el contenido de **Model - replace with FBX** por tu FBX. El origen es el punto de
agarre y el eje Y apunta hacia arriba. El mango provisional mide unos 0,48 m. Conserva el objeto
raíz, el componente `GuidingTorch`, la luz y el indicador de alcance. Ajusta Hand Offset si
necesitas corregir el agarre.

Las almas son `Assets/SoulBlocks/Prefabs/SpiritGuiding/WanderingSpirit.prefab`: amarillas, con
núcleo luminoso y halo translúcido pulsante. Ver [ALMAS-Y-ARENA.md](ALMAS-Y-ARENA.md).
