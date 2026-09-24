using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pooler genérico para GameObjects, pensado como utility.
/// Uso rápido (estático, sin configurar nada en el inspector):
///
///     GameObject bullet = ObjectPooler.Spawn(bulletPrefab, position, rotation);
///     ObjectPooler.Despawn(bullet);
///
/// Uso avanzado (con pools predefinidos y warm-up), configurando pools en el inspector
/// o registrándolos por código con ObjectPooler.CreatePool(...).
/// </summary>
[DisallowMultipleComponent]
public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string id;              // Identificador del pool (por defecto, nombre del prefab)
        public GameObject prefab;
        public int initialSize = 10;
        public bool expandable = true; // Si se permite crear más instancias cuando el pool se queda corto
        public Transform parentOverride; // Opcional: contenedor propio para este pool
    }

    [Header("Pools predefinidos (opcional)")]
    [SerializeField] private List<Pool> pools = new List<Pool>();

    [Header("Configuración general")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private static ObjectPooler _instance;
    public static ObjectPooler Instance
    {
        get
        {
            if (_instance == null)
            {
                // Busca una instancia existente en la escena
                _instance = FindFirstObjectByType<ObjectPooler>();

                if (_instance == null)
                {
                    // Crea una automáticamente si no existe (para poder usarlo como utility sin setup)
                    GameObject go = new GameObject("ObjectPooler");
                    _instance = go.AddComponent<ObjectPooler>();
                }
            }
            return _instance;
        }
    }

    // pool key -> cola de instancias inactivas
    private readonly Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();
    // pool key -> configuración del pool
    private readonly Dictionary<string, Pool> poolConfig = new Dictionary<string, Pool>();
    // pool key -> contenedor (Transform) donde se guardan las instancias
    private readonly Dictionary<string, Transform> poolContainers = new Dictionary<string, Transform>();
    // instancia -> key del pool al que pertenece (para poder hacer Despawn sin especificar el pool)
    private readonly Dictionary<GameObject, string> instanceToKey = new Dictionary<GameObject, string>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        foreach (var pool in pools)
        {
            RegisterPool(pool);
        }
    }

    // ---------------------------------------------------------------------
    // Registro de pools
    // ---------------------------------------------------------------------

    /// <summary>
    /// Crea (o reconfigura) un pool para un prefab dado. Si no se pasa id, se usa el nombre del prefab.
    /// </summary>
    public static string CreatePool(GameObject prefab, int initialSize = 10, bool expandable = true, string id = null, Transform parentOverride = null)
    {
        var pool = new Pool
        {
            id = string.IsNullOrEmpty(id) ? prefab.name : id,
            prefab = prefab,
            initialSize = initialSize,
            expandable = expandable,
            parentOverride = parentOverride
        };

        Instance.RegisterPool(pool);
        return pool.id;
    }

    private void RegisterPool(Pool pool)
    {
        if (pool.prefab == null)
        {
            Debug.LogWarning($"[ObjectPooler] Pool '{pool.id}' no tiene prefab asignado. Se ignora.");
            return;
        }

        string key = string.IsNullOrEmpty(pool.id) ? pool.prefab.name : pool.id;
        pool.id = key;

        if (poolDict.ContainsKey(key))
        {
            // Ya existe, no lo recreamos, solo actualizamos config por si cambió
            poolConfig[key] = pool;
            return;
        }

        Transform container = pool.parentOverride;
        if (container == null)
        {
            GameObject containerGO = new GameObject($"Pool_{key}");
            containerGO.transform.SetParent(transform);
            container = containerGO.transform;
        }

        poolContainers[key] = container;
        poolConfig[key] = pool;
        poolDict[key] = new Queue<GameObject>();

        for (int i = 0; i < pool.initialSize; i++)
        {
            GameObject obj = CreateInstance(pool, container);
            poolDict[key].Enqueue(obj);
        }
    }

    private GameObject CreateInstance(Pool pool, Transform container)
    {
        GameObject obj = Instantiate(pool.prefab, container);
        obj.SetActive(false);
        instanceToKey[obj] = pool.id;

        // Si el prefab implementa IPooledObject, le avisamos que fue creado.
        var pooled = obj.GetComponent<IPooledObject>();
        pooled?.OnCreated();

        return obj;
    }

    // ---------------------------------------------------------------------
    // Spawn
    // ---------------------------------------------------------------------

    /// <summary>
    /// Obtiene (o crea) un objeto del pool asociado al prefab y lo activa en la posición/rotación dadas.
    /// Si el pool no existe todavía, se crea automáticamente con tamaño inicial 1.
    /// </summary>
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return Instance.SpawnInternal(prefab.name, prefab, position, rotation, parent);
    }

    /// <summary>
    /// Spawn usando un id de pool ya registrado (por ejemplo, con CreatePool o desde el inspector).
    /// </summary>
    public static GameObject Spawn(string poolId, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return Instance.SpawnInternal(poolId, null, position, rotation, parent);
    }

    private GameObject SpawnInternal(string key, GameObject prefabFallback, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (!poolDict.ContainsKey(key))
        {
            if (prefabFallback == null)
            {
                Debug.LogError($"[ObjectPooler] No existe un pool con id '{key}' y no se proporcionó prefab de referencia.");
                return null;
            }

            // Auto-registro de pool sobre la marcha (utility-friendly: cero setup requerido)
            RegisterPool(new Pool { id = key, prefab = prefabFallback, initialSize = 1, expandable = true });
        }

        Queue<GameObject> queue = poolDict[key];
        Pool config = poolConfig[key];
        GameObject obj;

        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
        }
        else if (config.expandable)
        {
            obj = CreateInstance(config, poolContainers[key]);
        }
        else
        {
            Debug.LogWarning($"[ObjectPooler] Pool '{key}' agotado y no es expandible.");
            return null;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.transform.SetParent(parent != null ? parent : poolContainers[key]);
        obj.SetActive(true);

        var pooled = obj.GetComponent<IPooledObject>();
        pooled?.OnSpawn();

        return obj;
    }

    // ---------------------------------------------------------------------
    // Despawn
    // ---------------------------------------------------------------------

    /// <summary>
    /// Desactiva el objeto y lo devuelve a su pool de origen.
    /// </summary>
    public static void Despawn(GameObject obj)
    {
        Instance.DespawnInternal(obj);
    }

    /// <summary>
    /// Despawn con delay (útil para efectos, proyectiles, etc.).
    /// </summary>
    public static void Despawn(GameObject obj, float delay)
    {
        Instance.StartCoroutine(Instance.DespawnAfterDelay(obj, delay));
    }

    private System.Collections.IEnumerator DespawnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        DespawnInternal(obj);
    }

    private void DespawnInternal(GameObject obj)
    {
        if (obj == null) return;

        if (!instanceToKey.TryGetValue(obj, out string key))
        {
            // No fue creado por este pooler; simplemente lo desactivamos.
            Debug.LogWarning($"[ObjectPooler] El objeto '{obj.name}' no pertenece a ningún pool. Se desactiva sin poolear.");
            obj.SetActive(false);
            return;
        }

        var pooled = obj.GetComponent<IPooledObject>();
        pooled?.OnDespawn();

        obj.SetActive(false);
        obj.transform.SetParent(poolContainers[key]);
        poolDict[key].Enqueue(obj);
    }

    // ---------------------------------------------------------------------
    // Utilidades extra
    // ---------------------------------------------------------------------

    /// <summary>Cantidad de instancias inactivas disponibles en un pool.</summary>
    public static int AvailableCount(string poolId)
    {
        return Instance.poolDict.TryGetValue(poolId, out var q) ? q.Count : 0;
    }

    /// <summary>Elimina físicamente todas las instancias de un pool (por ejemplo, al cambiar de escena).</summary>
    public static void ClearPool(string poolId)
    {
        var self = Instance;
        if (!self.poolDict.TryGetValue(poolId, out var queue)) return;

        while (queue.Count > 0)
        {
            GameObject obj = queue.Dequeue();
            self.instanceToKey.Remove(obj);
            if (obj != null) Destroy(obj);
        }
    }
}

/// <summary>
/// Interfaz opcional para que los prefabs reaccionen a eventos del ciclo de vida del pool.
/// No es obligatorio implementarla.
/// </summary>
public interface IPooledObject
{
    void OnCreated();   // Llamado una vez, cuando la instancia se crea por primera vez
    void OnSpawn();     // Llamado cada vez que el objeto se activa desde el pool
    void OnDespawn();   // Llamado cada vez que el objeto se devuelve al pool
}
