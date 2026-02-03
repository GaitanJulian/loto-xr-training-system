using UnityEngine;
using Project.Actions;
using Project.Core;

namespace Project.Actions.Mapping
{
    [CreateAssetMenu(menuName = "XR Training/Action Mapping", fileName = "ActionMapping")]
    public sealed class ActionMappingSO : ScriptableObject
    {
        public ActionType actionType;
        public string targetId;

        [Header("WorldState effect")]
        public string boolKey;
        public bool boolValue = true;

        public void Apply(WorldState state)
        {
            if (!string.IsNullOrEmpty(boolKey))
            {
                state.SetBool(boolKey, boolValue);
            }
        }

        public bool Matches(ActionEvent evt)
        {
            if (evt.Type != actionType) return false;
            if (!string.IsNullOrEmpty(targetId) && evt.TargetId != targetId) return false;
            return true;
        }
    }
}
