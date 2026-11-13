# Tilesets: selva y ruinas con moho (plataformero 2D de vista lateral)

Unity 6000.3, 2D Tilemap + Rule Tile. **Cada tile mide 128×128 px y equivale a 1 unidad de Unity (PPU 128).**
El pixel art está dibujado a 32×32 y escalado ×4 sin suavizado: cada "píxel de arte" son 4×4 px de pantalla. No hay antialiasing ni alfa parcial.

**Paleta limitada (29 colores):**
- verdes de selva húmeda (G0–G7)
- musgo poco saturado (M0–M3)
- piedra de ruina gris-violeta (S0–S5)
- tierra mojada (E0–E4)
- agua (W0–W3)
- 2 acentos: hongo/brasa (A0) y luz/flor (A1). A1 también marca la cerradura de la puerta.

Los fondos solo usan los tonos más oscuros y menos saturados, para que el terreno jugable resalte.

## Estructura
```
Assets/Art/Tilesets/
├─ terreno/      terreno.png + .json      → RuleTiles/tierra
├─ plataformas/  plataformas.png + .json  → RuleTiles/piedra, madera, rama, liana
├─ ruinas/       ruinas.png + .json       → RuleTiles/muro  (+ columnas, bloques, escalones, arco, puerta)
├─ decoracion/   decoracion.png + .json   → RuleTiles/raiz   (+ decoración sin colisión)
└─ fondos/       fondo_1_selva_lejana.png, fondo_2_ruinas.png, fondo_3_follaje_cercano.png
Assets/Editor/TilesetImporter.cs           (configuración automática al importar)
```

## Formato de los spritesheets
- Rejilla de tiles de **128×128**, con **4 px de separación**. Cada tile tiene **2 px de borde extruido por lado** (se repite el píxel del borde), así que cada celda ocupa 132 px y no aparecen costuras en el Tilemap.
- Tile en la columna `c` y fila `f` (contando desde arriba): `x = c·132 + 2`, `y = f·132 + 2`.
- **Índice = fila × 8 + columna.** Todas las hojas tienen 8 columnas.
- Cada PNG va con un `.json` que lista nombre, posición y máscara de vecinos de cada tile. El importador lo usa para cortar la hoja y crear los Rule Tile.

### terreno.png (1056×924)
| Índices | Tiles |
|---|---|
| 0–46 | `tierra_00` … `tierra_46`: autotile blob. Tiene superficie de musgo y hierba, relleno, bordes izquierdo y derecho (paredes de pozos y caídas), esquinas externas e internas, y techos con raíces y musgo colgando |
| 48, 49, 50 | `tierra_var_piedras`, `tierra_var_raices`, `tierra_var_fosil`: variantes del relleno interior; el Rule Tile las mezcla al azar |

La superficie está al ras del borde superior del tile, así que la colisión de rejilla coincide con el arte. Los techos dejan 3 píxeles de arte (12 px de pantalla) de raíces colgando dentro del mismo tile.

### plataformas.png (1056×264)
| Índice | Tile | Índice | Tile |
|---|---|---|---|
| 0 | `piedra_corta` (1 tile) | 4 | `madera_corta` (1 tile) |
| 1 | `piedra_inicio` | 5 | `madera_inicio` |
| 2 | `piedra_medio` | 6 | `madera_medio` |
| 3 | `piedra_fin` | 7 | `madera_fin` |
| 8 | `rama_corta` (1 tile) | 12 | `liana_suelta` (1 tile) |
| 9 | `rama_inicio` | 13 | `liana_arriba` (se engancha al techo) |
| 10 | `rama_medio` | 14 | `liana_medio` (se repite) |
| 11 | `rama_fin` | 15 | `liana_abajo` (punta) |

- **Piedra** = plataforma flotante sólida de ruina.
- **Madera** (vigas rotas) y **rama** = plataformas de una vía. Se atraviesan desde abajo.
- **Liana** = enredadera para trepar. No tiene collider; el trigger lo define programación.

### ruinas.png (1056×1320)
| Índices | Tiles |
|---|---|
| 0–46 | `muro_00` … `muro_46`: muro de ruina con moho, autotile blob (mismo orden que tierra) |
| 48 | `columna_capitel` (arriba) |
| 49 | `columna_fuste` (se repite) |
| 50 | `columna_base` (va sobre el suelo) |
| 51 | `columna_rota` (remate roto) |
| 52 | `bloque_caido` |
| 53 | `bloques_pequenos` |
| 54 | `escalones_sube_der` |
| 55 | `escalones_sube_izq` |
| 56, 57 / 64, 65 / 72, 73 | **arco abierto 2×3**: `arco_sup_izq`, `arco_sup_der` / `arco_med_*` / `arco_inf_*` |
| 58, 59 / 66, 67 / 74, 75 | **puerta cerrada 2×3**: `puerta_sup_*` / `puerta_med_*` / `puerta_inf_*`. Tiene un glifo luminoso que indica que está bloqueada |

Las columnas se arman apiladas: capitel o columna rota arriba, fuste las veces que haga falta y base abajo.

### decoracion.png (1056×264), sin colisión
| Índice | Tile | Dónde va |
|---|---|---|
| 0 | `hojas` | sobre el suelo (apoyado abajo) |
| 1 | `helecho` | sobre el suelo |
| 2 | `flores` | sobre el suelo |
| 3 | `hierba_alta` | sobre el suelo |
| 4 | `hongos` | sobre el suelo |
| 5 | `charco` | sobre el suelo; agua con brillo |
| 6 | `gotas` | bajo un techo (colgando arriba) |
| 7 | `musgo_techo` | bajo un techo |
| 8 | `musgo_borde_der` | en la celda vacía a la **derecha** de una cornisa; el musgo sobresale del borde |
| 9 | `musgo_borde_izq` | en la celda vacía a la **izquierda** de una cornisa |
| 10–13 | `raiz_suelta`, `raiz_arriba`, `raiz_medio`, `raiz_abajo` | raíces colgantes (Rule Tile de columna) |

### fondos/ (parallax, repetibles en horizontal)
Cada capa mide 1024×1080 px, que a PPU 128 es una pantalla de 1080p de alto. Se repiten sin costura en X (`Wrap Mode U = Repeat`).

| Archivo | Capa | Sugerencia de velocidad |
|---|---|---|
| `fondo_1_selva_lejana.png` | selva lejana con neblina (opaca) | 0.1–0.2 × cámara |
| `fondo_2_ruinas.png` | ruinas a media distancia: templo, columnas, arco (transparente) | 0.3–0.5 × cámara |
| `fondo_3_follaje_cercano.png` | troncos, copas y lianas cercanas (transparente, la capa más oscura) | 0.6–0.8 × cámara |

## Rule Tiles que se generan
| Rule Tile | Tipo | Vecinos | Collider |
|---|---|---|---|
| `terreno/RuleTiles/tierra` | blob 47 | 3×3 (tabla de abajo); el relleno sale al azar entre `tierra_46` y las 3 variantes | Grid |
| `ruinas/RuleTiles/muro` | blob 47 | 3×3 (tabla de abajo) | Grid |
| `plataformas/RuleTiles/piedra` | fila | izq/der: corta, inicio, medio, fin | Grid |
| `plataformas/RuleTiles/madera` | fila | izq/der | Grid (de una vía, ver abajo) |
| `plataformas/RuleTiles/rama` | fila | izq/der | Grid (de una vía) |
| `plataformas/RuleTiles/liana` | columna | arriba/abajo: suelta, arriba, medio, abajo | None |
| `decoracion/RuleTiles/raiz` | columna | arriba/abajo | None |

Reglas de las filas:
- **corta** = sin vecino a izquierda ni derecha
- **inicio** = vecino solo a la derecha
- **medio** = vecinos a ambos lados
- **fin** = vecino solo a la izquierda

Las columnas siguen la misma lógica con arriba y abajo.

**Plataformas de una vía:** pon `madera` y `rama` en su propio Tilemap. Agrégale Tilemap Collider 2D con *Used By Effector* (y Composite si quieres), más un **Platform Effector 2D** con *Use One Way* activado. No requiere código.

## Importación automática (`Assets/Editor/TilesetImporter.cs`)
- Sprite Mode **Multiple**, cortado en rejilla de 128×128 respetando el padding. Los fondos van como **Single** con Wrap U = Repeat.
- **PPU 128**, **Filter Point**, **Compression None**, **Mesh Type Full Rect**, sin mipmaps, Max Size 8192, pivote centrado.
- Al reimportar, cada sprite conserva su ID, así que las referencias no se rompen.
- Menú: **Tools ▸ Tilesets ▸ Reimportar tilesets** y **Tools ▸ Tilesets ▸ Generar Rule Tiles**.

Paquetes necesarios:
- `com.unity.2d.sprite` y `com.unity.2d.tilemap`: ya están en `Packages/manifest.json`.
- **2D Tilemap Extras**: instálalo desde *Package Manager ▸ Unity Registry*. Sin él, el script compila pero avisa en consola que no puede crear los Rule Tile. Después de instalarlo, ejecuta *Tools ▸ Tilesets ▸ Generar Rule Tiles*.

## Autotile blob de 47 tiles (tierra y muro)
Índice local 0–46: es el mismo número del sprite (`tierra_NN` / `muro_NN`) y su índice en el sheet.

- **✔** = el vecino es el mismo Rule Tile.
- **✘** = no lo es (el borde del mapa cuenta como ✘).
- **·** = no importa. Una diagonal solo cuenta si sus dos lados ortogonales son ✔.

Máscara: N = 1, NE = 2, E = 4, SE = 8, S = 16, SO = 32, O = 64, NO = 128. En Unity, N es la celda de arriba (+Y).

| Índice | Col,Fila | Máscara | NO | N | NE | O | E | SO | S | SE |
|---:|:---:|---:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| 0 | 0,0 | 0 | · | ✘ | · | ✘ | ✘ | · | ✘ | · |
| 1 | 1,0 | 1 | · | ✔ | · | ✘ | ✘ | · | ✘ | · |
| 2 | 2,0 | 4 | · | ✘ | · | ✘ | ✔ | · | ✘ | · |
| 3 | 3,0 | 5 | · | ✔ | ✘ | ✘ | ✔ | · | ✘ | · |
| 4 | 4,0 | 7 | · | ✔ | ✔ | ✘ | ✔ | · | ✘ | · |
| 5 | 5,0 | 16 | · | ✘ | · | ✘ | ✘ | · | ✔ | · |
| 6 | 6,0 | 17 | · | ✔ | · | ✘ | ✘ | · | ✔ | · |
| 7 | 7,0 | 20 | · | ✘ | · | ✘ | ✔ | · | ✔ | ✘ |
| 8 | 0,1 | 21 | · | ✔ | ✘ | ✘ | ✔ | · | ✔ | ✘ |
| 9 | 1,1 | 23 | · | ✔ | ✔ | ✘ | ✔ | · | ✔ | ✘ |
| 10 | 2,1 | 28 | · | ✘ | · | ✘ | ✔ | · | ✔ | ✔ |
| 11 | 3,1 | 29 | · | ✔ | ✘ | ✘ | ✔ | · | ✔ | ✔ |
| 12 | 4,1 | 31 | · | ✔ | ✔ | ✘ | ✔ | · | ✔ | ✔ |
| 13 | 5,1 | 64 | · | ✘ | · | ✔ | ✘ | · | ✘ | · |
| 14 | 6,1 | 65 | ✘ | ✔ | · | ✔ | ✘ | · | ✘ | · |
| 15 | 7,1 | 68 | · | ✘ | · | ✔ | ✔ | · | ✘ | · |
| 16 | 0,2 | 69 | ✘ | ✔ | ✘ | ✔ | ✔ | · | ✘ | · |
| 17 | 1,2 | 71 | ✘ | ✔ | ✔ | ✔ | ✔ | · | ✘ | · |
| 18 | 2,2 | 80 | · | ✘ | · | ✔ | ✘ | ✘ | ✔ | · |
| 19 | 3,2 | 81 | ✘ | ✔ | · | ✔ | ✘ | ✘ | ✔ | · |
| 20 | 4,2 | 84 | · | ✘ | · | ✔ | ✔ | ✘ | ✔ | ✘ |
| 21 | 5,2 | 85 | ✘ | ✔ | ✘ | ✔ | ✔ | ✘ | ✔ | ✘ |
| 22 | 6,2 | 87 | ✘ | ✔ | ✔ | ✔ | ✔ | ✘ | ✔ | ✘ |
| 23 | 7,2 | 92 | · | ✘ | · | ✔ | ✔ | ✘ | ✔ | ✔ |
| 24 | 0,3 | 93 | ✘ | ✔ | ✘ | ✔ | ✔ | ✘ | ✔ | ✔ |
| 25 | 1,3 | 95 | ✘ | ✔ | ✔ | ✔ | ✔ | ✘ | ✔ | ✔ |
| 26 | 2,3 | 112 | · | ✘ | · | ✔ | ✘ | ✔ | ✔ | · |
| 27 | 3,3 | 113 | ✘ | ✔ | · | ✔ | ✘ | ✔ | ✔ | · |
| 28 | 4,3 | 116 | · | ✘ | · | ✔ | ✔ | ✔ | ✔ | ✘ |
| 29 | 5,3 | 117 | ✘ | ✔ | ✘ | ✔ | ✔ | ✔ | ✔ | ✘ |
| 30 | 6,3 | 119 | ✘ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✘ |
| 31 | 7,3 | 124 | · | ✘ | · | ✔ | ✔ | ✔ | ✔ | ✔ |
| 32 | 0,4 | 125 | ✘ | ✔ | ✘ | ✔ | ✔ | ✔ | ✔ | ✔ |
| 33 | 1,4 | 127 | ✘ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |
| 34 | 2,4 | 193 | ✔ | ✔ | · | ✔ | ✘ | · | ✘ | · |
| 35 | 3,4 | 197 | ✔ | ✔ | ✘ | ✔ | ✔ | · | ✘ | · |
| 36 | 4,4 | 199 | ✔ | ✔ | ✔ | ✔ | ✔ | · | ✘ | · |
| 37 | 5,4 | 209 | ✔ | ✔ | · | ✔ | ✘ | ✘ | ✔ | · |
| 38 | 6,4 | 213 | ✔ | ✔ | ✘ | ✔ | ✔ | ✘ | ✔ | ✘ |
| 39 | 7,4 | 215 | ✔ | ✔ | ✔ | ✔ | ✔ | ✘ | ✔ | ✘ |
| 40 | 0,5 | 221 | ✔ | ✔ | ✘ | ✔ | ✔ | ✘ | ✔ | ✔ |
| 41 | 1,5 | 223 | ✔ | ✔ | ✔ | ✔ | ✔ | ✘ | ✔ | ✔ |
| 42 | 2,5 | 241 | ✔ | ✔ | · | ✔ | ✘ | ✔ | ✔ | · |
| 43 | 3,5 | 245 | ✔ | ✔ | ✘ | ✔ | ✔ | ✔ | ✔ | ✘ |
| 44 | 4,5 | 247 | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✘ |
| 45 | 5,5 | 253 | ✔ | ✔ | ✘ | ✔ | ✔ | ✔ | ✔ | ✔ |
| 46 | 6,5 | 255 | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ |

## Orden de capas sugerido
Un `Grid` con Cell Size (1, 1, 0). De atrás hacia delante:
1. **Fondos**: las 3 capas de parallax.
2. **Decoración trasera**: arco, columnas y raíces.
3. **Terreno**: `tierra` y `muro`, con Tilemap Collider 2D + Composite Collider 2D.
4. **Plataformas sólidas**: `piedra`.
5. **Plataformas de una vía**: `madera` y `rama`, con Platform Effector 2D.
6. **Trepables**: `liana`, como trigger.
7. **Decoración delantera**: hierba, helechos, charcos, gotas y musgo de borde.

El mapa demo (`C:\Gen\interfaz\mapa_demo.png` y la página "Mapa demo" del archivo FigCraft) muestra cómo encajan todas las piezas.

## Regenerar el arte
`C:\Gen\interfaz\generador\generar_lateral.ps1` vuelve a escribir los PNG y los `.json` en esta carpeta. Unity los reimporta conservando las referencias.
