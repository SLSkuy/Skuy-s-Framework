using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Framework
{
    /// <summary>
    /// Mono-ECS 桥接：鼠标点击与场景 Plane 求交，为所有 ASAgent 更新寻路目标
    /// </summary>
    public class AgentClickBridge : MonoBehaviour
    {
        [Header("相机")]
        public Camera targetCamera;

        [Header("Plane 设置")]
        [Tooltip("场景中的 Plane 游戏对象，射线将与其求交")]
        public Transform targetPlane;

        [Header("调试")]
        [SerializeField] private bool debugDrawRay;

        private EntityManager _entityManager;

        private void Start()
        {
            if (targetCamera == null) targetCamera = Camera.main;

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            if (!targetCamera || !targetPlane) return;

            // 检测鼠标左键按下
            if (!Mouse.current.leftButton.isPressed) return;

            Ray ray = targetCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            // 与场景中 Plane 对象所在平面求交
            Plane plane = new Plane(targetPlane.up, targetPlane.position);
            if (!plane.Raycast(ray, out float enter)) return;

            float3 targetPoint = ray.GetPoint(enter);

#if UNITY_EDITOR
            if (debugDrawRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * enter, Color.green, 2f);
            }
#endif

            SetDestinationForAll(targetPoint);
        }

        /// <summary>
        /// 为所有 ASAgent 实体设置寻路目标
        /// </summary>
        private void SetDestinationForAll(float3 destination)
        {
            var query = _entityManager.CreateEntityQuery(typeof(ASAgent));
            using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            if (entities.Length == 0) return;

            var requester = new ASRequester { Destination = destination };

            foreach (var entity in entities)
            {
                // 写入 ASRequester（触发 PFRequestSystem 重新寻路）
                if (_entityManager.HasComponent<ASRequester>(entity))
                    _entityManager.SetComponentData(entity, requester);
                else
                    _entityManager.AddComponentData(entity, requester);

                // 重置移动状态
                if (_entityManager.HasComponent<ASFollower>(entity))
                {
                    var follower = _entityManager.GetComponentData<ASFollower>(entity);
                    follower.PathAvailable = false;
                    follower.TargetIndex = 0;
                    follower.DestinationReached = false;
                    _entityManager.SetComponentData(entity, follower);
                }

                // 清除旧路径数据
                if (_entityManager.HasBuffer<ASPathBuffer>(entity))
                {
                    var pathBuffer = _entityManager.GetBuffer<ASPathBuffer>(entity);
                    pathBuffer.Clear();
                }
            }
        }
    }
}
