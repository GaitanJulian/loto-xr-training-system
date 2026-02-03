using UnityEngine;
using Project.Core;

namespace Project.Procedure.Conditions
{
    // Todas las condiciones heredan de esto.
    public abstract class ConditionSO : ScriptableObject
    {
        public abstract bool Evaluate(WorldState state);
    }
}
