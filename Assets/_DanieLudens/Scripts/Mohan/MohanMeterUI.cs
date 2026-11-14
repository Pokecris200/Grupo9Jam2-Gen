using UnityEngine;
using UnityEngine.UI;

public class MohanMeterUI : MonoBehaviour
{
    [SerializeField] private Slider slider;             // Bar that shows the meter
    [SerializeField] private float shakeStrength = 4f;  // Shake distance in pixels
    [SerializeField] private float shakeSpeed = 40f;    // How fast it shakes
    [SerializeField] private float escapeShakeMultiplier = 2f; // Stronger shake in the escape phase

    private RectTransform rect;  // Bar position on screen
    private Vector2 basePosition; // Resting position

    // Cache references and start empty
    void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>(); // Use the slider on this object if none assigned
        rect = GetComponent<RectTransform>();                // Get the bar's RectTransform
        basePosition = rect.anchoredPosition;                // Remember where it rests
        slider.value = 0f;                                   // Start empty
    }

    // Listen for meter changes
    void OnEnable()
    {
        MohanMeter.OnMeterChanged += UpdateBar; // Update when the meter rises
    }

    // Stop listening when disabled
    void OnDisable()
    {
        MohanMeter.OnMeterChanged -= UpdateBar; // Avoid calls on a disabled object
    }

    // Show the new meter value
    private void UpdateBar(float value)
    {
        slider.value = value; // Fill the bar
    }

    // Shake the bar while the meter is rising
    void Update()
    {
        MohanMeter meter = MohanMeter.Instance;                // Current meter
        bool shaking = meter != null && meter.IsFilling;       // Only shake while filling

        if (!shaking)
        {
            rect.anchoredPosition = basePosition; // Back to rest
            return;
        }

        float strength = shakeStrength * (meter.EscapePhase ? escapeShakeMultiplier : 1f); // Stronger when escaping
        float x = Mathf.Sin(Time.time * shakeSpeed) * strength;                          // Side to side
        float y = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * strength * 0.5f;            // Smaller up and down
        rect.anchoredPosition = basePosition + new Vector2(x, y);                        // Apply the offset
    }
}