using Unity.Cinemachine;
using UnityEngine;

public class CinemachineShake : CinemachineExtension
{
    [SerializeField] private float rumbleMax = 0.12f;     // Strongest shake during the delivery rumble
    [SerializeField] private float rumbleRampTime = 3f;   // Seconds to reach the strongest rumble (match the tension delay)
    [SerializeField] private float burstStrength = 0.35f; // Strong shake when the escape starts or the player is caught
    [SerializeField] private float burstDuration = 0.5f;  // Seconds for a burst to calm down
    [SerializeField] private float shakeFrequency = 25f;  // How fast the camera shakes

    private float shakeStrength; // Current shake size
    private bool rumbling;       // True during the delivery rumble

    // Connect to the camera and listen for the delivery, the escape and the game over
    protected override void OnEnable()
    {
        base.OnEnable();                         // Let Cinemachine connect the extension
        Altar.OnChichasDelivered += StartRumble; // Earth starts shaking
        EscapeSequence.OnEscapeStarted += Burst; // The Mohán arrives
        MohanMeter.OnMohanCaught += Burst;       // The Mohán catches the player
    }

    // Stop listening when disabled
    void OnDisable()
    {
        Altar.OnChichasDelivered -= StartRumble; // Avoid calls on a disabled object
        EscapeSequence.OnEscapeStarted -= Burst; // Avoid calls on a disabled object
        MohanMeter.OnMohanCaught -= Burst;       // Avoid calls on a disabled object
    }

    // Add the shake after Cinemachine has placed the camera
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Noise) return;       // Only once, at the noise stage
        if (deltaTime < 0f) { shakeStrength = 0f; return; }     // Camera was reset: no shake
        state.PositionCorrection += ShakeOffset(deltaTime);     // Move the camera by the shake
    }

    // Start the slow rumble after the delivery
    private void StartRumble()
    {
        rumbling = true; // Shake grows over time
    }

    // Strong shake that calms down
    private void Burst()
    {
        rumbling = false;              // The rumble ends
        shakeStrength = burstStrength; // Jump to a strong shake
    }

    // Calculate this frame's shake
    private Vector3 ShakeOffset(float deltaTime)
    {
        if (rumbling) shakeStrength = Mathf.MoveTowards(shakeStrength, rumbleMax, rumbleMax / rumbleRampTime * deltaTime); // Grow slowly
        else shakeStrength = Mathf.MoveTowards(shakeStrength, 0f, burstStrength / burstDuration * deltaTime);             // Calm down

        if (shakeStrength <= 0f) return Vector3.zero; // No shake

        float t = Time.time * shakeFrequency;                   // Time scaled by frequency
        float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f;       // Smooth random X (-1 to 1)
        float y = (Mathf.PerlinNoise(0f, t + 10f) - 0.5f) * 2f; // Smooth random Y (-1 to 1)
        return new Vector3(x, y, 0f) * shakeStrength;           // Scale by the current strength
    }
}