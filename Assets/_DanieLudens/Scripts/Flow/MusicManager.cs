using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Clips")]
    [SerializeField] private AudioClip mainMusic;       // Música principal de exploración
    [SerializeField] private AudioClip chaseMusic;      // Música de persecución (ej. Final Rush)

    [Header("Transición")]
    [SerializeField] private float fadeDuration = 1.0f; // Segundos que tarda la transición suave (0 = corte directo)
    [SerializeField] private bool stopOnGameOver = true; // Si se silencia suavemente al perder

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private float initialVolume = 1.0f;

    void Awake()
    {
        // Configurar Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        initialVolume = audioSource.volume > 0f ? audioSource.volume : 1.0f;

        // Si ya hay un clip sonando, asegurarnos de registrarlo
        if (audioSource.clip != null)
        {
            mainMusic = audioSource.clip;
        }
        else if (mainMusic != null)
        {
            audioSource.clip = mainMusic;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void OnEnable()
    {
        // Escucha el evento en que comienza la persecución (mismo evento que usa MohanFace.cs)
        EscapeSequence.OnEscapeStarted += OnChaseStarted;

        // Opcional: Detener música al ser atrapado por el Mohán
        if (stopOnGameOver)
        {
            MohanMeter.OnMohanCaught += OnGameOver;
        }
    }

    void OnDisable()
    {
        EscapeSequence.OnEscapeStarted -= OnChaseStarted;

        if (stopOnGameOver)
        {
            MohanMeter.OnMohanCaught -= OnGameOver;
        }
    }

    // Se activa cuando la secuencia de escape / persecución inicia
    private void OnChaseStarted()
    {
        if (chaseMusic != null)
        {
            ChangeMusic(chaseMusic);
        }
        else
        {
            Debug.LogWarning("[MusicManager] No se ha asignado chaseMusic en el Inspector.");
        }
    }

    // Se activa cuando el jugador pierde
    private void OnGameOver()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut(0.8f));
    }

    /// <summary>
    /// Cambia la música actual con una transición suave (fade out -> cambio de clip -> fade in).
    /// </summary>
    public void ChangeMusic(AudioClip newClip)
    {
        if (audioSource.clip == newClip) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (fadeDuration > 0f && gameObject.activeInHierarchy)
        {
            fadeCoroutine = StartCoroutine(CrossfadeRoutine(newClip));
        }
        else
        {
            audioSource.Stop();
            audioSource.clip = newClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        float startVol = audioSource.volume;
        float halfFade = fadeDuration * 0.5f;

        // 1. Fade out de la música actual
        for (float t = 0; t < halfFade; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / halfFade);
            yield return null;
        }
        audioSource.volume = 0f;

        // 2. Cambiar de clip e iniciar reproducción
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.loop = true;
        audioSource.Play();

        // 3. Fade in de la nueva música
        for (float t = 0; t < halfFade; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, initialVolume, t / halfFade);
            yield return null;
        }
        audioSource.volume = initialVolume;
        fadeCoroutine = null;
    }

    private IEnumerator FadeOut(float duration)
    {
        float startVol = audioSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        audioSource.Stop();
        audioSource.volume = initialVolume;
        fadeCoroutine = null;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
