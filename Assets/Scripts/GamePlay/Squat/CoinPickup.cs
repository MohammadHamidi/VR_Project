using UnityEngine;
using CombatSystem.Pooling;
using CombatSystem.Events;

namespace CombatSystem.Collectibles
{
    [RequireComponent(typeof(SphereCollider))]
    public class CoinPickup : MonoBehaviour, IPoolable
    {
        [Header("Coin Settings")]
        [SerializeField] private int value = 1;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float lifetime = 10f;

        [Header("VFX Settings")]
        [SerializeField] private ParticleSystem collectEffect;
        [SerializeField] private AudioClip collectSound;

        private ObjectPool<CoinPickup> _pool;
        private Vector3 _startPosition;
        private float _spawnTime;
        private Transform _cachedTransform;
        private SphereCollider _collider;
        private AudioSource _audioSource;

        void Awake()
        {
            _cachedTransform = transform;
            _collider = GetComponent<SphereCollider>();
            _audioSource = GetComponent<AudioSource>();
            
            _collider.isTrigger = true;
            
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            
            ConfigureAudioSource();
        }

        void Update()
        {
            AnimateCoin();
            CheckLifetime();
        }

        private void ConfigureAudioSource()
        {
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D sound
            _audioSource.volume = 0.7f;
        }

        private void AnimateCoin()
        {
            _cachedTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            
            float bobOffset = Mathf.Sin((Time.time - _spawnTime) * bobSpeed) * bobHeight;
            Vector3 newPosition = _startPosition;
            newPosition.y += bobOffset;
            _cachedTransform.position = newPosition;
        }

        private void CheckLifetime()
        {
            if (Time.time - _spawnTime >= lifetime)
            {
                ReturnToPool();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("MainCamera"))
            {
                CollectCoin();
            }
        }

        private void CollectCoin()
        {
            _collider.enabled = false;
            CombatEvents.OnCoinCollected?.Invoke(value);
            PlayCollectEffects();
            ReturnToPool();
        }

        private void PlayCollectEffects()
        {
            if (collectEffect != null)
                collectEffect.Play();
            
            if (_audioSource != null && collectSound != null)
                _audioSource.PlayOneShot(collectSound);
        }

        private void ReturnToPool()
        {
            _pool?.Return(this);
        }

        #region IPoolable Implementation
        public void SetPool<T>(ObjectPool<T> pool) where T : MonoBehaviour, IPoolable
        {
            _pool = pool as ObjectPool<CoinPickup>;
        }

        public void OnSpawned()
        {
            _startPosition = _cachedTransform.position;
            _spawnTime = Time.time;
            _collider.enabled = true;
        }

        public void OnDespawned()
        {
            _collider.enabled = false;
        }
        #endregion
    }
}