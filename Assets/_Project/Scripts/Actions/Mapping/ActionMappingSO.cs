using UnityEngine;
using Project.Core;

namespace Project.Actions.Mapping
{
    [CreateAssetMenu(menuName = "XR Training/Action Mapping", fileName = "ActionMapping")]
    public sealed class ActionMappingSO : ScriptableObject
    {
        public ActionType actionType;

        [Tooltip("Si es None, aplica a cualquier target.")]
        public TargetId target = TargetId.None;

        [Header("WorldState effect")]
        public string boolKey;
        public bool boolValue = true;

        public void Apply(WorldState state)
        {
            if (!string.IsNullOrEmpty(boolKey))
                state.SetBool(boolKey, boolValue);
        }

        public bool Matches(ActionEvent evt)
        {
            if (evt.Type != actionType) return false;
            if (target != TargetId.None && evt.Target != target) return false;
            return true;
        }
    }
}
