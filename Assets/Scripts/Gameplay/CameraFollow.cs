using UnityEngine;

namespace BusanTaxi.Gameplay
{
    /// <summary>
    /// 대상과 고정 오프셋을 유지하며 부드럽게 따라가는 카메라.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -10f);
        [SerializeField] private float smoothTime = 0.15f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.LookAt(target.position);
        }
    }
}
