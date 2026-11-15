using UnityEngine;

// SCRIPT DE PRUEBA: mover un cuadradito para probar el escenario.
// Izquierda / derecha con A-D o las flechas, salto con Espacio. Nada más.
// Necesita en el mismo objeto: Rigidbody 2D (Dynamic) y un Collider 2D (por ejemplo Box Collider 2D).
public class MovimientoPrueba : MonoBehaviour
{
    public float velocidad = 5f;     // unidades por segundo (1 unidad = 1 tile)
    public float fuerzaSalto = 10f;  // más alto = salta más

    Rigidbody2D cuerpo;
    bool enSuelo;

    void Start()
    {
        cuerpo = GetComponent<Rigidbody2D>();
        cuerpo.freezeRotation = true;   // para que el cuadrado no se voltee
    }

    void Update()
    {
        // -1 = izquierda, 0 = quieto, 1 = derecha
        float direccion = Input.GetAxisRaw("Horizontal");
        cuerpo.linearVelocity = new Vector2(direccion * velocidad, cuerpo.linearVelocity.y);

        // saltar solo si está tocando el suelo
        if (Input.GetKeyDown(KeyCode.Space) && enSuelo)
            cuerpo.linearVelocity = new Vector2(cuerpo.linearVelocity.x, fuerzaSalto);
    }

    // Unity llama a esto mientras el cuadrado toca algo con collider.
    // Si lo que toca está debajo de él, cuenta como suelo.
    void OnCollisionStay2D(Collision2D choque)
    {
        foreach (var punto in choque.contacts)
            if (punto.normal.y > 0.5f) { enSuelo = true; return; }
    }

    void OnCollisionExit2D(Collision2D choque)
    {
        enSuelo = false;
    }
}
