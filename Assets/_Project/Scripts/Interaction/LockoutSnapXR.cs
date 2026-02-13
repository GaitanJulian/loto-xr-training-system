using System.Collections;
using UnityEngine;
using Project.Actions;
using Project.Procedure;

namespace Project.XR
{
    public sealed class LockoutSnapXR : MonoBehaviour
    {
        [Header("Procedure")]
        [SerializeField] private ProcedureRunner runner;

        [Header("Search")]
        [SerializeField] private float searchRadius = 0.20f;
        [SerializeField] private LayerMask socketLayer = ~0;

        [Header("Meta XR")]
        [SerializeField] private Behaviour[] grabbables;
        [SerializeField] private float grabCooldownSec = 0.35f;

        [Header("State")]
        public bool IsMounted { get; private set; }
        public bool IsPadlocked { get; private set; }

        private LockoutSocketXR _currentSocket;

        // OnSelected
        public void OnGrabStarted()
        {
            if (IsMounted && !IsPadlocked)
            {
                // Retirar lockout
                IsMounted = false;
                transform.SetParent(null, true);
            }
        }

        // OnUnselected
        public void TrySnap()
        {
            if (IsMounted) return;

            LockoutSocketXR socket = FindBestSocket();
            if (socket == null) return;
            if (socket.Identity == null) return;

            Transform mount = socket.GetActiveMount();
            if (mount == null) return;

            // Opción B: usar la pose del mount (world) pero parentar al socket (jerarquía limpia)
            SnapUtility.SnapWorld(
                transform,
                mount,
                socket.transform
            );

            IsMounted = true;
            _currentSocket = socket;

            // bloquear breaker desde ya
            var lever = socket.GetComponentInParent<BreakerLeverControllerXR>();
            if (lever != null)
                lever.SetLocked(true);

            // publicar evento procedural
            if (runner != null)
            {
                runner.PublishAction(new ActionEvent(
                    ActionType.AttachLockoutDevice,
                    socket.Identity.Id
                ));
            }

            // cooldown
            if (grabCooldownSec > 0f)
                StartCoroutine(GrabCooldown(grabCooldownSec));
        }

        // llamado por PadlockSnapXR
        public void ApplyPadlock()
        {
            if (!IsMounted || IsPadlocked) return;

            IsPadlocked = true;
            SetGrabEnabled(false);
        }

        private LockoutSocketXR FindBestSocket()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                searchRadius,
                socketLayer,
                QueryTriggerInteraction.Collide
            );

            LockoutSocketXR best = null;
            float bestDist = float.MaxValue;

            foreach (var h in hits)
            {
                var socket = h.GetComponentInParent<LockoutSocketXR>();
                if (socket == null) continue;

                Transform m = socket.GetActiveMount();
                if (m == null) continue;

                float d = Vector3.Distance(transform.position, m.position);
                if (d > socket.SnapRadius) continue;

                if (d < bestDist)
                {
                    bestDist = d;
                    best = socket;
                }
            }

            return best;
        }

        private IEnumerator GrabCooldown(float sec)
        {
            SetGrabEnabled(false);
            yield return new WaitForSeconds(sec);
            if (!IsPadlocked)
                SetGrabEnabled(true);
        }

        private void SetGrabEnabled(bool enabled)
        {
            if (grabbables == null) return;
            foreach (var g in grabbables)
                if (g != null) g.enabled = enabled;
        }

        private void RestoreWorldScale(Vector3 worldScale)
        {
            Transform p = transform.parent;
            if (p == null)
            {
                transform.localScale = worldScale;
                return;
            }

            Vector3 ps = p.lossyScale;
            transform.localScale = new Vector3(
                ps.x != 0 ? worldScale.x / ps.x : worldScale.x,
                ps.y != 0 ? worldScale.y / ps.y : worldScale.y,
                ps.z != 0 ? worldScale.z / ps.z : worldScale.z
            );
        }
    }
}
