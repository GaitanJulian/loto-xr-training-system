using UnityEngine;
using Project.Actions;
using Project.Procedure;

namespace Project.XR
{
    public sealed class BreakerLeverPokeXR : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ProcedureRunner runner;
        [SerializeField] private TargetIdentity targetIdentity;
        [SerializeField] private Transform lever;

        [Header("Meta SDK (optional references for control)")]
        [SerializeField] private Behaviour rotateTransformer; // OneGrabRotateTransformer (referencia como Behaviour para no acoplar versión)

        [Header("Relative setup")]
        [SerializeField] private Axis axis = Axis.X;
        [SerializeField] private float downDelta = -50f;

        [Header("Detent")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float detentThreshold = 0.75f;

        [Header("Snap")]
        [SerializeField] private float snapSpeedDegPerSec = 900f;
        [SerializeField] private float snapEpsilonDeg = 0.5f;

        [Header("Lock (LOTO)")]
        [SerializeField] private bool locked;

        [Header("Procedure")]
        [SerializeField] private OffPosition offPosition = OffPosition.Down;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip thudClip;

        public enum Axis { X, Y, Z }
        public enum OffPosition { Down, Up }
        private enum LeverState { Up, Down }

        private float _upAbs;
        private float _downAbs;

        private LeverState _stateAtGrab;
        private LeverState _state;

        private bool _snapping;
        private float _snapTarget;

        private bool _offPublished;

        private void Awake()
        {
            if (lever == null) lever = transform;

            _upAbs = GetAxisAngleSigned();
            _downAbs = NormalizeSigned(_upAbs + downDelta);

            float cur = GetAxisAngleSigned();
            _state = (Distance(cur, _downAbs) < Distance(cur, _upAbs)) ? LeverState.Down : LeverState.Up;

            if (IsOffState(_state))
                _offPublished = true;
        }

        private void Update()
        {
            if (!_snapping) return;

            float cur = GetAxisAngleSigned();
            float next = Mathf.MoveTowards(cur, _snapTarget, snapSpeedDegPerSec * Time.deltaTime);
            SetAxisAngleSigned(next);

            if (Distance(next, _snapTarget) <= snapEpsilonDeg)
            {
                SetAxisAngleSigned(_snapTarget);
                _snapping = false;

                _state = (Distance(_snapTarget, _downAbs) < Distance(_snapTarget, _upAbs)) ? LeverState.Down : LeverState.Up;

                if (!locked)
                {
                    PublishIfNeeded();
                    PlayOneShot(clickClip);
                }
            }
        }

        // Estos métodos los conectas desde el InteractableUnityEventWrapper

        public void OnHoverEnter()
        {
            // TODO: highlight sutil (luego)
        }

        public void OnHoverExit()
        {
            // TODO: quitar highlight (luego)
        }

        public void OnGrabStarted()
        {
            _stateAtGrab = GetNearestState(GetAxisAngleSigned());

            if (locked)
            {
                // Bloqueo real: idealmente impedir movimiento deshabilitando el transformer
                if (rotateTransformer != null)
                    rotateTransformer.enabled = false;

                PlayOneShot(thudClip);
            }
            else
            {
                if (rotateTransformer != null && !rotateTransformer.enabled)
                    rotateTransformer.enabled = true;
            }
        }

        public void OnGrabEnded()
        {
            // Si estaba locked: volver al estado original y mantener bloqueado
            if (locked)
            {
                float target = (_stateAtGrab == LeverState.Up) ? _upAbs : _downAbs;
                _snapTarget = target;
                _snapping = true;
                return;
            }

            float cur = GetAxisAngleSigned();
            float progress = Progress01FromUp(cur);

            LeverState targetState;
            if (_stateAtGrab == LeverState.Up)
                targetState = (progress >= detentThreshold) ? LeverState.Down : LeverState.Up;
            else
                targetState = (progress <= (1f - detentThreshold)) ? LeverState.Up : LeverState.Down;

            _snapTarget = (targetState == LeverState.Up) ? _upAbs : _downAbs;
            _snapping = true;
        }

        public void SetLocked(bool value)
        {
            locked = value;

            // Al bloquear, también puedes deshabilitar el transformer para prevenir movimiento incluso durante grab
            if (rotateTransformer != null)
                rotateTransformer.enabled = !locked;
        }

        private void PublishIfNeeded()
        {
            _state = GetNearestState(GetAxisAngleSigned());

            if (!IsOffState(_state)) return;
            if (_offPublished) return;

            _offPublished = true;

            if (runner != null && targetIdentity != null)
                runner.PublishAction(new ActionEvent(ActionType.ToggleBreakerOff, targetIdentity.Id));
        }

        private LeverState GetNearestState(float curSigned)
        {
            return (Distance(curSigned, _downAbs) < Distance(curSigned, _upAbs)) ? LeverState.Down : LeverState.Up;
        }

        private bool IsOffState(LeverState s)
        {
            return offPosition == OffPosition.Down ? s == LeverState.Down : s == LeverState.Up;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }

        private float GetAxisAngleSigned()
        {
            Vector3 e = lever.localEulerAngles;
            float raw = axis switch
            {
                Axis.X => e.x,
                Axis.Y => e.y,
                Axis.Z => e.z,
                _ => 0f
            };

            if (raw > 180f) raw -= 360f;
            return raw;
        }

        private void SetAxisAngleSigned(float angleSigned)
        {
            float a = angleSigned % 360f;
            if (a < 0f) a += 360f;

            Vector3 e = lever.localEulerAngles;
            switch (axis)
            {
                case Axis.X: e.x = a; break;
                case Axis.Y: e.y = a; break;
                case Axis.Z: e.z = a; break;
            }
            lever.localEulerAngles = e;
        }

        private static float NormalizeSigned(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }

        private static float Distance(float a, float b)
        {
            return Mathf.Abs(Mathf.DeltaAngle(a, b));
        }

        private float Progress01FromUp(float currentSigned)
        {
            float deltaFromUp = Mathf.DeltaAngle(_upAbs, currentSigned);

            float min = Mathf.Min(0f, downDelta);
            float max = Mathf.Max(0f, downDelta);

            float clamped = Mathf.Clamp(deltaFromUp, min, max);
            return Mathf.InverseLerp(0f, downDelta, clamped);
        }
    }
}
