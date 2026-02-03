using UnityEngine;

namespace Project.Procedure
{
    [CreateAssetMenu(menuName = "XR Training/Procedure", fileName = "Procedure")]
    public sealed class ProcedureDefinitionSO : ScriptableObject
    {
        public string procedureId;
        public string displayName;

        [TextArea] public string description;

        public StepDefinitionSO[] steps;
    }
}
