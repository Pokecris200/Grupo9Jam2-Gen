using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject optionsPanel;
    public bool isGameActive;
    private static GameManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
        Instance = this;
        isGameActive = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
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
}

