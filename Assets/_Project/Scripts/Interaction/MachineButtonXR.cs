using UnityEngine;
using UnityEngine.Events;
using Project.Actions;
using Project.Procedure;
using Project.XR;

namespace Project.XR
{
    /// <summary>
    /// Botón industrial tipo "pulse":
    /// - Dispara una sola vez cuando el PokeInteractable entra en Selected (fondo / select).
    /// - Cooldown.
    /// - Publica ActionEvent usando ActionType (enum) y TargetId (enum) vía ProcedureRunner.
    /// - Expone UnityEvents para audio/FX (OnPressed / OnRejected).
    /// - Opcional: gating (ej: Start solo si breaker está ON).
    /// </summary>
    public sealed class MachineButtonXR : MonoBehaviour
    {
        [Header("Procedure")]
        [SerializeField] private ProcedureRunner runner;
        [SerializeField] private TargetIdentity targetIdentity;

        [Header("Action")]
        [SerializeField] private ActionType actionType = ActionType.MachineStart;

        [Header("Rules")]
        [SerializeField] private float cooldownSeconds = 0.25f;

        [Tooltip("Si true, evita re-disparos si el wrapper llama Selected dos veces sin Unselected (por jitter).")]
        [SerializeField] private bool ignoreWhileSelecting = true;

        [Header("Gating (optional)")]
        [Tooltip("Ej: BreakerLeverControllerXR para bloquear Start cuando breaker está OFF.")]
        [SerializeField] private BreakerLeverControllerXR breaker;

        [Tooltip("Si true, requiere breaker ON para permitir el ActionType configurado abajo.")]
        [SerializeField] private bool requireBreakerOn = false;

        [Tooltip("Qué acciones quedan bloqueadas si breaker está OFF (solo aplica si requireBreakerOn=true).")]
        [SerializeField] private ActionType[] gatedActionsWhenBreakerOff;

        [Header("Unity Events (Audio / FX)")]
        public UnityEvent OnPressed;
        public UnityEvent OnRejected;

        [Header("Debug")]
        [SerializeField] private bool log;

        private bool _selecting;
        private bool _firedThisSelect;
        private float _nextAllowedTime;

        // Conectar desde wrapper: OnSelected
        public void OnSelected()
        {
            if (ignoreWhileSelecting && _selecting) return;
            _selecting = true;

            if (Time.time < _nextAllowedTime) return;
            if (_firedThisSelect) return;

            if (!CanFire(out string reason))
            {
                if (log) Debug.Log($"[{name}] Rejected: {reason}", this);
                OnRejected?.Invoke();
                _nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
                return;
            }

            _firedThisSelect = true;
            _nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);

            OnPressed?.Invoke();
            PublishPulse();
        }

        // Conectar desde wrapper: OnUnselected
        public void OnUnselected()
        {
            _selecting = false;
            _firedThisSelect = false;
        }

        private bool CanFire(out string reason)
        {
            reason = string.Empty;

            if (runner == null)
            {
                reason = "ProcedureRunner is null.";
                return false;
            }

            if (targetIdentity == null)
            {
                reason = "TargetIdentity is null.";
                return false;
            }

            if (!requireBreakerOn) return true;

            // Si no hay breaker asignado, no bloquees (pero lo ideal es asignarlo)
            if (breaker == null) return true;

            bool breakerOff = breaker.IsOffPhysical();

            if (!breakerOff) return true;

            // breaker está OFF: bloquea solo si esta acción está en la lista
            if (IsActionGated(actionType))
            {
                reason = "Breaker is OFF.";
                return false;
            }

            return true;
        }

        private bool IsActionGated(ActionType a)
        {
            if (gatedActionsWhenBreakerOff == null) return false;
            for (int i = 0; i < gatedActionsWhenBreakerOff.Length; i++)
            {
                if (gatedActionsWhenBreakerOff[i] == a) return true;
            }
            return false;
        }

        private void PublishPulse()
        {
            runner.PublishAction(new ActionEvent(actionType, targetIdentity.Id));

            if (log) Debug.Log($"[{name}] Published {actionType} -> {targetIdentity.Id}", this);
        }
    }
}
