using System.Collections.Generic;
using UnityEngine;

namespace ShootZombie.Core
{
    /// <summary>
    /// Generic object pooling system for performance optimization.
    /// Reuses game objects instead of constantly creating and destroying them.
    /// </summary>
    public class ObjectPool : Singleton<ObjectPool>
    {
        #region Pool Data Structure
        
        [System.Serializable]
        public class Pool
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 10;
            public bool expandable = true;
        }
        
        #endregion

        #region Inspector Fields
        
        [SerializeField] 
        private List<Pool> pools = new List<Pool>();
        
        #endregion

        #region Private Fields
        
        private Dictionary<string, Queue<GameObject>> _poolDictionary;
        private Dictionary<string, Pool> _poolConfigs;
        private Dictionary<string, Transform> _poolParents;
        
        #endregion

        #region Initialization
        
        protected override void OnSingletonAwake()
        {
            InitializePools();
        }

        private void InitializePools()
        {
            _poolDictionary = new Dictionary<string, Queue<GameObject>>();
            _poolConfigs = new Dictionary<string, Pool>();
            _poolParents = new Dictionary<string, Transform>();

            foreach (Pool pool in pools)
            {
                CreatePool(pool);
            }
        }

        private void CreatePool(Pool pool)
        {
            if (_poolDictionary.ContainsKey(pool.tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool with tag '{pool.tag}' already exists!");
                return;
            }

            Queue<GameObject> objectPool = new Queue<GameObject>();
            
            // Create parent container for organization
            GameObject parentObj = new GameObject($"Pool_{pool.tag}");
            parentObj.transform.SetParent(transform);
            _poolParents[pool.tag] = parentObj.transform;

            // Pre-instantiate objects
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreatePooledObject(pool.prefab, parentObj.transform);
                objectPool.Enqueue(obj);
            }

            _poolDictionary[pool.tag] = objectPool;
            _poolConfigs[pool.tag] = pool;
        }

        private GameObject CreatePooledObject(GameObject prefab, Transform parent)
        {
            GameObject obj = Instantiate(prefab, parent);
            obj.SetActive(false);
            
            // Add pooled object component for tracking
            var pooledObj = obj.GetComponent<PooledObject>();
            if (pooledObj == null)
            {
                pooledObj = obj.AddComponent<PooledObject>();
            }
            
            return obj;
        }
        
        #endregion

        #region Public API
        
        /// <summary>
        /// Spawns an object from the pool.
        /// </summary>
        /// <param name="tag">The pool tag</param>
        /// <param name="position">Spawn position</param>
        /// <param name="rotation">Spawn rotation</param>
        /// <returns>The spawned object, or null if pool doesn't exist</returns>
        public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool with tag '{tag}' doesn't exist!");
                return null;
            }

            Queue<GameObject> pool = _poolDictionary[tag];
            GameObject objectToSpawn;

            if (pool.Count > 0)
            {
                objectToSpawn = pool.Dequeue();
            }
            else
            {
                // Pool is empty - expand if allowed
                Pool config = _poolConfigs[tag];
                if (config.expandable)
                {
                    objectToSpawn = CreatePooledObject(config.prefab, _poolParents[tag]);
                    Debug.Log($"[ObjectPool] Pool '{tag}' expanded. Consider increasing initial size.");
                }
                else
                {
                    Debug.LogWarning($"[ObjectPool] Pool '{tag}' is empty and not expandable!");
                    return null;
                }
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            // Notify the object it was spawned
            var pooledObj = objectToSpawn.GetComponent<PooledObject>();
            if (pooledObj != null)
            {
                pooledObj.OnSpawn();
            }

            // Also call IPoolable interface if implemented
            var poolable = objectToSpawn.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnSpawnFromPool();
            }

            return objectToSpawn;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="tag">The pool tag</param>
        /// <param name="objectToReturn">The object to return</param>
        public void Despawn(string tag, GameObject objectToReturn)
        {
            if (!_poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPool] Pool with tag '{tag}' doesn't exist! Destroying object instead.");
                Destroy(objectToReturn);
                return;
            }

            // Notify the object it's being despawned
            var poolable = objectToReturn.GetComponent<IPoolable>();
            if (poolable != null)
            {
                poolable.OnReturnToPool();
            }

            objectToReturn.SetActive(false);
            objectToReturn.transform.SetParent(_poolParents[tag]);
            _poolDictionary[tag].Enqueue(objectToReturn);
        }

        /// <summary>
        /// Creates a new pool at runtime.
        /// </summary>
        public void CreateNewPool(string tag, GameObject prefab, int initialSize = 10, bool expandable = true)
        {
            Pool newPool = new Pool
            {
                tag = tag,
                prefab = prefab,
                initialSize = initialSize,
                expandable = expandable
            };
            
            CreatePool(newPool);
        }

        /// <summary>
        /// Checks if a pool with the given tag exists.
        /// </summary>
        public bool HasPool(string tag)
        {
            return _poolDictionary != null && _poolDictionary.ContainsKey(tag);
        }

        /// <summary>
        /// Gets the number of available objects in a pool.
        /// </summary>
        public int GetPoolSize(string tag)
        {
            if (!_poolDictionary.ContainsKey(tag)) return 0;
            return _poolDictionary[tag].Count;
        }

        /// <summary>
        /// Clears all pools and destroys all pooled objects.
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var parent in _poolParents.Values)
            {
                if (parent != null)
                {
                    Destroy(parent.gameObject);
                }
            }

            _poolDictionary?.Clear();
            _poolParents?.Clear();
        }
        
        #endregion
    }

    /// <summary>
    /// Interface for objects that need to respond to pool events.
    /// </summary>
    public interface IPoolable
    {
        void OnSpawnFromPool();
        void OnReturnToPool();
    }

    /// <summary>
    /// Component added to pooled objects for tracking.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string PoolTag { get; set; }
        
        public void OnSpawn()
        {
            // Override in derived classes if needed
        }

        /// <summary>
        /// Returns this object to its pool.
        /// </summary>
        public void ReturnToPool()
        {
            if (!string.IsNullOrEmpty(PoolTag) && ObjectPool.HasInstance)
            {
                ObjectPool.Instance.Despawn(PoolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
