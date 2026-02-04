using UnityEngine;

namespace Project.Actions
{
    public sealed class TargetIdentity : MonoBehaviour
    {
        [SerializeField] private TargetId id = TargetId.None;
        public TargetId Id => id;
    }
}
