using UnityEngine;
using Project.Core;

namespace Project.Procedure.Conditions
{
    public enum LogicalOp
    {
        And,
        Or
    }

    [CreateAssetMenu(menuName = "XR Training/Conditions/Composite", fileName = "Cond_Composite")]
    public sealed class CompositeConditionSO : ConditionSO
    {
        public LogicalOp op = LogicalOp.And;

        [Tooltip("Sub-condiciones a evaluar.")]
        public ConditionSO[] conditions;

        public override bool Evaluate(WorldState state)
        {
            if (conditions == null || conditions.Length == 0)
                return false;

            if (op == LogicalOp.And)
            {
                foreach (var c in conditions)
                {
                    if (c == null) continue;
                    if (!c.Evaluate(state)) return false;
                }
                return true;
            }

            // OR
            foreach (var c in conditions)
            {
                if (c == null) continue;
                if (c.Evaluate(state)) return true;
            }
            return false;
        }
    }
}
