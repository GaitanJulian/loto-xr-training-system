using UnityEngine;
using Project.Procedure.Conditions;

namespace Project.Procedure
{
    [CreateAssetMenu(menuName = "XR Training/Step", fileName = "Step")]
    public sealed class StepDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string stepId;

        [Header("UX")]
        [TextArea] public string instruction;

        [Header("Completion")]
        public ConditionSO completionCondition;
    }
}
