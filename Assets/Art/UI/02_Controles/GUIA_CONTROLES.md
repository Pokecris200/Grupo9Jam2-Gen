# Controles

Mira `VISTA_controles.png`. (Primero lee `../GUIA_GENERAL_UI.md`.)

Las teclas están en el orden del teclado: **W arriba** y **A – D abajo** a los lados, para recordarlas fácil.

## Piezas
- `../00_Comun/Fondos/fondo_menu.png` (1920 × 1080)
- `panel_controles.png` (960 × 720)
- `../00_Comun/Teclas/tecla.png` (72 × 80) y `tecla_ancha.png` (204 × 80)
- Botón: Prefab `Boton`

## Jerarquía y posiciones (X, Y desde arriba a la izquierda, en px)

```
Pantalla_Controles
├─ Fondo           Image fondo_menu        X 0    Y 0    1920 × 1080
├─ Panel           Image panel_controles   X 480  Y 116   960 × 720
├─ Titulo          Texto "CONTROLES"       X 480  Y 168   960 × 96    tamaño 80, dorado, centrado
├─ Txt_CambiarMundo  "CAMBIAR DE MUNDO"    X 480  Y 290   960 × 52    tamaño 40, gris, centrado
├─ Tecla_W         tecla.png + "W"         X 924  Y 356    72 × 80
├─ Tecla_A         tecla.png + "A"         X 824  Y 460    72 × 80
├─ Tecla_D         tecla.png + "D"         X 1024 Y 460    72 × 80
├─ Txt_Mover       "MOVER"                 X 480  Y 560   960 × 52    tamaño 40, gris, centrado
├─ Tecla_Espacio   tecla_ancha + "ESPACIO" X 858  Y 636   204 × 80
├─ Txt_Saltar      "SALTAR"                X 480  Y 730   960 × 52    tamaño 40, gris, centrado
└─ Boton_Volver    Prefab Boton "VOLVER"   X 656  Y 868   528 × 96
```

Las letras de las teclas: tamaño 40, color `#22190F`, sin sombra, centradas (texto hijo con Bottom 16).
