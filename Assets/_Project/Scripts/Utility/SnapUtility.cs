using UnityEngine;

namespace Project.XR
{
    public static class SnapUtility
    {
        public static void SnapWorld(Transform item, Transform snapPose, Transform parentAfterSnap = null)
        {
            if (item == null || snapPose == null)
                return;

            Vector3 worldScale = item.lossyScale;

            // 1) Copiar pose WORLD
            item.SetPositionAndRotation(snapPose.position, snapPose.rotation);

            // 2) Parent manteniendo pose mundial
            if (parentAfterSnap != null)
                item.SetParent(parentAfterSnap, true);

            // 3) Restaurar escala mundial
            SetWorldScale(item, worldScale);
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
