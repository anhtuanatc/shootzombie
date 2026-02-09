using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

namespace ShootZombie.UI
{
    /// <summary>
    /// Controls the options/settings menu.
    /// </summary>
    public class OptionsController : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private AudioMixer audioMixer;
        
        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        
        [Header("Gameplay Settings")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle screenShakeToggle;
        
        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button backButton;

        private Resolution[] _resolutions;

        private const string MASTER_VOL_KEY = "MasterVolume";
        private const string MUSIC_VOL_KEY = "MusicVolume";
        private const string SFX_VOL_KEY = "SFXVolume";
        private const string QUALITY_KEY = "QualityLevel";
        private const string SENSITIVITY_KEY = "Sensitivity";
        private const string SCREENSHAKE_KEY = "ScreenShake";

        private void Start()
        {
            SetupUI();
            LoadSettings();
            SetupListeners();
        }

        private void SetupUI()
        {
            // Setup quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }

            // Setup resolution dropdown
            if (resolutionDropdown != null)
            {
                _resolutions = Screen.resolutions;
                resolutionDropdown.ClearOptions();
                var options = new System.Collections.Generic.List<string>();
                int currentIndex = 0;
                
                for (int i = 0; i < _resolutions.Length; i++)
                {
                    string option = $"{_resolutions[i].width} x {_resolutions[i].height}";
                    options.Add(option);
                    
                    if (_resolutions[i].width == Screen.currentResolution.width &&
                        _resolutions[i].height == Screen.currentResolution.height)
                    {
                        currentIndex = i;
                    }
                }
                
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = currentIndex;
            }

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;

            if (vsyncToggle != null)
                vsyncToggle.isOn = QualitySettings.vSyncCount > 0;
        }

        private void SetupListeners()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void LoadSettings()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = PlayerPrefs.GetFloat(MASTER_VOL_KEY, 1f);
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 1f);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = PlayerPrefs.GetFloat(SFX_VOL_KEY, 1f);
            if (sensitivitySlider != null)
                sensitivitySlider.value = PlayerPrefs.GetFloat(SENSITIVITY_KEY, 1f);
            if (screenShakeToggle != null)
                screenShakeToggle.isOn = PlayerPrefs.GetInt(SCREENSHAKE_KEY, 1) == 1;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(value, 0.001f)) * 20);
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(value, 0.001f)) * 20);
        }

        private void OnSFXVolumeChanged(float value)
        {
            if (audioMixer != null)
                audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(value, 0.001f)) * 20);
        }

        public void ApplySettings()
        {
            // Save audio
            if (masterVolumeSlider != null)
                PlayerPrefs.SetFloat(MASTER_VOL_KEY, masterVolumeSlider.value);
            if (musicVolumeSlider != null)
                PlayerPrefs.SetFloat(MUSIC_VOL_KEY, musicVolumeSlider.value);
            if (sfxVolumeSlider != null)
                PlayerPrefs.SetFloat(SFX_VOL_KEY, sfxVolumeSlider.value);

            // Apply quality
            if (qualityDropdown != null)
            {
                QualitySettings.SetQualityLevel(qualityDropdown.value);
                PlayerPrefs.SetInt(QUALITY_KEY, qualityDropdown.value);
            }

            // Apply resolution
            if (resolutionDropdown != null && _resolutions != null)
            {
                Resolution res = _resolutions[resolutionDropdown.value];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            }

            // Apply fullscreen
            if (fullscreenToggle != null)
                Screen.fullScreen = fullscreenToggle.isOn;

            // Apply vsync
            if (vsyncToggle != null)
                QualitySettings.vSyncCount = vsyncToggle.isOn ? 1 : 0;

            // Save misc
            if (sensitivitySlider != null)
                PlayerPrefs.SetFloat(SENSITIVITY_KEY, sensitivitySlider.value);
            if (screenShakeToggle != null)
                PlayerPrefs.SetInt(SCREENSHAKE_KEY, screenShakeToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
        }

        public void OnBackClicked()
        {
            gameObject.SetActive(false);
            transform.parent?.gameObject.SetActive(true);
        }
    }
}
