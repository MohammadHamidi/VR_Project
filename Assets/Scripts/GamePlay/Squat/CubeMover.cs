using UnityEngine;
using CombatSystem.Pooling;
using CombatSystem.Events;

namespace CombatSystem.Obstacles
{
    public class CubeMover : MonoBehaviour, IPoolable
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 2f;
        [SerializeField] private float destroyZ = -5f;
        
        [Header("Dodge Detection")]
        [SerializeField] private float dodgeZoneStart = 3f;
        [SerializeField] private float dodgeZoneEnd = 1f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem destroyEffect;
        [SerializeField] private AudioClip destroySound;

        private ObjectPool<CubeMover> _pool;
        private bool _isInDodgeZone = false;
        private bool _wasDestroyed = false;
        private Transform _cachedTransform;
        private AudioSource _audioSource;

        public bool IsInDodgeZone => _isInDodgeZone;
        public bool WasDestroyed => _wasDestroyed;

        void Awake()
        {
            _cachedTransform = transform;
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
            
            ConfigureAudioSource();
        }

        void Update()
        {
            MoveCube();
            CheckDodgeZone();
            CheckDestroy();
        }

        private void ConfigureAudioSource()
        {
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f; // 3D sound
            _audioSource.volume = 0.5f;
        }

        private void MoveCube()
        {
            _cachedTransform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);
        }

        private void CheckDodgeZone()
        {
            float currentZ = _cachedTransform.position.z;
            bool wasInZone = _isInDodgeZone;
            _isInDodgeZone = currentZ <= dodgeZoneStart && currentZ >= dodgeZoneEnd;

            if (_isInDodgeZone && !wasInZone)
            {
                CombatEvents.OnCubeEnterDodgeZone?.Invoke(this);
            }
            else if (!_isInDodgeZone && wasInZone)
            {
                CombatEvents.OnCubeExitDodgeZone?.Invoke(this);
            }
        }

        private void CheckDestroy()
        {
            if (_cachedTransform.position.z <= destroyZ)
            {
                ReturnToPool();
            }
        }

        public void DestroyAndSpawnCoin()
        {
            if (_wasDestroyed) return;
            
            _wasDestroyed = true;
            PlayDestroyEffects();
            CombatEvents.OnCubeDestroyed?.Invoke(_cachedTransform.position);
            ReturnToPool();
        }

        private void PlayDestroyEffects()
        {
            if (destroyEffect != null)
                destroyEffect.Play();
            
            if (_audioSource != null && destroySound != null)
                _audioSource.PlayOneShot(destroySound);
        }

        private void ReturnToPool()
        {
            _pool?.Return(this);
        }

        #region IPoolable Implementation
        public void SetPool<T>(ObjectPool<T> pool) where T : MonoBehaviour, IPoolable
        {
            _pool = pool as ObjectPool<CubeMover>;
        }

        public void OnSpawned()
        {
            _isInDodgeZone = false;
            _wasDestroyed = false;
        }

        public void OnDespawned()
        {
            _isInDodgeZone = false;
            _wasDestroyed = false;
        }
        #endregion
    }
}