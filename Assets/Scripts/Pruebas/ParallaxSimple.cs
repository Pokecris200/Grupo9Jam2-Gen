using UnityEngine;

// SCRIPT DE PRUEBA: parallax sencillo para ver el fondo mientras llega el de tu compañero.
// Las capas van DENTRO de la Main Camera (como hijas), así el fondo siempre llena la pantalla.
// El script solo las desliza a los lados, más despacio que el escenario:
//   Seguir X = 1   → la capa no se desliza (se ve quieta, como el cielo, muy lejos)
//   Seguir X = 0.5 → se desliza a media velocidad (distancia media)
//   Seguir X = 0   → se desliza igual que el escenario (como si estuviera pegada a él)
// [DefaultExecutionOrder(1000)] = se ejecuta después de que Cinemachine mueve la cámara, para que no tiemble.
[DefaultExecutionOrder(1000)]
public class ParallaxSimple : MonoBehaviour
{
    public Transform camara;                    // la Main Camera (si lo dejas vacío la busca sola)
    [Range(0f, 1f)] public float seguirX = 0.5f;

    float xInicialLocal;
    float camaraXInicial;

    void Start()
    {
        if (camara == null) camara = Camera.main.transform;
        xInicialLocal = transform.localPosition.x;
        camaraXInicial = camara.position.x;
    }

    void LateUpdate()
    {
        // cuánto se ha movido la cámara a los lados desde que empezó el juego
        float recorrido = camara.position.x - camaraXInicial;
        // la capa va con la cámara (es su hija), así que la "devolvemos" un poco hacia atrás:
        // cuanto menos sigue a la cámara, más se nota que se desliza
        Vector3 p = transform.localPosition;
        p.x = xInicialLocal - recorrido * (1f - seguirX);
        transform.localPosition = p;
    }
}
