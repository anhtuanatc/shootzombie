using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ShootZombie.UI
{
    /// <summary>
    /// Enhanced button with hover/click animations and sounds.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale Animation")]
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float clickScale = 0.95f;
        [SerializeField] private float animationSpeed = 10f;
        
        [Header("Color Animation")]
        [SerializeField] private bool useColorAnimation = true;
        [SerializeField] private Color hoverColor = Color.white;
        
        [Header("Audio")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] [Range(0f, 1f)] private float volume = 0.5f;

        private Vector3 _originalScale;
        private Vector3 _targetScale;
        private Image _image;
        private Color _originalColor;
        private AudioSource _audioSource;
        private bool _isHovered;
        private bool _isPressed;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _targetScale = _originalScale;
            _image = GetComponent<Image>();
            if (_image != null) _originalColor = _image.color;
            
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && (hoverSound != null || clickSound != null))
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, animationSpeed * Time.unscaledDeltaTime);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _targetScale = _originalScale * hoverScale;
            
            if (useColorAnimation && _image != null)
                _image.color = hoverColor;
                
            PlaySound(hoverSound);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            _targetScale = _originalScale;
            
            if (useColorAnimation && _image != null)
                _image.color = _originalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _targetScale = _originalScale * clickScale;
            PlaySound(clickSound);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            _targetScale = _isHovered ? _originalScale * hoverScale : _originalScale;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
