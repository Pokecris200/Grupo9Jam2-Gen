using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject creditsPanel;
    public bool isGameActive;

    [Header("Audio UI")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickSound;

    void Start()
    {
        isGameActive = true;
    }

    // Llama a esta función desde los botones
    public void PlayClickSound()
    {
        if (sfxSource != null && clickSound != null)
        {
            sfxSource.PlayOneShot(clickSound);
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Intro"); // Intro cinematic, then level 1
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenOptions()
    {
        menuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void OpenCredits()
    {
        menuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        creditsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }
    public void closeControls()
    {
        controlsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    public void OpenControls()
    {
        menuPanel.SetActive(false);
        controlsPanel.SetActive(true);
    }
}