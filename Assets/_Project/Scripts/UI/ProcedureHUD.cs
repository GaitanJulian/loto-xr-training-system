using TMPro;
using UnityEngine;
using Project.Procedure;

namespace Project.UI
{
    public sealed class ProcedureHUD : MonoBehaviour
    {
        [SerializeField] private ProcedureRunner runner;
        [SerializeField] private TMP_Text stepText;

        private void OnEnable()
        {
            if (runner != null)
                runner.OnStepChanged += HandleStepChanged;

            // Inicializa texto si ya hay step
            HandleStepChanged(runner != null ? runner.CurrentStep : null);
        }

        private void OnDisable()
        {
            if (runner != null)
                runner.OnStepChanged -= HandleStepChanged;
        }

        private void HandleStepChanged(StepDefinitionSO step)
        {
            if (stepText == null) return;

            if (step == null)
            {
                stepText.text = "No procedure / no step";
                return;
            }

            stepText.text = $"{step.stepId}\n{step.instruction}";
        }
    }
}
