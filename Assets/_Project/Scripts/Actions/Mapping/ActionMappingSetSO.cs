using UnityEngine;

namespace Project.Actions.Mapping
{
    [CreateAssetMenu(menuName = "XR Training/Action Mapping Set", fileName = "ActionMappingSet")]
    public sealed class ActionMappingSetSO : ScriptableObject
    {
        public ActionMappingSO[] mappings;
    }
}
