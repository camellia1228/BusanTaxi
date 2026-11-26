using UnityEngine;

namespace BusanTaxi.Gameplay
{
    /// <summary>
    /// 차 뒤를 따라가는 추적 카메라.
    /// 차의 방향(yaw)을 살짝 늦게 따라가서 드리프트할 때 차가 옆으로 돌아간 모습이 보인다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 7.5f;
        [SerializeField] private float height = 3.2f;
        [SerializeField] private float lookHeight = 1.0f;
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float yawFollowSharpness = 3.5f;

        private Vector3 velocity;
        private float currentYaw;

        private void Start()
        {
            if (target == null) return;
            currentYaw = target.eulerAngles.y;
            transform.position = DesiredPosition();
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            currentYaw = Mathf.LerpAngle(currentYaw, target.eulerAngles.y, 1f - Mathf.Exp(-yawFollowSharpness * dt));
            transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, positionSmoothTime);
            transform.LookAt(target.position + Vector3.up * lookHeight);
        }

        private Vector3 DesiredPosition()
        {
            return target.position + Quaternion.Euler(0f, currentYaw, 0f) * new Vector3(0f, height, -distance);
        }
    }
}
