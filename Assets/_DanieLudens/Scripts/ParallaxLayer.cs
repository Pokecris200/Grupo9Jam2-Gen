using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.5f;   // Speed on screen relative to the level (0 = still, 1 = like the level, >1 = in front)
    [SerializeField] private bool followCameraY = true;  // Keep the layer vertically centered on the camera
    [SerializeField] private float yOffset = 0f;         // Vertical offset from the camera when following it

    private Transform cam;     // Main camera
    private float startX;      // Layer X when the game started
    private float camStartX;   // Camera X when the game started
    private float startY;      // Layer Y when the game started
    private float tileWidth;   // Width of one repetition of the art

    // Save the starting positions and the art width
    void Start()
    {
        cam = Camera.main.transform;                                         // Get the camera
        startX = transform.position.x;                                       // Layer start X
        startY = transform.position.y;                                       // Layer start Y
        camStartX = cam.position.x;                                          // Camera start X
        SpriteRenderer sr = GetComponent<SpriteRenderer>();                  // Layer sprite
        tileWidth = sr.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x); // One repetition (8 units for 1024 px at PPU 128)
    }

    // Move the layer after the camera has moved
    void LateUpdate()
    {
        float camDelta = cam.position.x - camStartX;              // How far the camera has traveled
        float x = startX + camDelta * (1f - scrollSpeed);          // Far layers follow the camera, so they look slower
        x += Mathf.Round((cam.position.x - x) / tileWidth) * tileWidth; // Jump whole repetitions to stay around the camera (invisible)

        float y = followCameraY ? cam.position.y + yOffset : startY; // Follow the camera vertically or stay fixed
        transform.position = new Vector3(x, y, transform.position.z); // Apply the new position
    }
}
