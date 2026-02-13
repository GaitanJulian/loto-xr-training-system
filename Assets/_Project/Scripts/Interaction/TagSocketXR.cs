using UnityEngine;
using Project.Actions;

namespace Project.XR
{
    public sealed class TagSocketXR : MonoBehaviour
    {
        [Header("Identity (required)")]
        [SerializeField] private TargetIdentity targetIdentity;

        [Header("Snap Pose")]
        [Tooltip("Pose exacta donde quedará la etiqueta. Si es null, se usa este Transform.")]
        [SerializeField] private Transform snapPose;

        [Header("Acceptance")]
        [SerializeField] private float snapRadius = 0.08f;

        public TargetIdentity Identity => targetIdentity;
        public Transform SnapPose => snapPose != null ? snapPose : transform;
        public float SnapRadius => snapRadius;

        private void Awake()
        {
            if (targetIdentity == null)
                Debug.LogError($"{name}: TagSocketXR requires TargetIdentity.", this);
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
