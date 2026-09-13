using Nocturne.Core;
using UnityEngine;

namespace Nocturne.World
{
    /// <summary>
    /// A проход (door/portal/barrier): unique stable id, point cost, permanent openness (TZ §7).
    /// Hold/spend is driven by PlayerInteractor; this owns data, collision and visuals.
    /// </summary>
    public sealed class Gate : MonoBehaviour
    {
        [Tooltip("Unique stable id, NOT tied to the GameObject name.")]
        public string id = "gate_01";
        [Min(1)] public int cost = 30;
        public bool isOpen;

        [Header("Open shader animation (per-instance material copy)")]
        [Tooltip("Float shader param animated on open. Must exist on the gate material (e.g. _Dissolve on DoorWobble).")]
        public string dissolveProperty = "_Dissolve";
        [Tooltip("Param value when closed (X).")]
        public float closedDissolve;
        [Tooltip("Param value when open (Y).")]
        public float openDissolve = 1f;
        [Tooltip("Seconds to animate the param from X to Y after opening.")]
        [Min(0.05f)] public float openDuration = 0.8f;

        public event System.Action<Gate> Opened;

        private Collider2D blocker;
        private SpriteRenderer visual;
        private int dissolveId;
        private Material materialCopy;
        private Coroutine openRoutine;

        private void Awake()
        {
            blocker = GetComponent<Collider2D>();
            visual = GetComponent<SpriteRenderer>();
            dissolveId = string.IsNullOrEmpty(dissolveProperty) ? 0 : Shader.PropertyToID(dissolveProperty);
            ApplyCollision();
            ApplyColor();
            // Snap to the matching end value on load; only fresh opens animate.
            SnapDissolve(isOpen ? openDissolve : closedDissolve);
        }

        private void OnDestroy()
        {
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }
            // visual.material clones the shared asset; destroy our copy to avoid editor leaks.
            if (materialCopy != null)
            {
                Destroy(materialCopy);
                materialCopy = null;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterGate(this);
        }

        private void OnValidate()
        {
            if (cost < 1) cost = 1;
            if (string.IsNullOrEmpty(id)) id = "gate_01";
            if (openDuration < 0.05f) openDuration = 0.05f;
            if (blocker == null) blocker = GetComponent<Collider2D>();
            ApplyCollision();
        }

        public bool CanOpen(int unspent) => !isOpen && unspent >= cost;

        /// <summary>Atomic spend + open. Returns false if funds/conditions changed mid-hold.</summary>
        public bool TryOpen(RunState run)
        {
            if (run == null || !CanOpen(run.Unspent)) return false;
            if (!run.TrySpend(cost, id)) return false;
            SetOpen(true);
            Opened?.Invoke(this);
            AudioManager.GateOpen();
            return true;
        }

        public void SetOpen(bool open)
        {
            var wasOpen = isOpen;
            isOpen = open;
            ApplyCollision();
            ApplyColor();
            if (open && !wasOpen && Application.isPlaying)
            {
                if (openRoutine != null) StopCoroutine(openRoutine);
                openRoutine = StartCoroutine(PlayOpenAnimation());
            }
            else
            {
                if (openRoutine != null)
                {
                    StopCoroutine(openRoutine);
                    openRoutine = null;
                }
                SnapDissolve(open ? openDissolve : closedDissolve);
            }
        }

        private void ApplyCollision()
        {
            if (blocker != null)
                blocker.enabled = !isOpen;
        }

        private void ApplyColor()
        {
            if (visual == null) return;
            // Placeholder: closed = solid blue, open = faded. Real art replaces colors, not logic.
            visual.color = isOpen ? new Color(0.3f, 1f, 0.3f, 0.25f) : new Color(0.3f, 0.5f, 1f, 1f);
        }

        /// <summary>
        /// Per-instance material copy for this gate's SpriteRenderer.
        /// Returns null when the shared material has no such property (e.g. default
        /// sprite material in tests) so the open still works, just without animation.
        /// Never touches sharedMaterial: the asset stays unchanged for other gates.
        /// </summary>
        private Material DissolveMaterial()
        {
            if (materialCopy != null) return materialCopy;
            if (visual == null) return null;
            if (dissolveId == 0 && !string.IsNullOrEmpty(dissolveProperty))
                dissolveId = Shader.PropertyToID(dissolveProperty);
            if (dissolveId == 0) return null;
            var shared = visual.sharedMaterial;
            if (shared != null && !shared.HasProperty(dissolveId)) return null;
            // Accessing .material clones the shared asset for this renderer only.
            materialCopy = visual.material;
            if (materialCopy == null || !materialCopy.HasProperty(dissolveId)) return null;
            return materialCopy;
        }

        private void SnapDissolve(float value)
        {
            var mat = DissolveMaterial();
            if (mat != null) mat.SetFloat(dissolveId, value);
        }

        private System.Collections.IEnumerator PlayOpenAnimation()
        {
            var mat = DissolveMaterial();
            if (mat == null)
            {
                openRoutine = null;
                yield break;
            }
            var duration = Mathf.Max(0.05f, openDuration);
            mat.SetFloat(dissolveId, closedDissolve);
            var t = 0f;
            // Unscaled so the animation always finishes even if timeScale changes.
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                mat.SetFloat(dissolveId,
                    Mathf.Lerp(closedDissolve, openDissolve, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            mat.SetFloat(dissolveId, openDissolve);
            openRoutine = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = isOpen ? Color.gray : Color.red;
            Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>() is BoxCollider2D box
                ? (Vector3)box.size * 2
                : new Vector3(1f, 3f, 0f));
        }
    }
}
