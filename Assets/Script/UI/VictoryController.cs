using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShootZombie.Core;
using System.Collections;

namespace ShootZombie.UI
{
    /// <summary>
    /// Controls the victory screen UI.
    /// </summary>
    public class VictoryController : MonoBehaviour
    {
        [Header("Stats Display")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI newHighScoreText;
        
        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Effects")]
        [SerializeField] private ParticleSystem confettiEffect;

        private int _finalScore, _kills, _wave;
        private bool _isNewHighScore;

        private void Start()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnEnable()
        {
            if (confettiEffect != null) confettiEffect.Play();
            StartCoroutine(ShowStatsAnimated());
        }

        public void SetStats(int score, int kills, int wave)
        {
            _finalScore = score;
            _kills = kills;
            _wave = wave;
            _isNewHighScore = GameManager.HasInstance && score >= GameManager.Instance.HighScore && score > 0;
        }

        private IEnumerator ShowStatsAnimated()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            
            if (titleText != null) titleText.text = "VICTORY!";
            if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {_finalScore:N0}";
            if (killsText != null) killsText.text = $"TOTAL KILLS: {_kills}";
            if (waveText != null) waveText.text = $"WAVES CLEARED: {_wave}";
            if (highScoreText != null && GameManager.HasInstance)
                highScoreText.text = $"HIGH SCORE: {GameManager.Instance.HighScore:N0}";
            if (newHighScoreText != null)
                newHighScoreText.gameObject.SetActive(_isNewHighScore);
        }

        public void OnRestartClicked()
        {
            if (GameManager.HasInstance) GameManager.Instance.RestartGame();
        }

        public void OnMainMenuClicked()
        {
            if (GameManager.HasInstance) GameManager.Instance.GoToMainMenu();
        }
    }
}
