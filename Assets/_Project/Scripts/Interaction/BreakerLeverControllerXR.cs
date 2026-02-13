using UnityEngine;
using Project.Actions;
using Project.Procedure;

namespace Project.XR
{
    /// <summary>
    /// Controller XR del breaker lever:
    /// - NO rota la palanca (eso lo hace el Transformer del Meta SDK).
    /// - Lee estado físico (ON/OFF) a partir del ángulo actual.
    /// - Publica ActionEvent al core cuando corresponde.
    /// - Soporta bloqueo LOTO deshabilitando componentes de input.
    /// </summary>
    public sealed class BreakerLeverControllerXR : MonoBehaviour
    {
        [Header("Procedure")]
        [SerializeField] private ProcedureRunner runner;
        [SerializeField] private TargetIdentity targetIdentity;

        [Header("Angle source (must be the transform that actually rotates)")]
        [SerializeField] private Transform leverAngleSource;

        [Header("Axis / Range (relative)")]
        [SerializeField] private Axis axis = Axis.X;

        [Tooltip("Delta desde UP hacia DOWN. Ej: -50 si el transformer tiene Min=-50 y Max=0.")]
        [SerializeField] private float downDelta = -50f;

        [Header("OFF definition")]
        [SerializeField] private OffPosition offPosition = OffPosition.Down;

        [Header("Publish behavior")]
        [Tooltip("Si true, publica ToggleBreakerOff solo una vez por sesión (primera vez que llega a OFF).")]
        [SerializeField] private bool publishOffOnlyOnce = true;

        [Tooltip("Umbral de decisión (0..1) para considerar que está en OFF. 0.5 = más cerca del detent OFF que del ON.")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float offThreshold01 = 0.5f;

        [Header("Lock (LOTO) - disable input components")]
        [SerializeField] private Behaviour[] disableWhenLocked;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickClip;
        [SerializeField] private AudioClip thudClip;

        public enum Axis { X, Y, Z }
        public enum OffPosition { Down, Up }

        public bool Locked => _locked;

        private bool _locked;
        private bool _offPublished;

        private float _upAbs;   // ángulo absoluto UP al iniciar (firmado)
        private float _downAbs; // ángulo absoluto DOWN = up + downDelta (firmado)

        private void Awake()
        {
            if (leverAngleSource == null)
                leverAngleSource = transform;

            // Calibración: UP es el ángulo actual en Awake.
            _upAbs = GetAxisAngleSigned();
            _downAbs = NormalizeSigned(_upAbs + downDelta);

            // Si ya inicia en OFF, marca publicado (si corresponde)
            if (IsOffPhysical01(offThreshold01) && publishOffOnlyOnce)
                _offPublished = true;
        }

        /// <summary>
        /// Conectar desde Meta SDK (InteractableUnityEventWrapper -> OnSelected / GrabStarted).
        /// </summary>
        public void OnGrabStarted()
        {
            if (_locked)
            {
                // Feedback de bloqueo (opcional)
                PlayOneShot(thudClip);
            }
        }

        /// <summary>
        /// Conectar desde Meta SDK (InteractableUnityEventWrapper -> OnUnselected / GrabEnded).
        /// Importante: aquí NO snappeamos; el transformer ya resolvió detent/snap.
        /// </summary>
        public void OnGrabEnded()
        {
            if (_locked) return;

            // El transformer ya terminó el movimiento (incluyendo snap suave) cuando llega aquí,
            // pero si tu wrapper dispara antes del snap, cambia esto a esperar 1 frame en el caller.
            if (IsOffPhysical01(offThreshold01))
            {
                PublishToggleOffIfNeeded();
            }
        }

        public void SetLocked(bool value)
        {
            _locked = value;

            if (disableWhenLocked != null)
            {
                for (int i = 0; i < disableWhenLocked.Length; i++)
                {
                    if (disableWhenLocked[i] != null)
                        disableWhenLocked[i].enabled = !_locked;
                }
            }

            if (_locked)
                PlayOneShot(thudClip);
        }

        /// <summary>
        /// Progreso normalizado: 0 = UP (ON), 1 = DOWN (OFF).
        /// </summary>
        public float GetProgress01()
        {
            float cur = GetAxisAngleSigned();
            float deltaFromUp = Mathf.DeltaAngle(_upAbs, cur);

            float min = Mathf.Min(0f, downDelta);
            float max = Mathf.Max(0f, downDelta);

            float clamped = Mathf.Clamp(deltaFromUp, min, max);

            // InverseLerp funciona incluso si downDelta es negativo
            return Mathf.InverseLerp(0f, downDelta, clamped);
        }

        /// <summary>
        /// Estado OFF basado en progreso (robusto).
        /// </summary>
        public bool IsOffPhysical()
        {
            return IsOffPhysical01(offThreshold01);
        }

        /// <summary>
        /// Estado OFF con umbral configurable.
        /// </summary>
        public bool IsOffPhysical01(float threshold01)
        {
            float t = GetProgress01();

            // Si "OFF" es Down, OFF cuando t >= threshold
            if (offPosition == OffPosition.Down)
                return t >= threshold01;

            // Si "OFF" es Up, OFF cuando t < (1 - threshold)
            return t <= (1f - threshold01);
        }

        /// <summary>
        /// Útil para debug/otros sistemas (lockout socket).
        /// </summary>
        public float GetAxisAngleSigned_Public() => GetAxisAngleSigned();

        private void PublishToggleOffIfNeeded()
        {
            if (publishOffOnlyOnce && _offPublished) return;

            _offPublished = true;

            if (runner != null && targetIdentity != null)
            {
                runner.PublishAction(new ActionEvent(ActionType.ToggleBreakerOff, targetIdentity.Id));
                PlayOneShot(clickClip);
            }
        }

        private float GetAxisAngleSigned()
        {
            Vector3 e = leverAngleSource.localEulerAngles; // 0..360
            float raw = axis switch
            {
                Axis.X => e.x,
                Axis.Y => e.y,
                Axis.Z => e.z,
                _ => 0f
            };

            if (raw > 180f) raw -= 360f; // -180..180
            return raw;
        }

        private static float NormalizeSigned(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a < -180f) a += 360f;
            return a;
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource == null) return;
            audioSource.PlayOneShot(clip);
        }
    }
}
