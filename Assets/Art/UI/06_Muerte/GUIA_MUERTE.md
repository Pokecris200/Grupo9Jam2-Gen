# Pantalla de muerte

Mira `VISTA_muerte.png`. (Primero lee `../GUIA_GENERAL_UI.md`.)

## Piezas
- `fondo_muerte.png` (1920 × 1080) — escena del juego muy oscurecida (o usa el juego real detrás + capa oscura, como en la pausa, con Alpha 80 %)
- `panel_muerte.png` (688 × 384)
- Botón: Prefab `Boton`

## Jerarquía y posiciones (X, Y desde arriba a la izquierda, en px)

```
Pantalla_Muerte
├─ Fondo                  Image fondo_muerte       X 0    Y 0    1920 × 1080
├─ Titulo                 "HAS CAÍDO"              X 0    Y 130  1920 × 140  tamaño 120, dorado, centrado, sombra 10 px
├─ Panel                  Image panel_muerte       X 616  Y 340   688 × 384
├─ Boton_Reintentar       "REINTENTAR"             X 656  Y 416   528 × 96
├─ Boton_SalirMainMenu    "SALIR AL MAIN MENU"     X 656  Y 536   528 × 96   ← texto tamaño 30 (es largo)
└─ Txt_Tip                TIP: "..."               X 160  Y 770  1600 × 48   tamaño 30, gris, centrado
```

## El tip

Es **un solo texto** TextMeshPro. Para que "TIP:" salga dorado y el resto gris, escribe en el texto:

```
<color=#E8D27A>TIP:</color> "Sigue la flecha para encontrar la salida"
```

(TextMeshPro entiende esas etiquetas de color; activa *Rich Text* si no se ve.)

Cabe una frase larga en una línea. Para tener muchos tips, el programador guarda una lista de frases y cambia este texto
al azar cada vez que se abre la pantalla. Ejemplos:
- "Sigue la flecha para encontrar la salida"
- "Cambia de mundo con W para descubrir caminos ocultos"
- "Las chichas solo aparecen en el mundo espiritual"
- "Lleva las 4 chichas al altar para abrir el portal"

- *First Selected* del EventSystem = `Boton_Reintentar`.
