using UnityEngine;

namespace ShootZombie.Utilities
{
    /// <summary>
    /// Camera performance tester - disable camera to see if it causes lag.
    /// </summary>
    public class CameraProfiler : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private KeyCode toggleCameraKey = KeyCode.F2;
        [SerializeField] private KeyCode toggleSmoothingKey = KeyCode.F3;
        [SerializeField] private bool showInfo = true;
        
        private Camera.CameraController _cameraController;
        private bool _cameraEnabled = true;
        private bool _smoothingEnabled = true;
        private float _fpsWithCamera;
        private float _fpsWithoutCamera;
        private int _frameCount;
        private float _deltaSum;

        private void Start()
        {
            _cameraController = FindObjectOfType<Camera.CameraController>();
            if (_cameraController == null)
            {
                Debug.LogWarning("[CameraProfiler] No CameraController found!");
                enabled = false;
            }
        }

        private void Update()
        {
            // Toggle camera
            if (Input.GetKeyDown(toggleCameraKey))
            {
                _cameraEnabled = !_cameraEnabled;
                if (_cameraController != null)
                {
                    _cameraController.enabled = _cameraEnabled;
                }
                
                Debug.Log($"[CameraProfiler] Camera {(_cameraEnabled ? "ENABLED" : "DISABLED")}");
                ResetStats();
            }
            
            // Toggle smoothing
            if (Input.GetKeyDown(toggleSmoothingKey) && _cameraController != null)
            {
                _smoothingEnabled = !_smoothingEnabled;
                // Note: You'll need to add a public property to CameraController
                Debug.Log($"[CameraProfiler] Smoothing {(_smoothingEnabled ? "ENABLED" : "DISABLED")}");
                Debug.LogWarning("Note: Smoothing toggle requires public property in CameraController");
            }
            
            // Track FPS
            _frameCount++;
            _deltaSum += Time.unscaledDeltaTime;
            
            if (_frameCount >= 60)
            {
                float avgFPS = _frameCount / _deltaSum;
                
                if (_cameraEnabled)
                {
                    _fpsWithCamera = avgFPS;
                }
                else
                {
                    _fpsWithoutCamera = avgFPS;
                }
                
                _frameCount = 0;
                _deltaSum = 0;
            }
        }

        private void OnGUI()
        {
            if (!showInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 270, 400, 200));
            GUI.Box(new Rect(0, 0, 390, 190), "");
            
            GUILayout.BeginVertical();
            
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 14;
            titleStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label("📷 Camera Profiler", titleStyle);
            
            GUILayout.Space(5);
            
            // Status
            Color originalColor = GUI.color;
            GUI.color = _cameraEnabled ? Color.green : Color.red;
            GUILayout.Label($"Camera: {(_cameraEnabled ? "ENABLED" : "DISABLED")}");
            GUI.color = originalColor;
            
            GUILayout.Space(5);
            
            // FPS comparison
            GUILayout.Label("FPS Comparison:");
            GUILayout.Label($"  With Camera: {_fpsWithCamera:F1} FPS");
            GUILayout.Label($"  Without Camera: {_fpsWithoutCamera:F1} FPS");
            
            if (_fpsWithCamera > 0 && _fpsWithoutCamera > 0)
            {
                float diff = _fpsWithoutCamera - _fpsWithCamera;
                float percent = (diff / _fpsWithCamera) * 100f;
                
                if (Mathf.Abs(diff) > 5f)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"  Difference: {diff:F1} FPS ({percent:F1}%)");
                    
                    if (diff > 10f)
                    {
                        GUI.color = Color.red;
                        GUILayout.Label("  ⚠ Camera is causing significant lag!");
                    }
                    GUI.color = originalColor;
                }
                else
                {
                    GUI.color = Color.green;
                    GUILayout.Label("  ✅ Camera impact is minimal");
                    GUI.color = originalColor;
                }
            }
            
            GUILayout.Space(10);
            
            // Controls
            GUILayout.Label("Controls:");
            GUILayout.Label($"  {toggleCameraKey}: Toggle Camera");
            GUILayout.Label($"  {toggleSmoothingKey}: Toggle Smoothing");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void ResetStats()
        {
            _frameCount = 0;
            _deltaSum = 0;
        }

        [ContextMenu("Test: Disable Camera for 5 seconds")]
        public void TestDisableCamera()
        {
            StartCoroutine(TestCameraImpact());
        }

        private System.Collections.IEnumerator TestCameraImpact()
        {
            Debug.Log("[CameraProfiler] Starting camera impact test...");
            
            // Measure with camera
            _cameraController.enabled = true;
            yield return new WaitForSeconds(1f);
            
            float fpsSum = 0;
            for (int i = 0; i < 60; i++)
            {
                fpsSum += 1f / Time.unscaledDeltaTime;
                yield return null;
            }
            _fpsWithCamera = fpsSum / 60f;
            
            Debug.Log($"[CameraProfiler] FPS with camera: {_fpsWithCamera:F1}");
            
            // Measure without camera
            _cameraController.enabled = false;
            yield return new WaitForSeconds(1f);
            
            fpsSum = 0;
            for (int i = 0; i < 60; i++)
            {
                fpsSum += 1f / Time.unscaledDeltaTime;
                yield return null;
            }
            _fpsWithoutCamera = fpsSum / 60f;
            
            Debug.Log($"[CameraProfiler] FPS without camera: {_fpsWithoutCamera:F1}");
            
            // Re-enable camera
            _cameraController.enabled = true;
            
            // Report
            float diff = _fpsWithoutCamera - _fpsWithCamera;
            if (Mathf.Abs(diff) > 5f)
            {
                Debug.LogWarning($"[CameraProfiler] Camera causes {diff:F1} FPS drop!");
            }
            else
            {
                Debug.Log($"[CameraProfiler] Camera impact is minimal ({diff:F1} FPS)");
            }
        }
    }
}
