using UnityEngine;
using Project.Actions;

namespace Project.XR
{
    public sealed class PadlockSocketXR : MonoBehaviour
    {
        [Header("Identity (required)")]
        [SerializeField] private TargetIdentity targetIdentity;

        [Header("Snap Pose")]
        [Tooltip("Pose exacta donde quedará el candado. Si es null, se usa este Transform.")]
        [SerializeField] private Transform snapPose;

        [Header("Acceptance")]
        [SerializeField] private float snapRadius = 0.08f;

        private LockoutSnapXR _lockoutOwner;

        public TargetIdentity Identity => targetIdentity;
        public Transform SnapPose => snapPose != null ? snapPose : transform;
        public float SnapRadius => snapRadius;

        private void Awake()
        {
            _lockoutOwner = GetComponentInParent<LockoutSnapXR>();

            if (targetIdentity == null)
            {
                Debug.LogError($"{name}: PadlockSocketXR requires TargetIdentity.", this);
            }
        }

        public bool CanAcceptPadlock()
        {
            return _lockoutOwner != null
                && _lockoutOwner.IsMounted
                && !_lockoutOwner.IsPadlocked;
        }

        public void NotifyPadlockApplied()
        {
            if (_lockoutOwner != null)
                _lockoutOwner.ApplyPadlock();
        }

        private void Reset()
        {
            snapPose = transform;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(SnapPose.position, snapRadius);
        }
    }
}
