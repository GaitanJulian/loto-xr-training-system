using UnityEngine;
using UnityEngine.Events;
using Project.Actions;
using Project.Procedure;

namespace Project.XR
{
    public interface IPanelDoorLock
    {
        void SetDoorInteractionEnabled(bool enabled);
    }

    public interface ILotoLockState
    {
        bool IsLockoutDeviceApplied { get; }
        bool IsPadlockApplied { get; }
        bool IsTagAttached { get; }
    }

    public sealed class MachineStateController : MonoBehaviour
    {
        [Header("Procedure")]
        [SerializeField] private ProcedureRunner runner;
        [SerializeField] private TargetIdentity machineIdentity;

        [Header("Power")]
        [SerializeField] private BreakerLeverControllerXR breaker;

        [Header("LOTO state (optional)")]
        [SerializeField] private MonoBehaviour lotoLockStateSource; // ILotoLockState

        [Header("Panel door (optional)")]
        [SerializeField] private MonoBehaviour panelDoorLockSource; // IPanelDoorLock

        [Header("Feedback")]
        public UnityEvent OnMachineStarted;
        public UnityEvent OnMachineStopped;
        public UnityEvent OnStartDenied; // “no enciende” / buzzer suave

        [Header("Verify behavior")]
        [Tooltip("Si true, cuando TryStart falla por aislamiento, publica VerifyIsolation automáticamente.")]
        [SerializeField] private bool autoPublishVerifyIsolation = true;

        [Header("Debug")]
        [SerializeField] private bool log;

        public bool IsMachineOn => _isMachineOn;
        private bool _isMachineOn;

        private ILotoLockState Loto => lotoLockStateSource as ILotoLockState;
        private IPanelDoorLock Door => panelDoorLockSource as IPanelDoorLock;

        private void OnEnable()
        {
            if (runner != null) runner.OnActionPublished += OnAction;
        }

        private void OnDisable()
        {
            if (runner != null) runner.OnActionPublished -= OnAction;
        }

        private void Awake()
        {
            _isMachineOn = false;
            UpdateDoorAvailability();
        }

        private void OnAction(ActionEvent evt)
        {
            if (machineIdentity == null) return;
            if (evt.Target != machineIdentity.Id) return;

            switch (evt.Type)
            {
                case ActionType.PowerOff:
                    SetMachineOn(false);
                    break;

                case ActionType.MachineStart:
                    TryStart(isVerificationTry: false);
                    break;

                case ActionType.TryStart:
                    TryStart(isVerificationTry: true);
                    break;
            }
        }

        private void TryStart(bool isVerificationTry)
        {
            if (_isMachineOn) return;

            bool hasPower = breaker == null ? true : !breaker.IsOffPhysical();
            bool lotoBlocksStart = IsLotoBlockingStart();

            if (!hasPower || lotoBlocksStart)
            {
                // Correcto en LOTO: TryStart debe fallar.
                OnStartDenied?.Invoke();

                if (log)
                    Debug.Log($"[MachineStateController] Start denied. hasPower={hasPower} lotoBlocksStart={lotoBlocksStart}", this);

                if (isVerificationTry && autoPublishVerifyIsolation && runner != null && machineIdentity != null)
                {
                    runner.PublishAction(new ActionEvent(ActionType.VerifyIsolation, machineIdentity.Id));
                }

                return;
            }

            // Arranque real (fuera de LOTO o cuando sí hay energía).
            SetMachineOn(true);
        }

        private bool IsLotoBlockingStart()
        {
            if (Loto == null) return false;

            // Ajusta si quieres exigir también tag.
            bool locked = Loto.IsLockoutDeviceApplied && Loto.IsPadlockApplied;
            return locked;
        }

        private void SetMachineOn(bool value)
        {
            if (_isMachineOn == value) return;

            _isMachineOn = value;

            if (_isMachineOn) OnMachineStarted?.Invoke();
            else OnMachineStopped?.Invoke();

            UpdateDoorAvailability();
        }

        private void UpdateDoorAvailability()
        {
            // Tu regla: puerta habilitada solo si máquina OFF.
            if (Door != null) Door.SetDoorInteractionEnabled(!_isMachineOn);
        }
    }
}
