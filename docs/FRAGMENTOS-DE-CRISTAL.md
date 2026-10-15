# Fragmentos recolectables de cristal

Prefab: `Assets/SoulBlocks/Resources/Crystals/CrystalFragment.prefab`.

Al romper un cristal aparecen 3 fragmentos por tamaño (3 pequeño, 6 mediano, 9 grande).
Cada cubito vale una unidad y conserva el color del cristal. Se dispersa brevemente,
queda en el suelo y se recoge al acercarse a 1,1 metros, tras una espera de 0,7 s.
Romperlo ya no suma recursos directamente. Los fragmentos permanecen hasta recogerlos
o salir de la escena; no hay guardado de fragmentos en el suelo en esta prueba.

## Reemplazar con Blender

1. Abre el prefab CrystalFragment.
2. Dentro de **Model**, elimina **Placeholder - replace with Blender FBX**.
3. Coloca tu FBX como hijo de Model, centrado en el origen. El cubito provisional mide 0,24 m.
4. Ajusta sólo el modelo visual. Conserva el componente CrystalFragment del objeto raíz.
5. Más adelante puedes añadir Animator y animaciones dentro de Model. El script no
   modifica sus transformaciones locales, de modo que puedes animar esa jerarquía.

El objeto raíz controla la dispersión y la recolección. No necesita Rigidbody ni
colisiones físicas con el jugador. Pickup Radius y Pickup Delay son editables.
El material debe permitir _BaseColor o _Color para recibir el color del cristal.

BreakableCrystal permite asignar un prefab alternativo en **Fragment Prefab**.
Si ese campo está vacío usa el prefab de Resources indicado arriba. Mantén ese nombre
 y ruta, o asigna explícitamente el prefab antes de renombrarlo/moverlo.
