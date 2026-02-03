using Project.Procedure;
using UnityEngine;

namespace Project.Procedure.Debugging
{
    public sealed class ProcedureDebugDriver : MonoBehaviour
    {
        [SerializeField] private ProcedureRunner runner;

        // Keys de ejemplo para el WorldState
        [SerializeField] private string flagKey = "breakerOff";

        private void Start()
        {
            if (runner != null)
                runner.StartProcedure();
        }

        public void SetFlagTrue()
        {
            if (runner == null) return;
            runner.State.SetBool(flagKey, true);
            Debug.Log($"Flag '{flagKey}' set TRUE");
        }

        public void SetFlagFalse()
        {
            if (runner == null) return;
            runner.State.SetBool(flagKey, false);
            Debug.Log($"Flag '{flagKey}' set FALSE");
        }

        public void EvaluateStep()
        {
            if (runner == null) return;
            runner.EvaluateAndAdvance();
        }
    }
}
