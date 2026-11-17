# Configuración (volumen de música y efectos)

Mira `VISTA_configuracion.png`. (Primero lee `../GUIA_GENERAL_UI.md`.)

## Piezas
- `../00_Comun/Fondos/fondo_menu.png` (1920 × 1080)
- `panel_configuracion.png` (960 × 496)
- `icono_musica.png` (28 × 36), `icono_efectos.png` (36 × 36)
- `slider_fondo.png` (560 × 48), `slider_relleno.png` (560 × 48), `slider_manija.png` (40 × 48)
- Botón: Prefab `Boton`

## Jerarquía y posiciones (X, Y desde arriba a la izquierda, en px)

```
Pantalla_Configuracion
├─ Fondo           Image fondo_menu           X 0    Y 0    1920 × 1080
├─ Panel           Image panel_configuracion  X 480  Y 184   960 × 496
├─ Titulo          "CONFIGURACIÓN"            X 480  Y 244   960 × 96   tamaño 80, dorado, centrado
├─ Icono_Musica    Image icono_musica         X 584  Y 392    28 × 36
├─ Txt_Musica      "MÚSICA"                   X 632  Y 384   184 × 52   tamaño 36, gris, izquierda/medio
├─ Slider_Musica   Slider                     X 816  Y 388   560 × 48
├─ Icono_Efectos   Image icono_efectos        X 580  Y 552    36 × 36
├─ Txt_Efectos     "EFECTOS"                  X 632  Y 544   184 × 52   tamaño 36, gris, izquierda/medio
├─ Slider_Efectos  Slider                     X 816  Y 548   560 × 48
└─ Boton_Volver    Prefab Boton "VOLVER"      X 656  Y 712   528 × 96
```

## Cómo armar cada Slider (sin scripts)

1. **GameObject > UI > Slider**. Width **560**, Height **48**.
2. Hijo **Background**: Image = `slider_fondo`, Anchor stretch-stretch, todo en 0.
3. Hijo **Fill Area**: Anchor stretch-stretch, **Left 0, Right 0, Top 0, Bottom 0**.
   - Su hijo **Fill**: Image = `slider_relleno`, **Image Type = Sliced**, Left 0 / Right 0 / Top 0 / Bottom 0.
   - Antes, en el **Sprite Editor** de `slider_relleno`, pon **Border L = 12, R = 12** (T = 0, B = 0) y Apply. Así se estira sin deformar los bordes.
4. Hijo **Handle Slide Area**: Left 20, Right 20, Top 0, Bottom 0.
   - Su hijo **Handle**: Image = `slider_manija`, Width **40**, Height **48** (pulsa *Set Native Size*).
5. En el componente **Slider**: Min 0, Max 1, Value 0.7 (música) / 0.45 (efectos) solo como ejemplo.

El programador conecta el **On Value Changed** de cada Slider al volumen (AudioMixer o AudioSource).
