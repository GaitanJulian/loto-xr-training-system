using UnityEngine;
using Project.Core;

namespace Project.Procedure.Conditions
{
    [CreateAssetMenu(menuName = "XR Training/Conditions/Bool Flag", fileName = "Cond_BoolFlag")]
    public sealed class BoolFlagConditionSO : ConditionSO
    {
        [Tooltip("Nombre de la bandera en WorldState, ej: 'breakerOff'")]
        public string key;

        [Tooltip("Valor esperado para considerar la condición como cumplida.")]
        public bool expectedValue = true;

        public override bool Evaluate(WorldState state)
        {
            return state.GetBool(key) == expectedValue;
        }
    }
}
