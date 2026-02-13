/*
 * BreakerDetentRotateTransformer
 * Fork basado en Oculus.Interaction.OneGrabRotateTransformer
 * - Añade detents (Min/Max) y snap suave corto en EndTransform
 * - Mantiene coherencia del estado interno para evitar descalibración
 */

using System;
using UnityEngine;

namespace Oculus.Interaction
{
    public class BreakerDetentRotateTransformer : MonoBehaviour, ITransformer
    {
        public enum Axis
        {
            Right = 0,
            Up = 1,
            Forward = 2
        }

        [SerializeField, Optional]
        private Transform _pivotTransform = null;

        public Transform Pivot => _pivotTransform != null ? _pivotTransform : transform;

        [SerializeField]
        private Axis _rotationAxis = Axis.Up;

        public Axis RotationAxis => _rotationAxis;

        [Serializable]
        public class OneGrabRotateConstraints
        {
            public FloatConstraint MinAngle;
            public FloatConstraint MaxAngle;
        }

        [SerializeField]
        private OneGrabRotateConstraints _constraints =
            new OneGrabRotateConstraints()
            {
                MinAngle = new FloatConstraint(),
                MaxAngle = new FloatConstraint()
            };

        public OneGrabRotateConstraints Constraints
        {
            get => _constraints;
            set => _constraints = value;
        }

        [Header("Detent / Snap")]
        [Range(0.5f, 0.95f)]
        [SerializeField] private float _detentThreshold = 0.75f;

        [Tooltip("Duración del snap suave al soltar (segundos). 0 = snap instantáneo.")]
        [SerializeField] private float _snapDurationSec = 0.12f;

        [Tooltip("Si true, el snap usa SmoothStep (más mecánico). Si false, usa MoveTowards lineal.")]
        [SerializeField] private bool _useSmoothStep = true;

        private float _relativeAngle = 0.0f;
        private float _constrainedRelativeAngle = 0.0f;

        private IGrabbable _grabbable;
        private Vector3 _grabPositionInPivotSpace;
        private Pose _transformPoseInPivotSpace;

        private Pose _worldPivotPose;
        private Vector3 _previousVectorInPivotSpace;

        private Quaternion _localRotation;
        private float _startAngle = 0;

        // Estado de ciclo
        private bool _isTransforming;

        // Snap state
        private bool _isSnapping;
        private float _snapFromAngle;
        private float _snapToAngle;
        private float _snapElapsed;

        // Cache de decisión
        private float _angleAtBegin;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
        }

        public Pose ComputeWorldPivotPose()
        {
            if (_pivotTransform != null)
            {
                return _pivotTransform.GetPose();
            }

            var targetTransform = _grabbable.Transform;

            Vector3 worldPosition = targetTransform.position;
            Quaternion worldRotation = targetTransform.parent != null
                ? targetTransform.parent.rotation * _localRotation
                : _localRotation;

            return new Pose(worldPosition, worldRotation);
        }

        public void BeginTransform()
        {
            _isTransforming = true;
            _isSnapping = false;

            var grabPoint = _grabbable.GrabPoints[0];
            var targetTransform = _grabbable.Transform;

            if (_pivotTransform == null)
            {
                _localRotation = targetTransform.localRotation;
            }

            Vector3 localAxis = Vector3.zero;
            localAxis[(int)_rotationAxis] = 1f;

            _worldPivotPose = ComputeWorldPivotPose();
            Vector3 rotationAxis = _worldPivotPose.rotation * localAxis;

            Quaternion inverseRotation = Quaternion.Inverse(_worldPivotPose.rotation);

            Vector3 grabDelta = grabPoint.position - _worldPivotPose.position;
            if (Mathf.Abs(grabDelta.magnitude) < 0.001f)
            {
                Vector3 localAxisNext = Vector3.zero;
                localAxisNext[((int)_rotationAxis + 1) % 3] = 0.001f;
                grabDelta = _worldPivotPose.rotation * localAxisNext;
            }

            _grabPositionInPivotSpace = inverseRotation * grabDelta;

            Vector3 worldPositionDelta =
                inverseRotation * (targetTransform.position - _worldPivotPose.position);

            Quaternion worldRotationDelta = inverseRotation * targetTransform.rotation;
            _transformPoseInPivotSpace = new Pose(worldPositionDelta, worldRotationDelta);

            Vector3 initialOffset = _worldPivotPose.rotation * _grabPositionInPivotSpace;
            Vector3 initialVector = Vector3.ProjectOnPlane(initialOffset, rotationAxis);
            _previousVectorInPivotSpace = Quaternion.Inverse(_worldPivotPose.rotation) * initialVector;

            _startAngle = _constrainedRelativeAngle;
            _relativeAngle = _startAngle;

            _angleAtBegin = _constrainedRelativeAngle;

            float parentScale = targetTransform.parent != null ? targetTransform.parent.lossyScale.x : 1f;
            _transformPoseInPivotSpace.position /= parentScale;
        }

        public void UpdateTransform()
        {
            if (!_isTransforming) return;

            var grabPoint = _grabbable.GrabPoints[0];
            var targetTransform = _grabbable.Transform;

            Vector3 localAxis = Vector3.zero;
            localAxis[(int)_rotationAxis] = 1f;
            _worldPivotPose = ComputeWorldPivotPose();
            Vector3 rotationAxis = _worldPivotPose.rotation * localAxis;

            Vector3 targetOffset = grabPoint.position - _worldPivotPose.position;
            Vector3 targetVector = Vector3.ProjectOnPlane(targetOffset, rotationAxis);

            Vector3 previousVectorInWorldSpace =
                _worldPivotPose.rotation * _previousVectorInPivotSpace;

            _previousVectorInPivotSpace = Quaternion.Inverse(_worldPivotPose.rotation) * targetVector;

            float signedAngle =
                Vector3.SignedAngle(previousVectorInWorldSpace, targetVector, rotationAxis);

            _relativeAngle += signedAngle;

            _constrainedRelativeAngle = ApplyConstraints(_relativeAngle);

            ApplyAngleToTarget(_constrainedRelativeAngle);
        }

        public void EndTransform()
        {
            _isTransforming = false;

            // Decide detent final (Min o Max) usando threshold y el detent de inicio
            float min = GetMinAngle();
            float max = GetMaxAngle();

            // Normalizado t=0 en MAX (UP), t=1 en MIN (DOWN)
            float tEnd = Normalize01(_constrainedRelativeAngle, min, max);
            float tStart = Normalize01(_angleAtBegin, min, max);

            // ¿Empezó cerca de UP o DOWN?
            bool startedNearDown = tStart > 0.5f;

            float target;
            if (!startedNearDown)
            {
                // arrancó cerca de UP -> necesita cruzar threshold para ir a DOWN
                target = (tEnd >= _detentThreshold) ? min : max;
            }
            else
            {
                // arrancó cerca de DOWN -> necesita volver por debajo de (1-threshold) para ir a UP
                target = (tEnd <= (1f - _detentThreshold)) ? max : min;
            }

            // Snap suave corto
            if (_snapDurationSec <= 0f)
            {
                _relativeAngle = target;
                _constrainedRelativeAngle = target;
                ApplyAngleToTarget(_constrainedRelativeAngle);
                return;
            }

            _isSnapping = true;
            _snapFromAngle = _constrainedRelativeAngle;
            _snapToAngle = target;
            _snapElapsed = 0f;
        }

        private void LateUpdate()
        {
            // Ejecuta el snap post-release dentro del mismo transformer
            if (!_isSnapping) return;

            _snapElapsed += Time.deltaTime;

            float u = Mathf.Clamp01(_snapElapsed / _snapDurationSec);
            if (_useSmoothStep)
                u = u * u * (3f - 2f * u); // SmoothStep

            float a = Mathf.Lerp(_snapFromAngle, _snapToAngle, u);

            _relativeAngle = a;
            _constrainedRelativeAngle = a;

            ApplyAngleToTarget(_constrainedRelativeAngle);

            if (_snapElapsed >= _snapDurationSec)
            {
                _relativeAngle = _snapToAngle;
                _constrainedRelativeAngle = _snapToAngle;
                ApplyAngleToTarget(_constrainedRelativeAngle);
                _isSnapping = false;
            }
        }

        private float ApplyConstraints(float angle)
        {
            float constrained = angle;

            if (Constraints != null)
            {
                if (Constraints.MinAngle.Constrain)
                    constrained = Mathf.Max(constrained, Constraints.MinAngle.Value);

                if (Constraints.MaxAngle.Constrain)
                    constrained = Mathf.Min(constrained, Constraints.MaxAngle.Value);
            }

            return constrained;
        }

        private void ApplyAngleToTarget(float angle)
        {
            var targetTransform = _grabbable.Transform;

            Vector3 localAxis = Vector3.zero;
            localAxis[(int)_rotationAxis] = 1f;

            _worldPivotPose = ComputeWorldPivotPose();
            Vector3 rotationAxis = _worldPivotPose.rotation * localAxis;

            // delta relativo al _startAngle (del BeginTransform)
            // Para snap post-release, _startAngle puede estar viejo; rebasamos cuando no estamos en grab:
            // Si no estamos transformando, queremos que el "baseline" sea el ángulo actual en el que empieza el snap.
            // Solución simple: durante snap dejamos _startAngle igual al valor con el que terminó el grab.
            // En BeginTransform se re-setea correctamente.
            Quaternion deltaRotation = Quaternion.AngleAxis(angle - _startAngle, rotationAxis);

            float parentScale = targetTransform.parent != null ? targetTransform.parent.lossyScale.x : 1f;
            Pose transformDeltaInWorldSpace =
                new Pose(
                    _worldPivotPose.rotation * (parentScale * _transformPoseInPivotSpace.position),
                    _worldPivotPose.rotation * _transformPoseInPivotSpace.rotation);

            Pose transformDeltaRotated = new Pose(
                deltaRotation * transformDeltaInWorldSpace.position,
                deltaRotation * transformDeltaInWorldSpace.rotation);

            targetTransform.position = _worldPivotPose.position + transformDeltaRotated.position;
            targetTransform.rotation = transformDeltaRotated.rotation;
        }

        private float GetMinAngle()
        {
            if (Constraints != null && Constraints.MinAngle.Constrain)
                return Constraints.MinAngle.Value;
            return Mathf.Min(_constrainedRelativeAngle, _relativeAngle);
        }

        private float GetMaxAngle()
        {
            if (Constraints != null && Constraints.MaxAngle.Constrain)
                return Constraints.MaxAngle.Value;
            return Mathf.Max(_constrainedRelativeAngle, _relativeAngle);
        }

        private static float Normalize01(float angle, float min, float max)
        {
            // Queremos: 0 en MAX (UP), 1 en MIN (DOWN)
            // Asumimos max > min típicamente (0 > -50).
            // Si están invertidos, se corrige.
            if (max == min) return 0f;
            return Mathf.InverseLerp(max, min, angle);
        }

        #region Inject (opcional)

        public void InjectOptionalPivotTransform(Transform pivotTransform)
        {
            _pivotTransform = pivotTransform;
        }

        public void InjectOptionalRotationAxis(Axis rotationAxis)
        {
            _rotationAxis = rotationAxis;
        }

        public void InjectOptionalConstraints(OneGrabRotateConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
