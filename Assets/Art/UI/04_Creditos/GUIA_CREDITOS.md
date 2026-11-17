# Créditos

Mira `VISTA_creditos.png`. (Primero lee `../GUIA_GENERAL_UI.md`.)

## Piezas
- `../00_Comun/Fondos/fondo_menu.png` (1920 × 1080)
- `panel_creditos.png` (1200 × 744)
- Botón: Prefab `Boton`

## Jerarquía y posiciones (X, Y desde arriba a la izquierda, en px)

Todos los textos van **centrados** (alineación centro / medio) y con ancho 1200.

```
Pantalla_Creditos
├─ Fondo         Image fondo_menu        X 0    Y 0    1920 × 1080
├─ Panel         Image panel_creditos    X 360  Y 104  1200 × 744
├─ Titulo        "CRÉDITOS"              X 360  Y 156  1200 × 96   tamaño 80, dorado
├─ Nombre_1      "Daniel Esteban Ardila" X 360  Y 300  1200 × 52   tamaño 40, dorado
├─ Rol_1         "Programador Principal" X 360  Y 346  1200 × 44   tamaño 32, gris
├─ Nombre_2      "Oscar Celis"           X 360  Y 424  1200 × 52   tamaño 40, dorado
├─ Rol_2         "SFX y Assets"          X 360  Y 470  1200 × 44   tamaño 32, gris
├─ Nombre_3      "Juan David Gonzalez"   X 360  Y 548  1200 × 52   tamaño 40, dorado
├─ Rol_3         "Arte y UI"             X 360  Y 594  1200 × 44   tamaño 32, gris
├─ Nombre_4      "Cristian Forero"       X 360  Y 672  1200 × 52   tamaño 40, dorado
├─ Rol_4         "Project Manager & Copywriting"  X 360  Y 718  1200 × 44   tamaño 32, gris
└─ Boton_Volver  Prefab Boton "VOLVER"   X 656  Y 880   528 × 96
```

Cada persona ocupa **124 px** de alto (nombre + rol). Para agregar otra, copia el par Nombre/Rol y súmale 124 a la Y.
La fuente escribe todo en mayúsculas aunque el texto esté en minúsculas.
