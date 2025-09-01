using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem.Pooling
{
    public class ObjectPool<T> where T : MonoBehaviour, IPoolable
    {
        private readonly Queue<T> _pool = new Queue<T>();
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _initialSize;

        public ObjectPool(T prefab, Transform parent = null, int initialSize = 10)
        {
            _prefab = prefab;
            _parent = parent;
            _initialSize = initialSize;
            
            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                var obj = Object.Instantiate(_prefab, _parent);
                obj.gameObject.SetActive(false);
                obj.SetPool(this);
                _pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;
            
            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = Object.Instantiate(_prefab, _parent);
                obj.SetPool(this);
            }

            obj.gameObject.SetActive(true);
            obj.OnSpawned();
            return obj;
        }

        public void Return(T obj)
        {
            obj.OnDespawned();
            obj.gameObject.SetActive(false);
            _pool.Enqueue(obj);
        }
    }

    public interface IPoolable
    {
        void SetPool<T>(ObjectPool<T> pool) where T : MonoBehaviour, IPoolable;
        void OnSpawned();
        void OnDespawned();
    }
}