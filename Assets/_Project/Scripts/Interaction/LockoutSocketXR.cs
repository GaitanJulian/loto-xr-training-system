using UnityEngine;
using Project.Actions;

namespace Project.XR
{
    [RequireComponent(typeof(Collider))]
    public sealed class LockoutSocketXR : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private TargetIdentity targetIdentity;

        [Header("Mount Poses")]
        [SerializeField] private Transform mountOn;
        [SerializeField] private Transform mountOff;

        [Header("Detection")]
        [SerializeField] private float snapRadius = 0.10f;

        public TargetIdentity Identity => targetIdentity;
        public float SnapRadius => snapRadius;

        [Header("Breaker Lever")]
        [SerializeField] private BreakerLeverControllerXR _lever;

        private void Awake()
        {
            if (targetIdentity == null)
                Debug.LogError($"{name}: LockoutSocketXR requires TargetIdentity", this);
        }

        public Transform GetActiveMount()
        {
            if (_lever == null) return mountOn;

            float t = _lever.GetProgress01();
            bool isOff = t >= 0.5f;

            return isOff ? mountOff : mountOn;
        }

        private void Reset()
        {
            var c = GetComponent<Collider>();
            c.isTrigger = true;
        }
    }
}
