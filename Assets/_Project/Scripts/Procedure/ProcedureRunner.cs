using Project.Core;
using UnityEngine;
using Project.Procedure.Conditions;
using System;
using Project.Actions;
using Project.Actions.Mapping;

namespace Project.Procedure
{
    public sealed class ProcedureRunner : MonoBehaviour
    {
        [Header("Procedure")]
        [SerializeField] private ProcedureDefinitionSO procedure;

        private WorldState _state;
        private int _currentIndex;

        public event Action<StepDefinitionSO> OnStepChanged;
        public event Action<ActionEvent> OnActionPublished;

        [Header("Actions")]
        [SerializeField] private ActionMappingSetSO actionMappings;

        private ActionBus _actionBus;

        public WorldState State => _state;

        public StepDefinitionSO CurrentStep
        {
            get
            {
                if (procedure == null || procedure.steps == null || procedure.steps.Length == 0)
                    return null;

                if (_currentIndex < 0 || _currentIndex >= procedure.steps.Length)
                    return null;

                return procedure.steps[_currentIndex];
            }
        }

        private void Awake()
        {
            _state = new WorldState();
            _currentIndex = 0;
            _actionBus = new ActionBus();
            _actionBus.OnAction += HandleAction;
        }

        private void OnDisable()
        {
            _actionBus.OnAction -= HandleAction;
        }

        public void StartProcedure()
        {
            _currentIndex = 0;
            Debug.Log($"Procedure started: {procedure?.displayName}");
            LogCurrentStep();
            OnStepChanged?.Invoke(CurrentStep);
        }

        // Lo llamaremos manualmente por ahora (con un botón) para validar el motor.
        public void EvaluateAndAdvance()
        {
            var step = CurrentStep;
            if (step == null)
            {
                Debug.LogWarning("No current step (procedure not set or empty).");
                return;
            }

            if (step.completionCondition == null)
            {
                Debug.LogWarning($"Step '{step.stepId}' has no completion condition.");
                return;
            }

            bool completed = step.completionCondition.Evaluate(_state);
            if (!completed)
            {
                Debug.Log($"Step not completed yet: {step.stepId}");
                return;
            }

            AdvanceStep();
        }

        private void AdvanceStep()
        {
            if (procedure == null || procedure.steps == null)
                return;

            int next = _currentIndex + 1;
            if (next >= procedure.steps.Length)
            {
                Debug.Log("Procedure completed!");
                _currentIndex = procedure.steps.Length - 1;
                return;
            }

            _currentIndex = next;
            Debug.Log($"Advanced to step: {CurrentStep.stepId}");
            LogCurrentStep();
            OnStepChanged?.Invoke(CurrentStep);
        }

        private void LogCurrentStep()
        {
            var step = CurrentStep;
            if (step == null) return;

            Debug.Log($"Current step: {step.stepId} | {step.instruction}");
        }

        public void PublishAction(ActionEvent evt)
        {
            OnActionPublished?.Invoke(evt);   // Notifica a controladores externos (MachineStateController, UI, etc.)
            _actionBus.Publish(evt);
        }

        private void HandleAction(ActionEvent evt)
        {
            Debug.Log(evt.ToString());

            if (actionMappings == null || actionMappings.mappings == null)
                return;

            foreach (var mapping in actionMappings.mappings)
            {
                if (mapping == null) continue;

                if (mapping.Matches(evt))
                {
                    mapping.Apply(_state);
                    Debug.Log($"Applied mapping: {mapping.actionType} -> {mapping.boolKey}");
                }
            }

            EvaluateAndAdvance();
        }

    }
}
