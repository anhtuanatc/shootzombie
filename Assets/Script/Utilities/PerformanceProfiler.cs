using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ShootZombie.Utilities
{
    /// <summary>
    /// Performance profiler to detect lag and bottlenecks.
    /// Shows FPS, frame time, and method execution times.
    /// </summary>
    public class PerformanceProfiler : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool showProfiler = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;
        [SerializeField] private bool detailedMode = false;
        
        [Header("Warning Thresholds")]
        [SerializeField] private float warningFPS = 30f;
        [SerializeField] private float criticalFPS = 15f;
        [SerializeField] private float warningFrameTime = 33f; // ms
        
        // FPS tracking
        private float _deltaTime;
        private float _fps;
        private float _avgFPS;
        private float _minFPS = float.MaxValue;
        private float _maxFPS = 0f;
        
        // Frame time tracking
        private float _frameTime;
        private float _avgFrameTime;
        private Queue<float> _frameTimeHistory = new Queue<float>();
        private const int HISTORY_SIZE = 60;
        
        // Performance counters
        private int _totalFrames;
        private float _totalTime;
        
        // Stopwatch for profiling
        private static Dictionary<string, Stopwatch> _stopwatches = new Dictionary<string, Stopwatch>();
        private static Dictionary<string, float> _profileResults = new Dictionary<string, float>();
        
        // Memory tracking
        private long _lastMemory;
        private long _currentMemory;
        
        private void Update()
        {
            // Toggle profiler
            if (Input.GetKeyDown(toggleKey))
            {
                showProfiler = !showProfiler;
            }
            
            // Calculate FPS
            _deltaTime = Time.unscaledDeltaTime;
            _fps = 1f / _deltaTime;
            _frameTime = _deltaTime * 1000f; // Convert to ms
            
            // Track history
            _frameTimeHistory.Enqueue(_frameTime);
            if (_frameTimeHistory.Count > HISTORY_SIZE)
            {
                _frameTimeHistory.Dequeue();
            }
            
            // Calculate averages
            _totalFrames++;
            _totalTime += _deltaTime;
            _avgFPS = _totalFrames / _totalTime;
            
            float sumFrameTime = 0f;
            foreach (float ft in _frameTimeHistory)
            {
                sumFrameTime += ft;
            }
            _avgFrameTime = sumFrameTime / _frameTimeHistory.Count;
            
            // Track min/max
            if (_fps < _minFPS) _minFPS = _fps;
            if (_fps > _maxFPS) _maxFPS = _fps;
            
            // Memory tracking
            _currentMemory = System.GC.GetTotalMemory(false);
        }

        private void OnGUI()
        {
            if (!showProfiler) return;
            
            int width = detailedMode ? 500 : 350;
            int height = detailedMode ? 450 : 250;
            
            GUILayout.BeginArea(new Rect(10, 10, width, height));
            
            // Background
            GUI.Box(new Rect(0, 0, width, height), "");
            
            GUILayout.BeginVertical();
            
            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label("🔍 Performance Profiler", titleStyle);
            GUILayout.Label($"Press {toggleKey} to toggle | Detailed: {detailedMode}");
            
            GUILayout.Space(10);
            
            // FPS Section
            DrawFPSSection();
            
            GUILayout.Space(10);
            
            // Frame Time Section
            DrawFrameTimeSection();
            
            GUILayout.Space(10);
            
            // Memory Section
            DrawMemorySection();
            
            if (detailedMode)
            {
                GUILayout.Space(10);
                DrawDetailedSection();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawFPSSection()
        {
            GUILayout.Label("📊 FPS:");
            
            // Current FPS with color coding
            Color originalColor = GUI.color;
            if (_fps < criticalFPS)
                GUI.color = Color.red;
            else if (_fps < warningFPS)
                GUI.color = Color.yellow;
            else
                GUI.color = Color.green;
            
            GUILayout.Label($"  Current: {_fps:F1} FPS");
            GUI.color = originalColor;
            
            GUILayout.Label($"  Average: {_avgFPS:F1} FPS");
            GUILayout.Label($"  Min: {_minFPS:F1} | Max: {_maxFPS:F1}");
            
            // Warning
            if (_fps < warningFPS)
            {
                GUI.color = Color.red;
                GUILayout.Label($"  ⚠ WARNING: Low FPS detected!");
                GUI.color = originalColor;
            }
        }

        private void DrawFrameTimeSection()
        {
            GUILayout.Label("⏱ Frame Time:");
            
            Color originalColor = GUI.color;
            if (_frameTime > warningFrameTime)
                GUI.color = Color.red;
            else if (_frameTime > warningFrameTime * 0.7f)
                GUI.color = Color.yellow;
            else
                GUI.color = Color.green;
            
            GUILayout.Label($"  Current: {_frameTime:F2} ms");
            GUI.color = originalColor;
            
            GUILayout.Label($"  Average: {_avgFrameTime:F2} ms");
            GUILayout.Label($"  Target: 16.67 ms (60 FPS)");
            
            // Frame budget bar
            float budgetUsed = _frameTime / 16.67f;
            DrawProgressBar(budgetUsed, "Frame Budget");
        }

        private void DrawMemorySection()
        {
            GUILayout.Label("💾 Memory:");
            
            float memoryMB = _currentMemory / 1024f / 1024f;
            GUILayout.Label($"  Current: {memoryMB:F2} MB");
            
            if (_lastMemory > 0)
            {
                float delta = (_currentMemory - _lastMemory) / 1024f / 1024f;
                string deltaStr = delta >= 0 ? $"+{delta:F2}" : $"{delta:F2}";
                GUILayout.Label($"  Delta: {deltaStr} MB");
            }
            
            _lastMemory = _currentMemory;
        }

        private void DrawDetailedSection()
        {
            GUILayout.Label("🔬 Detailed Info:");
            GUILayout.Label($"  Total Frames: {_totalFrames}");
            GUILayout.Label($"  Uptime: {_totalTime:F1}s");
            GUILayout.Label($"  Time Scale: {Time.timeScale:F2}");
            GUILayout.Label($"  Fixed Delta: {Time.fixedDeltaTime * 1000:F2} ms");
            
            // Profile results
            if (_profileResults.Count > 0)
            {
                GUILayout.Space(5);
                GUILayout.Label("⏲ Method Timings:");
                foreach (var kvp in _profileResults)
                {
                    Color originalColor = GUI.color;
                    if (kvp.Value > 5f) GUI.color = Color.red;
                    else if (kvp.Value > 2f) GUI.color = Color.yellow;
                    
                    GUILayout.Label($"  {kvp.Key}: {kvp.Value:F2} ms");
                    GUI.color = originalColor;
                }
            }
        }

        private void DrawProgressBar(float value, string label)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"  {label}:", GUILayout.Width(100));
            
            Rect barRect = GUILayoutUtility.GetRect(200, 20);
            
            // Background
            GUI.Box(barRect, "");
            
            // Fill
            Color fillColor = Color.green;
            if (value > 1f) fillColor = Color.red;
            else if (value > 0.8f) fillColor = Color.yellow;
            
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * Mathf.Min(value, 1f), barRect.height);
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            
            // Percentage
            GUI.Label(barRect, $"{value * 100:F0}%", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });
            
            GUILayout.EndHorizontal();
        }

        // Static profiling methods
        public static void BeginProfile(string name)
        {
            if (!_stopwatches.ContainsKey(name))
            {
                _stopwatches[name] = new Stopwatch();
            }
            
            _stopwatches[name].Restart();
        }

        public static void EndProfile(string name)
        {
            if (_stopwatches.ContainsKey(name))
            {
                _stopwatches[name].Stop();
                float ms = (float)_stopwatches[name].Elapsed.TotalMilliseconds;
                _profileResults[name] = ms;
                
                if (ms > 5f)
                {
                    UnityEngine.Debug.LogWarning($"[Profiler] {name} took {ms:F2}ms - SLOW!");
                }
            }
        }

        [ContextMenu("Reset Stats")]
        public void ResetStats()
        {
            _totalFrames = 0;
            _totalTime = 0;
            _minFPS = float.MaxValue;
            _maxFPS = 0;
            _frameTimeHistory.Clear();
            _profileResults.Clear();
            UnityEngine.Debug.Log("[Profiler] Stats reset");
        }

        [ContextMenu("Toggle Detailed Mode")]
        public void ToggleDetailedMode()
        {
            detailedMode = !detailedMode;
        }

        [ContextMenu("Force GC")]
        public void ForceGarbageCollection()
        {
            System.GC.Collect();
            UnityEngine.Debug.Log("[Profiler] Forced garbage collection");
        }
    }
}
