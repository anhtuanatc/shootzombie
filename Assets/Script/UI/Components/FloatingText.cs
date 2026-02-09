using UnityEngine;
using TMPro;

namespace ShootZombie.UI
{
    /// <summary>
    /// Floating damage/score text that appears and fades out.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float fadeSpeed = 2f;
        [SerializeField] private Vector3 randomOffset = new Vector3(0.5f, 0, 0.5f);

        private float _timer;
        private Color _originalColor;
        private Vector3 _velocity;
        private UnityEngine.Camera _mainCamera;

        private void Awake()
        {
            if (text == null) text = GetComponent<TextMeshProUGUI>();
            if (text != null) _originalColor = text.color;
            _mainCamera = UnityEngine.Camera.main;
        }

        private void OnEnable()
        {
            _timer = 0f;
            if (text != null) text.color = _originalColor;
            
            // Apply random offset
            transform.position += new Vector3(
                Random.Range(-randomOffset.x, randomOffset.x),
                0,
                Random.Range(-randomOffset.z, randomOffset.z)
            );
            
            _velocity = Vector3.up * floatSpeed;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            
            // Float upward
            transform.position += _velocity * Time.deltaTime;
            
            // Face camera
            if (_mainCamera != null)
            {
                transform.LookAt(transform.position + _mainCamera.transform.forward);
            }
            
            // Fade out
            if (text != null)
            {
                float alpha = Mathf.Lerp(1f, 0f, _timer / lifetime);
                text.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);
            }
            
            if (_timer >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        public void SetText(string value, Color? color = null)
        {
            if (text != null)
            {
                text.text = value;
                if (color.HasValue)
                {
                    _originalColor = color.Value;
                    text.color = _originalColor;
                }
            }
        }

        public void SetDamage(int damage)
        {
            SetText($"-{damage}", Color.red);
        }

        public void SetScore(int score)
        {
            SetText($"+{score}", Color.yellow);
        }

        public void SetHeal(int heal)
        {
            SetText($"+{heal}", Color.green);
        }
    }
}
