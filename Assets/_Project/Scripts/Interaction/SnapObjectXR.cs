using UnityEngine;
using Project.Actions;
using Project.Procedure;

namespace Project.XR
{
    [RequireComponent(typeof(Collider))]
    public sealed class SnapObjectXR : MonoBehaviour
    {
        public enum ObjectType
        {
            Lockout,
            Padlock,
            Tag
        }

        [Header("Type")]
        [SerializeField] private ObjectType objectType = ObjectType.Lockout;

        [Header("Procedure")]
        [SerializeField] private ProcedureRunner runner;

        [Tooltip("Acci�n que se publica cuando este objeto se coloca correctamente.")]
        [SerializeField] private ActionType actionTypeOnSnap = ActionType.AttachLockoutDevice;

        [Header("Physics / Interaction")]
        [SerializeField] private Rigidbody rb;

        [Tooltip("Componentes que se deshabilitan al snappear para que no se vuelva a agarrar.")]
        [SerializeField] private Behaviour[] disableAfterSnap;

        [Header("Parenting")]
        [SerializeField] private bool parentToSocket = true;

        [Header("Optional: lock breaker when snapped (for Lockout)")]
        [SerializeField] private bool enableBreakerLockOnSnap = true;
        [SerializeField] private BreakerLeverControllerXR breakerLockController;

        private bool _snapped;

        private void Reset()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Awake()
        {
            // Recomendaci�n: el collider que detecta sockets debe ser trigger.
            // Si necesitas collider s�lido para Meta SDK, usa un collider hijo trigger para snap.
        }

        private void OnTriggerStay(Collider other)
        {
            if (_snapped) return;

            if (!other.TryGetComponent<SnapSocketXR>(out var socket))
                return;

            if (!IsCompatible(socket))
                return;

            float d = Vector3.Distance(transform.position, socket.SnapPose.position);
            if (d > socket.SnapRadius)
                return;

            SnapTo(socket);
        }

        private bool IsCompatible(SnapSocketXR socket)
        {
            return (objectType == ObjectType.Lockout && socket.Type == SnapSocketXR.SocketType.Lockout)
                || (objectType == ObjectType.Padlock && socket.Type == SnapSocketXR.SocketType.Padlock)
                || (objectType == ObjectType.Tag && socket.Type == SnapSocketXR.SocketType.Tag);
        }

        private void SnapTo(SnapSocketXR socket)
        {
            _snapped = true;

            // 1) Align pose
            transform.SetPositionAndRotation(socket.SnapPose.position, socket.SnapPose.rotation);

            // 2) Freeze physics
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // 3) Disable Meta interaction components
            if (disableAfterSnap != null)
            {
                for (int i = 0; i < disableAfterSnap.Length; i++)
                {
                    if (disableAfterSnap[i] != null)
                        disableAfterSnap[i].enabled = false;
                }
            }

            // 4) Publish action event
            if (runner != null && socket.Identity != null)
            {
                runner.PublishAction(new ActionEvent(actionTypeOnSnap, socket.Identity.Id));
            }

            // 5) Optional: lock breaker immediately when lockout installed
            if (enableBreakerLockOnSnap && objectType == ObjectType.Lockout && breakerLockController != null)
            {
                breakerLockController.SetLocked(true);
            }

            // 6) Parent
            if (parentToSocket)
            {
                transform.SetParent(socket.transform, true);
            }
        }
    }
}
