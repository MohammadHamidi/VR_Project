using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using CombatSystem.Events;

namespace CombatSystem.Player
{
    public class SquatDodge : MonoBehaviour
    {
        public static SquatDodge Instance { get; private set; }

        [Header("Squat Detection")]
        [SerializeField] private Transform xrCamera;
        [SerializeField] private float standingHeight = 1.7f;
        [SerializeField] private float squatThreshold = 0.3f;
        [SerializeField] private float dodgeDuration = 0.5f;
        [SerializeField] private float cooldownDuration = 0.2f;

        [Header("Dodge Feedback")]
        [SerializeField] private AudioClip dodgeSound;
        [SerializeField] private ParticleSystem dodgeEffect;

        public bool IsDodging { get; private set; }
        public float CurrentSquatDepth { get; private set; }
        public bool IsOnCooldown { get; private set; }

        private AudioSource _audioSource;
        private Coroutine _dodgeCoroutine;
        private Coroutine _cooldownCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            if (xrCamera == null)
                xrCamera = Camera.main?.transform;
        }

        void Update()
        {
            CheckSquatPosition();
        }

        private void CheckSquatPosition()
        {
            if (xrCamera == null) return;

            float currentHeight = xrCamera.position.y;
            float heightDifference = standingHeight - currentHeight;
            CurrentSquatDepth = Mathf.Max(0f, heightDifference);

            CombatEvents.OnPlayerSquatDepthChanged?.Invoke(CurrentSquatDepth);

            if (CurrentSquatDepth >= squatThreshold && !IsDodging && !IsOnCooldown)
            {
                TriggerDodge();
            }
        }

        private void TriggerDodge()
        {
            if (_dodgeCoroutine != null)
                StopCoroutine(_dodgeCoroutine);
            
            _dodgeCoroutine = StartCoroutine(DodgeRoutine());
        }

        private IEnumerator DodgeRoutine()
        {
            IsDodging = true;
            CombatEvents.OnPlayerDodge?.Invoke();
            PlayDodgeFeedback();

            yield return new WaitForSeconds(dodgeDuration);
            IsDodging = false;

            if (_cooldownCoroutine != null)
                StopCoroutine(_cooldownCoroutine);
            
            _cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            IsOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            IsOnCooldown = false;
        }

        private void PlayDodgeFeedback()
        {
            if (_audioSource != null && dodgeSound != null)
                _audioSource.PlayOneShot(dodgeSound);

            if (dodgeEffect != null)
                dodgeEffect.Play();
        }

        public void CalibrateStandingHeight()
        {
            if (xrCamera != null)
            {
                standingHeight = xrCamera.position.y;
                Debug.Log($"Standing height calibrated to: {standingHeight:F2}m");
            }
        }
    }
}