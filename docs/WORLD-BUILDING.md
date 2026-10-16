# Construir el mundo y la expedición

Todo el mundo vive en `Assets/Scenes/SoulBlocks.unity`, hecho a mano: no hay seeds ni
generación. Al entrar en Play, `SceneLayout` lee los caminos y los marcadores de la escena y
arma con ellos el mapa que usan la expedición, el mapa (Tab), las prisiones, los refugios, los
portales y los objetivos.

## Caminos

1. En Hierarchy selecciona un camino bajo **World** (por ejemplo **Primary Path**), o usa
   **Tools > SoulBlocks > Editar puntos del camino**. Pulsa F sobre Scene para encuadrarlo.
2. Fuera de Play, arrastra las esferas P0, P1… X/Z cambian el recorrido; Y cambia la altura.
   La curva pasa por esos puntos.
3. En Inspector ajusta **Width**, **Thickness** y **Samples Per Segment**. Usa **Añadir punto al
   final**, **Insertar después del seleccionado** o **Eliminar punto seleccionado** (Ctrl+Z deshace).
4. Guarda la escena con Ctrl+S y prueba en Play.

Cada camino tiene un **Role**:

- **Decor**: solo suelo caminable.
- **Branch**: una rama de expedición, con su **Specialty** (Souls, Prisons o Exploration). Un
  camino Branch anidado dentro de otro lo extiende (así funciona **Deep Path**, que sigue a
  **Primary Path** después del laboratorio).
- **Connection**: un puente entre las dos ramas más cercanas a sus extremos.

Las ramas se cortan en tramos de 30 m (`Balance.TileSize`) para el descubrimiento y el mapa.
Mantén escala (1,1,1) en los caminos; evita puntos coincidentes, cruces y pendientes verticales.
El suelo usa la capa Ground (8).

## Marcadores

Se colocan como hijos del camino al que pertenecen (o cerca de él: si no cuelgan de un camino
Branch, toman la rama del tramo más cercano):

- **PrisonSite**: una prisión. Tier, propiedades (Resonant, ExposedCore), grupo de resonancia,
  patrón y **Branch End** (la del final de la rama: ofrece una bendición y abre su portal).
- **ShelterSite**: un refugio con su **Radius**. Pausa la energía y, con Entrega, recibe las
  almas que siguen a la antorcha. El laboratorio y la arena tienen uno cada uno.
- **PortalSite**: el portal de la rama, activo con Raíz I y su prisión final rota.
- **RiskAltarSite**: un altar de riesgo.
- **UnlockGate**: oculta a sus hijos hasta tener el desbloqueo indicado (por ejemplo
  `wisdom.raiz3`). Todo lo que cuelga de un gate queda bloqueado también en el mapa: **Deep Path
  Gate** (Raíz III), **Nucleo Gate** (Despertar) y **Connection Gate** (Raíz II).

Los ids de prisiones y tramos siguen el orden de la jerarquía; el guardado los usa, así que
reordenar marcadores cambia qué prisión está rota en una partida guardada.

## Recorrido actual

- **Primary Path** (Prisons): sube en S hasta el laboratorio de bloques. Dos T1, un grupo
  resonante de tres T2, un altar de riesgo, una T3 expuesta y la T3 final antes del laboratorio.
  Sigue por **Deep Path** (Raíz III) con dos T4 y el **Núcleo** (Despertar) en su plataforma.
- **Power Test Path** (Souls): hasta la arena de poderes. Dos T1 y la T2 final.
- **Exploration Path** (Exploration): hacia el oeste de la isla. Refugio a mitad de camino,
  una plataforma alcanzable solo con dash con un altar de riesgo, y la T2 final.
- **Connection Path** (Raíz II): une el camino principal con el de poderes.

La población de almas y grupos de cristales sobre los caminos se genera al jugar
(`Path Population`); ver [ALMAS-Y-ARENA.md](ALMAS-Y-ARENA.md).

## Terreno y árbol central

La isla es un Terrain editable bajo **Main Island**; ver [TERRENO-Y-ALTAR.md](TERRENO-Y-ALTAR.md).
