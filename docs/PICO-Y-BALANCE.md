# Pico: niveles, modelos y balance

En WorldBuilding, acércate al altar del árbol blanco y pulsa E. La primera sección permite mejorar el pico con los fragmentos de cristal recolectados. El primer aumento cuesta 10 cristales y cada compra siguiente cuesta 5 más; estos valores son provisionales y editables. Los bloques especiales conservan sus mejoras gratuitas de prueba.

| Etapa | Niveles | Daño añadido por nivel | Daño inicial → final | Segundos por golpe inicial → final | Vida cristal pequeño |
|---|---|---|---|---|---|
| Amarillo | 1–10 | 1 | 1 → 10 | 0,550 → 0,442 | 10 |
| Azul | 11–20 | 2 | 12 → 30 | 0,490 → 0,382 | 150 |
| Verde | 21–30 | 3 | 33 → 60 | 0,430 → 0,322 | 1000 |
| Celeste | 31–40 | 5 | 65 → 110 | 0,370 → 0,262 | 1500 |
| Rojo | 41–50 | 7 | 117 → 180 | 0,310 → 0,202 | 3000 |

El daño se acumula y nunca se reinicia. La velocidad mejora 0,012 segundos por nivel dentro de cada etapa. Al pasar al siguiente color se pierde una parte de esa rapidez; aun así, el inicio de cada etapa es más rápido que el inicio de la anterior. El efecto verde multiplica adicionalmente la velocidad. El contacto y la animación se ajustan juntos; un cristal recibe daño una sola vez por golpe.

## Variables

Selecciona `Assets/SoulBlocks/Resources/Mining/Balance.asset` en el Inspector. Las listas siguen el orden amarillo, azul, verde, celeste y rojo:

- `Damage Per Level`: incremento de daño por nivel de cada etapa.
- `Crystal Health`: vida base por color; medianos ×2, grandes ×3.
- `Starting Seconds`: duración inicial por etapa; menor tiempo significa más rapidez.
- `Seconds Faster Per Level` y `Minimum Seconds`: avance por nivel y límite de velocidad.
- `First Upgrade Cost` y `Additional Cost Per Level`: costes. Ambos a 0 permiten probar todos los niveles gratis.
- Violeta conserva su vida provisional por separado.

Edita el balance fuera de Play. Los cristales toman su vida al aparecer o al restaurarlos.

## Modelos de Blender

Abre `Assets/SoulBlocks/Resources/Mining/Pickaxe.prefab`. El componente `PickaxeVisual` contiene cinco referencias `Stage Prefabs`; asigna allí los cinco prefabs de tus modelos FBX en el orden anterior. `Model` es el punto de montaje. Los cinco modelos actuales están en `Assets/SoulBlocks/Prefabs/Mining/` y son copias del pico provisional.

El jugador instala este prefab en el punto de sujeción existente y conserva la animación de picado. Prepara cada modelo con origen y orientación consistentes; usa un prefab envolvente para corregir rotación/escala si el FBX lo requiere.

El nivel forma parte de RunState y del guardado normal. WorldBuilding continúa siendo una escena de prueba que reinicia la partida al volver a Play y no escribe guardados persistentes.

## Lectura inicial del balance

Los colores no bloquean el minado: el pico básico puede romper un rojo, pero necesita 3000 golpes. Al terminar cada etapa, un cristal pequeño de su color requiere respectivamente 1, 5, 17, 14 y 17 golpes. El salto a verde es el mayor: al nivel 21 requiere 31 golpes. Son valores de prueba, pendientes de ajustar junto con los costes y las recompensas.
