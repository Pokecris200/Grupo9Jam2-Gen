# Pausa

Mira `VISTA_pausa.png`. (Primero lee `../GUIA_GENERAL_UI.md`.)

## Piezas
- `fondo_pausa.png` (1920 × 1080) — una escena del juego oscurecida, solo para la vista previa
- `panel_pausa.png` (688 × 696)
- Botón: Prefab `Boton`

> **Sobre el fondo:** dentro del juego lo normal es que detrás de la pausa se vea **el juego real congelado**.
> En ese caso NO uses `fondo_pausa.png`: pon una Image negra que cubra toda la pantalla (stretch-stretch) con color `#0B1610` y **Alpha 70 %**.
> `fondo_pausa.png` sirve si quieren una imagen fija.

## Jerarquía y posiciones (X, Y desde arriba a la izquierda, en px)

```
Pantalla_Pausa
├─ Oscurecer             Image (#0B1610, alpha 70%) o fondo_pausa   X 0  Y 0   1920 × 1080
├─ Panel                 Image panel_pausa       X 616  Y 192   688 × 696
├─ Titulo                "PAUSA"                 X 616  Y 244   688 × 96   tamaño 80, dorado, centrado
├─ Boton_Continuar       "CONTINUAR"             X 656  Y 368   528 × 96
├─ Boton_Configuracion   "CONFIGURACIÓN"         X 656  Y 488   528 × 96
├─ Boton_MainMenu        "MAIN MENU"             X 656  Y 608   528 × 96
└─ Boton_Salir           "SALIR"                 X 656  Y 728   528 × 96
```

- Botones separados **120 px**.
- *First Selected* del EventSystem = `Boton_Continuar`.
- El botón Configuración abre la misma pantalla de `03_Configuracion`.
