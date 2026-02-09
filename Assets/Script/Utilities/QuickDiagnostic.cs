using UnityEngine;

namespace ShootZombie.Utilities
{
    /// <summary>
    /// Quick diagnostic tool to check common performance issues.
    /// Attach to any GameObject and check console for warnings.
    /// </summary>
    public class QuickDiagnostic : MonoBehaviour
    {
        [Header("Auto-Run on Start")]
        [SerializeField] private bool runOnStart = true;
        
        private void Start()
        {
            if (runOnStart)
            {
                RunDiagnostics();
            }
        }

        [ContextMenu("Run Diagnostics")]
        public void RunDiagnostics()
        {
            Debug.Log("=== 🔍 QUICK DIAGNOSTICS ===");
            
            CheckVSync();
            CheckQualitySettings();
            CheckPhysicsSettings();
            CheckPlayerSetup();
            CheckCameraSetup();
            CheckSceneObjects();
            
            Debug.Log("=== ✅ DIAGNOSTICS COMPLETE ===");
        }

        private void CheckVSync()
        {
            Debug.Log("\n📺 VSync Settings:");
            Debug.Log($"  VSync Count: {QualitySettings.vSyncCount}");
            
            if (QualitySettings.vSyncCount == 0)
            {
                Debug.LogWarning("  ⚠ VSync is OFF - may cause screen tearing and inconsistent framerate!");
                Debug.Log("  💡 Fix: Edit → Project Settings → Quality → VSync Count = Every V Blank");
            }
            else
            {
                Debug.Log("  ✅ VSync enabled");
            }
            
            Debug.Log($"  Target Frame Rate: {Application.targetFrameRate}");
        }

        private void CheckQualitySettings()
        {
            Debug.Log("\n🎨 Quality Settings:");
            Debug.Log($"  Quality Level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            Debug.Log($"  Pixel Light Count: {QualitySettings.pixelLightCount}");
            Debug.Log($"  Shadow Distance: {QualitySettings.shadowDistance}");
            Debug.Log($"  Shadow Resolution: {QualitySettings.shadowResolution}");
            
            if (QualitySettings.shadowDistance > 100f)
            {
                Debug.LogWarning($"  ⚠ Shadow distance is high ({QualitySettings.shadowDistance}) - may impact performance!");
            }
        }

        private void CheckPhysicsSettings()
        {
            Debug.Log("\n⚙ Physics Settings:");
            Debug.Log($"  Fixed Timestep: {Time.fixedDeltaTime} ({1f / Time.fixedDeltaTime:F0} Hz)");
            Debug.Log($"  Maximum Allowed Timestep: {Time.maximumDeltaTime}");
            
            if (Time.fixedDeltaTime != 0.02f)
            {
                Debug.LogWarning($"  ⚠ Fixed Timestep is not default (0.02)!");
                Debug.Log("  💡 Recommended: 0.02 (50Hz) or 0.01667 (60Hz)");
            }
            else
            {
                Debug.Log("  ✅ Fixed Timestep is default (50Hz)");
            }
        }

        private void CheckPlayerSetup()
        {
            Debug.Log("\n🧑 Player Setup:");
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("  ❌ No Player found with 'Player' tag!");
                return;
            }
            
            Debug.Log($"  Player: {player.name}");
            
            // Check Rigidbody
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"  Rigidbody:");
                Debug.Log($"    Interpolation: {rb.interpolation}");
                Debug.Log($"    Collision Detection: {rb.collisionDetectionMode}");
                Debug.Log($"    Constraints: {rb.constraints}");
                
                if (rb.interpolation == RigidbodyInterpolation.None)
                {
                    Debug.LogWarning("    ⚠ Rigidbody Interpolation is NONE - will cause jitter!");
                    Debug.Log("    💡 Fix: Set to Interpolate");
                }
                
                if (rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
                {
                    Debug.LogWarning("    ⚠ Collision Detection is Discrete - may miss fast collisions!");
                    Debug.Log("    💡 Fix: Set to Continuous");
                }
                
                if (rb.constraints == RigidbodyConstraints.None)
                {
                    Debug.LogWarning("    ⚠ No rotation constraints - player may rotate unexpectedly!");
                    Debug.Log("    💡 Fix: Freeze Rotation XYZ");
                }
            }
            else
            {
                Debug.Log("  No Rigidbody (using CharacterController or Transform)");
            }
            
            // Check CharacterController
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                Debug.Log($"  CharacterController:");
                Debug.Log($"    Radius: {cc.radius}");
                Debug.Log($"    Height: {cc.height}");
            }
        }

        private void CheckCameraSetup()
        {
            Debug.Log("\n📷 Camera Setup:");
            
            var mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                Debug.LogError("  ❌ No Main Camera found!");
                return;
            }
            
            Debug.Log($"  Main Camera: {mainCam.name}");
            Debug.Log($"  Clear Flags: {mainCam.clearFlags}");
            Debug.Log($"  Culling Mask: {mainCam.cullingMask}");
            Debug.Log($"  Far Clip Plane: {mainCam.farClipPlane}");
            
            if (mainCam.farClipPlane > 500f)
            {
                Debug.LogWarning($"  ⚠ Far clip plane is very high ({mainCam.farClipPlane}) - may impact performance!");
            }
        }

        private void CheckSceneObjects()
        {
            Debug.Log("\n🌍 Scene Objects:");
            
            int totalObjects = FindObjectsOfType<GameObject>().Length;
            int activeObjects = FindObjectsOfType<GameObject>(false).Length;
            int renderers = FindObjectsOfType<Renderer>().Length;
            int lights = FindObjectsOfType<Light>().Length;
            int colliders = FindObjectsOfType<Collider>().Length;
            
            Debug.Log($"  Total GameObjects: {totalObjects}");
            Debug.Log($"  Active GameObjects: {activeObjects}");
            Debug.Log($"  Renderers: {renderers}");
            Debug.Log($"  Lights: {lights}");
            Debug.Log($"  Colliders: {colliders}");
            
            if (lights > 8)
            {
                Debug.LogWarning($"  ⚠ Many lights in scene ({lights}) - may impact performance!");
                Debug.Log("  💡 Consider using baked lighting");
            }
            
            if (renderers > 1000)
            {
                Debug.LogWarning($"  ⚠ Many renderers ({renderers}) - consider object pooling!");
            }
        }

        [ContextMenu("Check for Lag Sources")]
        public void CheckLagSources()
        {
            Debug.Log("\n🐌 Checking for common lag sources...");
            
            // Check for expensive operations in Update
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
            Debug.Log($"  Total MonoBehaviours: {allMonoBehaviours.Length}");
            
            if (allMonoBehaviours.Length > 500)
            {
                Debug.LogWarning($"  ⚠ Many MonoBehaviours ({allMonoBehaviours.Length}) - each Update() adds overhead!");
            }
            
            // Check for Find operations
            Debug.Log("\n  💡 Common lag causes:");
            Debug.Log("    - GameObject.Find() in Update/FixedUpdate");
            Debug.Log("    - GetComponent() in Update/FixedUpdate (cache it!)");
            Debug.Log("    - Instantiate/Destroy in tight loops (use pooling!)");
            Debug.Log("    - String operations in Update");
            Debug.Log("    - LINQ queries in Update");
            Debug.Log("    - Physics raycasts without layermask");
        }
    }
}
