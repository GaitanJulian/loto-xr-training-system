using System.Collections;
using UnityEngine;
using Project.Actions;
using Project.Procedure;

namespace Project.XR
{
    public sealed class TagSnapXR : MonoBehaviour
    {
        [Header("Procedure")]
        [SerializeField] private ProcedureRunner runner;

        [Header("Search")]
        [SerializeField] private float searchRadius = 0.15f;
        [SerializeField] private LayerMask socketLayer = ~0;

        [Header("Meta XR components to disable after snap")]
        [SerializeField] private Behaviour[] grabbables;
        [SerializeField] private float grabCooldownSec = 0.2f;

        private bool _snapped;

        // Conectar desde InteractableUnityEventWrapper -> OnUnselected
        public void TrySnap()
        {
            if (_snapped) return;

            TagSocketXR socket = FindBestSocket();
            if (socket == null) return;
            if (socket.Identity == null) return;

            Transform snapPose = socket.SnapPose != null
                ? socket.SnapPose
                : socket.transform;

            // Snap world correcto usando la utilidad centralizada
            SnapUtility.SnapWorld(
                transform,
                snapPose,
                socket.transform // o socket.transform si prefieres parent más limpio
            );

            _snapped = true;

            // Publicar acción procedural
            if (runner != null)
            {
                runner.PublishAction(new ActionEvent(
                    ActionType.AttachTag,
                    socket.Identity.Id
                ));
            }

            // Deshabilitar grab definitivo
            if (grabCooldownSec > 0f)
                StartCoroutine(DisableGrabThenKeepDisabled(grabCooldownSec));
            else
                SetGrabEnabled(false);
        }


        private TagSocketXR FindBestSocket()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                searchRadius,
                socketLayer,
                QueryTriggerInteraction.Collide
            );

            TagSocketXR best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                var socket = hits[i].GetComponentInParent<TagSocketXR>();
                if (socket == null) continue;

                float d = Vector3.Distance(transform.position, socket.SnapPose.position);
                if (d > socket.SnapRadius) continue;

                if (d < bestDist)
                {
                    bestDist = d;
                    best = socket;
                }
            }

            return best;
        }

        private IEnumerator DisableGrabThenKeepDisabled(float sec)
        {
            SetGrabEnabled(false);
            yield return new WaitForSeconds(sec);
            SetGrabEnabled(false);
        }

        private void SetGrabEnabled(bool enabled)
        {
            if (grabbables == null) return;
            for (int i = 0; i < grabbables.Length; i++)
            {
                if (grabbables[i] != null)
                    grabbables[i].enabled = enabled;
            }
        }

        private static void SetWorldScale(Transform t, Vector3 worldScale)
        {
            Transform p = t.parent;
            if (p == null)
            {
                t.localScale = worldScale;
                return;
            }

            Vector3 ps = p.lossyScale;
            t.localScale = new Vector3(
                ps.x != 0 ? worldScale.x / ps.x : worldScale.x,
                ps.y != 0 ? worldScale.y / ps.y : worldScale.y,
                ps.z != 0 ? worldScale.z / ps.z : worldScale.z
            );
        }
    }
}
