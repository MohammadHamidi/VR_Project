// SpawnPointVisualController.cs
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using CombatSystem.Events;

namespace CombatSystem.Portals
{
    /// <summary>
    /// Controls the visual open/close of a portal that consists of:
    /// - One main Quad
    /// - Zero or more sphere objects around it
    /// 
    /// Opening:
    ///   - Enables all renderers (without disabling GameObjects)
    ///   - Scales all assigned visuals from closedScale -> openScale
    /// Closing:
    ///   - Scales all assigned visuals from openScale -> closedScale
    ///   - Optionally disables all renderers when fully closed
    /// 
    /// Also maps keys:
    ///   O = play opening
    ///   C = play closing
    /// </summary>
    public class SpawnPointVisualController : MonoBehaviour
    {
        [Header("Assignments")]
        [Tooltip("Main portal Quad (visual root of the portal).")]
        [SerializeField] private Transform mainQuad;

        [Tooltip("Optional extra visuals (e.g., spheres distributed around the Quad).")]
        [SerializeField] private List<Transform> spheres = new List<Transform>();

        [Tooltip("(Optional) Legacy single target fallback. Used only if Main Quad is not set.")]
        [SerializeField] public Transform target;

        private readonly List<Transform> _targets = new List<Transform>();
        private Renderer[] _renderers;

        [Header("Scales")]
        [SerializeField] public float closedScale = 0f;
        [SerializeField] public float openScale = 1f;

        [Header("Timings")]
        [SerializeField] private float openingDuration = 0.35f;
        [SerializeField] private float closingDuration = 0.30f;

        [Header("Behavior")]
        [Tooltip("If true, turn renderers off (enabled=false) when fully closed.")]
        [SerializeField] public bool deactivateOnClosed = true;

        [Tooltip("If true, start visually closed (scale=closedScale, renderers off if deactivateOnClosed).")]
        [SerializeField] public bool startClosed = true;

        [Header("Easing")]
        [SerializeField] private Ease openingEase = Ease.OutBack;
        [SerializeField] private Ease closingEase = Ease.InQuad;

        [Header("Test Input")]
        [SerializeField] private bool enableInputTesting = true;
        [SerializeField] private KeyCode openKey = KeyCode.O;
        [SerializeField] private KeyCode closeKey = KeyCode.C;

        private Sequence _tween;

        #region Unity lifecycle
        private void Awake()
        {
            BuildCaches();

            // Initial visual state
            SetScaleImmediate(startClosed ? closedScale : openScale);
            if (deactivateOnClosed && startClosed)
                SetRenderersEnabled(false);
        }

        private void OnEnable()
        {
            CombatEvents.OnPortalOpening += HandlePortalOpening;
            CombatEvents.OnPortalOpened  += HandlePortalOpened;
            CombatEvents.OnPortalClosing += HandlePortalClosing;
            CombatEvents.OnPortalClosed  += HandlePortalClosed;
        }

        private void OnDisable()
        {
            CombatEvents.OnPortalOpening -= HandlePortalOpening;
            CombatEvents.OnPortalOpened  -= HandlePortalOpened;
            CombatEvents.OnPortalClosing -= HandlePortalClosing;
            CombatEvents.OnPortalClosed  -= HandlePortalClosed;
            KillTween();
        }

        private void Update()
        {
            if (!enableInputTesting) return;

            if (Input.GetKeyDown(openKey))
            {
                // Simulate "opening" (animates to open)
                HandlePortalOpening();
            }
            else if (Input.GetKeyDown(closeKey))
            {
                // Simulate "closing" (animates to closed)
                HandlePortalClosing();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Keep caches fresh in editor when assignments change
            if (!Application.isPlaying)
                BuildCaches();
        }
#endif
        #endregion

        #region Event handlers
        private void HandlePortalOpening()
        {
            SetRenderersEnabled(true);
            AnimateScale(openScale, openingDuration, openingEase, onComplete: HandlePortalOpened);
            Debug.Log($"{name} opening animation started");
        }

        private void HandlePortalOpened()
        {
            SetScaleImmediate(openScale);
            SetRenderersEnabled(true);
            Debug.Log($"{name} fully opened");
        }

        private void HandlePortalClosing()
        {
            AnimateScale(closedScale, closingDuration, closingEase, onComplete: () =>
            {
                HandlePortalClosed();
                Debug.Log($"{name} closing animation completed");
            });
            Debug.Log($"{name} closing animation started");
        }

        private void HandlePortalClosed()
        {
            SetScaleImmediate(closedScale);
            if (deactivateOnClosed) SetRenderersEnabled(false);
            Debug.Log($"{name} fully closed");
        }
        #endregion

        #region Core visuals
        private void AnimateScale(float to, float duration, Ease ease, System.Action onComplete = null)
        {
            KillTween();

            var seq = DOTween.Sequence();
            foreach (var t in _targets)
            {
                if (!t) continue;
                seq.Join(t.DOScale(to, duration).SetEase(ease));
            }

            if (onComplete != null)
                seq.OnComplete(() => onComplete.Invoke());

            _tween = seq;
        }

        private void SetScaleImmediate(float s)
        {
            KillTween();
            var v = Vector3.one * s;
            foreach (var t in _targets)
            {
                if (!t) continue;
                t.localScale = v;
            }
        }

        private void KillTween()
        {
            if (_tween != null && _tween.IsActive()) _tween.Kill();
            _tween = null;
        }

        private void SetRenderersEnabled(bool enabled)
        {
            if (_renderers == null) return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i]) _renderers[i].enabled = enabled;
            }
        }
        #endregion

        #region Setup helpers
        private void BuildCaches()
        {
            _targets.Clear();

            // Priority: explicit Main Quad -> spheres -> legacy single target -> fallback to this.transform
            if (mainQuad) _targets.Add(mainQuad);
            if (spheres != null)
            {
                for (int i = 0; i < spheres.Count; i++)
                {
                    var s = spheres[i];
                    if (s && !_targets.Contains(s)) _targets.Add(s);
                }
            }
            if (!mainQuad && target && !_targets.Contains(target))
                _targets.Add(target);
            if (_targets.Count == 0)
                _targets.Add(transform);

            // Build unique renderer cache from all assigned visuals (self included, in case)
            var set = new HashSet<Renderer>();
            var list = new List<Renderer>();

            // Include renderers on this GameObject (e.g., if script is placed on a visual)
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r && set.Add(r)) list.Add(r);

            // Include renderers on every assigned target (and their children)
            foreach (var t in _targets)
            {
                if (!t) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true))
                    if (r && set.Add(r)) list.Add(r);
            }

            _renderers = list.ToArray();
        }
        #endregion

        #region Manual hooks (callable from other systems or UnityEvents)
        [ContextMenu("Play Opening")]
        public void PlayOpening()  => HandlePortalOpening();

        [ContextMenu("Play Opened (snap)")]
        public void PlayOpened()   => HandlePortalOpened();

        [ContextMenu("Play Closing")]
        public void PlayClosing()  => HandlePortalClosing();

        [ContextMenu("Play Closed (snap)")]
        public void PlayClosed()   => HandlePortalClosed();
        #endregion
    }
}
