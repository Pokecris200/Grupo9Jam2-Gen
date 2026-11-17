using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MasterKey = "MasterVolume";
    private const string MusicKey = "MusicVolume";
    private const string SFXKey = "SFXVolume";

    private void Start()
    {
        Setup(masterSlider, MasterKey);
        Setup(musicSlider, MusicKey);
        Setup(sfxSlider, SFXKey);
    }

    private void Setup(Slider slider, string key)
    {
        float saved = PlayerPrefs.GetFloat(key, 1f);
        slider.value = saved;
        ApplyVolume(key, saved);
        slider.onValueChanged.AddListener(value =>
        {
            ApplyVolume(key, value);
            PlayerPrefs.SetFloat(key, value);
        });
    }

    private void ApplyVolume(string key, float value)
    {
        // El mixer usa decibelios, por eso se convierte el valor del slider
        mixer.SetFloat(key, Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f);
    }
}