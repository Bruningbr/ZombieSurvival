using UnityEngine;

namespace ZombieSurvival.Utils
{
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 20;
        [SerializeField] private bool autoExpand = true;

        private GameObject[] pool;
        private int currentIndex;

        private void Awake()
        {
            pool = new GameObject[initialSize];
            for (int i = 0; i < initialSize; i++)
            {
                pool[i] = Instantiate(prefab, transform);
                pool[i].SetActive(false);
            }
        }

        public GameObject Get()
        {
            for (int i = 0; i < pool.Length; i++)
            {
                int index = (currentIndex + i) % pool.Length;
                if (!pool[index].activeInHierarchy)
                {
                    currentIndex = (index + 1) % pool.Length;
                    pool[index].SetActive(true);
                    return pool[index];
                }
            }

            if (autoExpand)
            {
                var newObj = Instantiate(prefab, transform);
                var newPool = new GameObject[pool.Length + 1];
                pool.CopyTo(newPool, 0);
                newPool[pool.Length] = newObj;
                pool = newPool;
                return newObj;
            }

            return null;
        }

        public void Return(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);
        }
    }

    public static class VectorExtensions
    {
        public static Vector3 Flat(this Vector3 v)
        {
            return new Vector3(v.x, 0, v.z);
        }

        public static float FlatDistance(this Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a.Flat(), b.Flat());
        }
    }

    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<T>();
                    if (instance == null)
                    {
                        var obj = new GameObject(typeof(T).Name);
                        instance = obj.AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance == null)
            {
                instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
