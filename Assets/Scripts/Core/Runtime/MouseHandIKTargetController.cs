using UnityEngine;

namespace Core.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MouseHandIKTargetController : MonoBehaviour
    {
        private enum MouseHitMode
        {
            PhysicsRaycast,
            Plane
        }

        [Header("Core Bindings")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform reachOrigin;
        [SerializeField] private Transform handTarget;

        [Header("Mouse Hit")]
        [SerializeField] private MouseHitMode hitMode = MouseHitMode.PhysicsRaycast;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float rayDistance = 100f;
        [SerializeField] private Vector3 planeNormal = Vector3.forward;

        [Header("Movement Limit")]
        [SerializeField] private float maxReach = 1f;
        [SerializeField] private float followSpeed = 20f;
        [SerializeField] private Vector3 targetOffset;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null || reachOrigin == null || handTarget == null)
            {
                return;
            }

            if (!TryGetMouseWorldPoint(out var targetPosition))
            {
                return;
            }

            targetPosition += targetOffset;
            targetPosition = ClampToReach(targetPosition);

            var lerpRate = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
            handTarget.position = Vector3.Lerp(handTarget.position, targetPosition, lerpRate);
        }

        private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
        {
            var ray = targetCamera.ScreenPointToRay(Input.mousePosition);

            if (hitMode == MouseHitMode.PhysicsRaycast)
            {
                if (Physics.Raycast(ray, out var hit, rayDistance, hitMask))
                {
                    worldPoint = hit.point;
                    return true;
                }

                worldPoint = default;
                return false;
            }

            var normal = planeNormal.sqrMagnitude > 0f ? planeNormal.normalized : Vector3.forward;
            var plane = new Plane(normal, reachOrigin.position);
            if (plane.Raycast(ray, out var enter))
            {
                worldPoint = ray.GetPoint(enter);
                return true;
            }

            worldPoint = default;
            return false;
        }

        private Vector3 ClampToReach(Vector3 targetPosition)
        {
            if (maxReach <= 0f)
            {
                return targetPosition;
            }

            var offset = targetPosition - reachOrigin.position;
            if (offset.sqrMagnitude <= maxReach * maxReach)
            {
                return targetPosition;
            }

            return reachOrigin.position + offset.normalized * maxReach;
        }
    }
}
