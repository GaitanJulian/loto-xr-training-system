using UnityEngine;
using Project.Actions;

namespace Project.XR
{
    public sealed class SnapSocketXR : MonoBehaviour
    {
        public enum SocketType
        {
            Lockout,
            Padlock,
            Tag
        }

        [Header("Socket")]
        [SerializeField] private SocketType socketType = SocketType.Lockout;

        [Tooltip("TargetIdentity que identifica ESTE punto. Ej: LockoutSocket_01, PadlockSocket_01, TagSocket_01.")]
        [SerializeField] private TargetIdentity targetIdentity;

        [Tooltip("Pose exacta donde debe quedar el objeto. Si es null, usa este transform.")]
        [SerializeField] private Transform snapPose;

        [SerializeField] private float snapRadius = 0.08f;

        public SocketType Type => socketType;
        public TargetIdentity Identity => targetIdentity;
        public Transform SnapPose => snapPose != null ? snapPose : transform;
        public float SnapRadius => snapRadius;

        private void Reset()
        {
            snapPose = transform;
        }

        private void OnDrawGizmosSelected()
        {
            var p = SnapPose.position;
            Gizmos.DrawWireSphere(p, snapRadius);
        }
    }
}
