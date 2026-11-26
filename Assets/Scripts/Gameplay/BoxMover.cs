using UnityEngine;
using UnityEngine.InputSystem;

namespace BusanTaxi.Gameplay
{
    /// <summary>
    /// 프로젝트 공용 입력 액션 "Player/Move"(WASD, 방향키, 게임패드)로 박스를 XZ 평면에서 이동시킨다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BoxMover : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;

        private Rigidbody body;
        private InputAction moveAction;
        private Vector2 moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void OnEnable()
        {
            moveAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Move") : null;
            if (moveAction == null)
            {
                Debug.LogWarning("[BoxMover] 'Player/Move' 입력 액션을 찾을 수 없습니다.");
                return;
            }
            moveAction.Enable();
        }

        private void Update()
        {
            moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        }

        private void FixedUpdate()
        {
            Vector3 dir = new Vector3(moveInput.x, 0f, moveInput.y);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            Vector3 v = dir * moveSpeed;
            v.y = body.linearVelocity.y; // 중력 유지
            body.linearVelocity = v;
        }
    }
}
