using System.Collections.Generic;
using UnityEngine;

namespace BusanTaxi.Gameplay
{
    /// <summary>
    /// 드리프트 중 뒷바퀴 위치에서 구체 연기를 뿜는다.
    /// 구체는 미리 만들어 둔 풀에서 재사용하며, 커지면서 떠오르고 투명해진다.
    /// </summary>
    public class DriftSmoke : MonoBehaviour
    {
        [SerializeField] private TaxiController car;
        [SerializeField] private Transform[] emitPoints;
        [SerializeField] private Material smokeMaterial;

        [Header("연기")]
        [SerializeField] private int poolSize = 80;
        [SerializeField] private float emitInterval = 0.04f;
        [SerializeField] private float lifetime = 1.1f;
        [SerializeField] private float startScale = 0.35f;
        [SerializeField] private float endScale = 1.6f;
        [SerializeField] private float riseSpeed = 0.7f;
        [SerializeField] private float startAlpha = 0.65f;
        [SerializeField] private float positionJitter = 0.15f;

        private class Puff
        {
            public Transform transform;
            public MeshRenderer renderer;
            public float age;
            public Vector3 velocity;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Puff> active = new List<Puff>();
        private readonly Stack<Puff> free = new Stack<Puff>();
        private MaterialPropertyBlock block;
        private Transform poolRoot;
        private Color baseColor = Color.white;
        private float emitTimer;

        public int ActivePuffCount => active.Count;

        private void Awake()
        {
            if (car == null) car = GetComponent<TaxiController>();
            block = new MaterialPropertyBlock();
            if (smokeMaterial != null && smokeMaterial.HasProperty(BaseColorId))
                baseColor = smokeMaterial.GetColor(BaseColorId);

            // 차를 따라다니지 않도록 월드에 둔다
            poolRoot = new GameObject("DriftSmokePool").transform;
            for (int i = 0; i < poolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "SmokePuff";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(poolRoot, false);
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = smokeMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                free.Push(new Puff { transform = go.transform, renderer = r });
            }
        }

        private void OnDestroy()
        {
            if (poolRoot != null) Destroy(poolRoot.gameObject);
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (car != null && car.IsDrifting)
            {
                emitTimer -= dt;
                while (emitTimer <= 0f)
                {
                    emitTimer += emitInterval;
                    foreach (var p in emitPoints)
                        if (p != null) Emit(p.position);
                }
            }
            else
            {
                emitTimer = 0f;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var puff = active[i];
                puff.age += dt;
                float t = puff.age / lifetime;
                if (t >= 1f)
                {
                    puff.transform.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    free.Push(puff);
                    continue;
                }

                puff.transform.position += puff.velocity * dt;
                puff.velocity *= Mathf.Exp(-2f * dt); // 공기 저항
                float ease = 1f - (1f - t) * (1f - t);
                puff.transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, ease);

                var c = baseColor;
                c.a = startAlpha * (1f - t);
                block.SetColor(BaseColorId, c);
                puff.renderer.SetPropertyBlock(block);
            }
        }

        private void Emit(Vector3 position)
        {
            if (free.Count == 0) return;
            var puff = free.Pop();
            puff.age = 0f;
            puff.transform.position = position + Random.insideUnitSphere * positionJitter;
            puff.transform.localScale = Vector3.one * startScale;
            puff.velocity = Vector3.up * riseSpeed + Random.insideUnitSphere * 0.4f;
            puff.transform.gameObject.SetActive(true);
            active.Add(puff);
        }
    }
}
