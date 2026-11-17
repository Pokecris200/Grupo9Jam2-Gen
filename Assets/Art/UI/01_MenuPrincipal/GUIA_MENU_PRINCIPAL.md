# Menú principal

Mira `VISTA_menu_principal.png` para ver cómo debe quedar.
(Primero lee `../GUIA_GENERAL_UI.md`: ahí está cómo poner posiciones, la fuente y el botón.)

## Piezas
- `../00_Comun/Fondos/fondo_menu.png` (1920 × 1080)
- `panel_menu.png` (688 × 720)
- Botón: el Prefab de `Boton` (ver guía general)

## Jerarquía y posiciones (X, Y desde arriba a la izquierda, en px)

```
Pantalla_MenuPrincipal
├─ Fondo            Image fondo_menu     X 0    Y 0    1920 × 1080
├─ Panel            Image panel_menu     X 616  Y 188   688 × 720
├─ Boton_Iniciar        "INICIAR"        X 656  Y 268   528 × 96
├─ Boton_Controles      "CONTROLES"      X 656  Y 388   528 × 96
├─ Boton_Configuracion  "CONFIGURACIÓN"  X 656  Y 508   528 × 96
├─ Boton_Creditos       "CRÉDITOS"       X 656  Y 628   528 × 96
└─ Boton_Salir          "SALIR"          X 656  Y 748   528 × 96
```

- Los botones están separados **120 px** entre sí (uno debajo del otro).
- En la vista, "INICIAR" aparece seleccionado (con la flecha): así se verá cuando el cursor o el teclado estén sobre él.
- En **EventSystem**, pon *First Selected* = `Boton_Iniciar` para que al abrir el menú ya esté seleccionado.
