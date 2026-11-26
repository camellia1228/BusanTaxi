using UnityEngine;
using UnityEngine.InputSystem;

namespace BusanTaxi.Gameplay
{
    /// <summary>
    /// 아케이드식 택시 주행.
    /// - W/S(위/아래): 가속 / 브레이크·후진
    /// - A/D(좌/우): 조향
    /// - Space(게임패드 A): 사이드브레이크 → 옆 접지력이 떨어져 드리프트
    /// 속도를 차의 앞(종) / 옆(횡) 성분으로 나눠, 옆 성분을 접지력으로 줄이는 방식.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class TaxiController : MonoBehaviour
    {
        [Header("엔진")]
        [SerializeField] private float maxSpeed = 28f;          // m/s (약 100km/h)
        [SerializeField] private float reverseMaxSpeed = 8f;
        [SerializeField] private float acceleration = 12f;      // m/s²
        [SerializeField] private float brakeDeceleration = 22f;
        [SerializeField] private float coastDeceleration = 2.5f;

        [Header("조향")]
        [SerializeField] private float steerRate = 95f;          // 최대 회전 속도 (deg/s)
        [SerializeField] private float fullSteerSpeed = 7f;      // 이 속도 이상에서 조향이 최대
        [SerializeField] private float driftSteerMultiplier = 1.35f;

        [Header("접지력")]
        [SerializeField] private float grip = 9f;                // 평소 옆 미끄러짐 감쇠
        [SerializeField] private float handbrakeGrip = 0.9f;     // 사이드브레이크 중
        [SerializeField] private float gripRecovery = 6f;        // 사이드브레이크 해제 후 접지력 회복 속도
        [SerializeField] private float handbrakeDeceleration = 5f;

        [Header("드리프트 판정")]
        [SerializeField] private float driftMinSpeed = 6f;
        [SerializeField] private float driftMinLateralSpeed = 2.5f;

        [Header("바퀴 표시")]
        [SerializeField] private Transform[] steerPivots;        // 앞바퀴 (조향 각도 표시)
        [SerializeField] private Transform[] wheelSpinners;      // 모든 바퀴 (굴러가는 회전)
        [SerializeField] private float wheelRadius = 0.35f;
        [SerializeField] private float maxSteerVisualAngle = 30f;

        public bool IsDrifting { get; private set; }
        public bool HandbrakeHeld => handbrakeHeld;
        public float ForwardSpeed { get; private set; }
        public float LateralSpeed { get; private set; }
        public float SpeedKmh => ForwardSpeed * 3.6f;

        private Rigidbody body;
        private InputAction moveAction;
        private InputAction handbrakeAction;
        private Vector2 moveInput;
        private bool handbrakeHeld;
        private float currentGrip;
        private float wheelSpinAngle;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.centerOfMass = new Vector3(0f, 0.4f, 0f);
            currentGrip = grip;

            handbrakeAction = new InputAction("Handbrake", InputActionType.Button);
            handbrakeAction.AddBinding("<Keyboard>/space");
            handbrakeAction.AddBinding("<Gamepad>/buttonSouth");
        }

        private void OnEnable()
        {
            moveAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Move") : null;
            if (moveAction == null) Debug.LogWarning("[TaxiController] 'Player/Move' 입력 액션을 찾을 수 없습니다.");
            else moveAction.Enable();
            handbrakeAction.Enable();
        }

        private void OnDisable()
        {
            handbrakeAction.Disable();
        }

        private void OnDestroy()
        {
            handbrakeAction.Dispose();
        }

        private void Update()
        {
            moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            handbrakeHeld = handbrakeAction.IsPressed();
            UpdateWheelVisuals(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float throttle = moveInput.y;
            float steer = moveInput.x;

            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 v = body.linearVelocity;
            float fSpeed = Vector3.Dot(v, fwd);
            float sSpeed = Vector3.Dot(v, right);

            // --- 종방향: 가속 / 브레이크 / 후진 / 관성 ---
            if (throttle > 0.01f)
            {
                fSpeed = fSpeed < -0.5f
                    ? Mathf.MoveTowards(fSpeed, 0f, brakeDeceleration * throttle * dt)
                    : fSpeed + acceleration * throttle * dt;
            }
            else if (throttle < -0.01f)
            {
                fSpeed = fSpeed > 0.5f
                    ? Mathf.MoveTowards(fSpeed, 0f, brakeDeceleration * -throttle * dt)
                    : fSpeed + acceleration * 0.6f * throttle * dt;
            }
            else
            {
                fSpeed = Mathf.MoveTowards(fSpeed, 0f, coastDeceleration * dt);
            }

            if (handbrakeHeld) fSpeed = Mathf.MoveTowards(fSpeed, 0f, handbrakeDeceleration * dt);
            fSpeed = Mathf.Clamp(fSpeed, -reverseMaxSpeed, maxSpeed);

            // --- 횡방향 접지력: 사이드브레이크면 확 떨어지고, 놓으면 서서히 회복 ---
            currentGrip = handbrakeHeld
                ? handbrakeGrip
                : Mathf.MoveTowards(currentGrip, grip, gripRecovery * dt);
            sSpeed *= Mathf.Exp(-currentGrip * dt);

            body.linearVelocity = fwd * fSpeed + right * sSpeed + Vector3.up * v.y;

            // --- 조향: 움직일 때만 돌고, 후진이면 반대로 ---
            float planarSpeed = new Vector2(fSpeed, sSpeed).magnitude;
            IsDrifting = planarSpeed > driftMinSpeed && Mathf.Abs(sSpeed) > driftMinLateralSpeed;

            float speedFactor = Mathf.Clamp01(Mathf.Abs(fSpeed) / fullSteerSpeed);
            float direction = fSpeed >= 0f ? 1f : -1f;
            float rate = steerRate * (IsDrifting ? driftSteerMultiplier : 1f);
            float yawRate = steer * rate * speedFactor * direction * Mathf.Deg2Rad;
            body.angularVelocity = new Vector3(0f, yawRate, 0f);

            ForwardSpeed = fSpeed;
            LateralSpeed = sSpeed;
        }

        private void UpdateWheelVisuals(float dt)
        {
            float steerAngle = moveInput.x * maxSteerVisualAngle;
            if (steerPivots != null)
                foreach (var p in steerPivots)
                    if (p != null) p.localRotation = Quaternion.Euler(0f, steerAngle, 0f);

            // 사이드브레이크 중엔 뒷바퀴가 잠기지만, 단순화를 위해 전체 바퀴를 같이 처리
            float spinSpeed = handbrakeHeld ? 0f : ForwardSpeed / wheelRadius * Mathf.Rad2Deg;
            wheelSpinAngle = (wheelSpinAngle + spinSpeed * dt) % 360f;
            if (wheelSpinners != null)
                foreach (var w in wheelSpinners)
                    if (w != null) w.localRotation = Quaternion.Euler(wheelSpinAngle, 0f, 0f);
        }
    }
}
