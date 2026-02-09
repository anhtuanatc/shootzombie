using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShootZombie.UI
{
    /// <summary>
    /// Reusable health bar component.
    /// Can be used for player, enemies, or spawners.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Slider slider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Header("Colors")]
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        
        [Header("Animation")]
        [SerializeField] private float animationSpeed = 5f;
        [SerializeField] private bool animate = true;
        
        [Header("Billboard")]
        [SerializeField] private bool faceCamera = true;

        private float _targetValue;
        private UnityEngine.Camera _mainCamera;

        private void Start()
        {
            _mainCamera = UnityEngine.Camera.main;
            if (slider != null) _targetValue = slider.value;
        }

        private void Update()
        {
            if (animate && slider != null)
            {
                slider.value = Mathf.Lerp(slider.value, _targetValue, animationSpeed * Time.deltaTime);
                UpdateColor(slider.value);
            }

            if (faceCamera && _mainCamera != null)
            {
                transform.LookAt(transform.position + _mainCamera.transform.forward);
            }
        }

        public void SetHealth(float current, float max)
        {
            float percent = Mathf.Clamp01(current / max);
            _targetValue = percent;

            if (!animate && slider != null)
            {
                slider.value = percent;
                UpdateColor(percent);
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            }
        }

        public void SetHealthNormalized(float percent)
        {
            _targetValue = Mathf.Clamp01(percent);
            if (!animate && slider != null)
            {
                slider.value = _targetValue;
                UpdateColor(_targetValue);
            }
        }

        private void UpdateColor(float percent)
        {
            if (fillImage == null) return;

            if (healthGradient != null)
            {
                fillImage.color = healthGradient.Evaluate(percent);
            }
            else
            {
                fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, percent);
            }
        }
    }
}
