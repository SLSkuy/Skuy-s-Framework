using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Utils.DataStruct;

namespace GamePlay.Navigation
{
    /// <summary>
    /// KD-Tree Debug工具，绘制当前场景中 Agent 构建出的 KD-Tree。
    /// </summary>
    public class KDTreeDrawGizmos : MonoBehaviour
    {
        [Header("绘制开关")]
        [SerializeField] private bool showKDTree = true;
        [SerializeField] private bool showBounds = true;
        [SerializeField] private bool showSplitPlanes = true;
        [SerializeField] private bool showPoints = true;

        [Header("绘制限制")]
        [SerializeField] private int maxDepth = -1;
        [SerializeField] private float pointRadius = 0.12f;
        [SerializeField] private float minBoxHeight = 0.1f;

        [Header("Debug颜色")]
        [SerializeField] private Color rootBoundColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color leafBoundColor = new Color(0.25f, 0.9f, 0.45f, 0.55f);
        [SerializeField] private Color xSplitColor = new Color(1f, 0.2f, 0.2f, 0.65f);
        [SerializeField] private Color ySplitColor = new Color(0.2f, 1f, 0.2f, 0.65f);
        [SerializeField] private Color zSplitColor = new Color(0.25f, 0.55f, 1f, 0.65f);
        [SerializeField] private Color pointColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        private EntityManager _manager;

        private void OnDrawGizmos()
        {
            if (!showKDTree || World.DefaultGameObjectInjectionWorld == null)
            {
                return;
            }

            if (_manager == default)
            {
                _manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            }

            EntityQuery query = _manager.CreateEntityQuery(
                ComponentType.ReadOnly<ASAgent>(),
                ComponentType.ReadOnly<LocalTransform>());

            using NativeArray<LocalTransform> transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            query.Dispose();

            if (transforms.Length == 0)
            {
                return;
            }

            NativeArray<float3> points = new NativeArray<float3>(transforms.Length, Allocator.Temp);
            for (int i = 0; i < transforms.Length; i++)
            {
                points[i] = transforms[i].Position;
            }

            KDTree tree = new KDTree(points, Allocator.Temp);
            try
            {
                NativeArray<KDTreeNode> nodes = tree.DebugNodes;

                if (showBounds || showSplitPlanes)
                {
                    DrawNodeRecursive(nodes, 0, 0);
                }

                if (showPoints)
                {
                    DrawPoints(tree.DebugPoints);
                }
            }
            finally
            {
                tree.Dispose();
                points.Dispose();
            }
        }

        private void DrawNodeRecursive(NativeArray<KDTreeNode> nodes, int nodeIndex, int depth)
        {
            if (nodeIndex < 0 || nodeIndex >= nodes.Length || (maxDepth >= 0 && depth > maxDepth))
            {
                return;
            }

            KDTreeNode node = nodes[nodeIndex];

            if (showBounds)
            {
                Gizmos.color = depth == 0 ? rootBoundColor : GetDepthColor(depth, node.IsLeaf);
                Gizmos.DrawWireCube(GetCenter(node.Bound), GetSize(node.Bound));
            }

            if (showSplitPlanes && !node.IsLeaf)
            {
                Gizmos.color = GetSplitColor(node.PartitionAxis);
                DrawSplitPlane(node);
            }

            if (!node.IsLeaf)
            {
                DrawNodeRecursive(nodes, node.NegativeChildIndex, depth + 1);
                DrawNodeRecursive(nodes, node.PositiveChildIndex, depth + 1);
            }
        }

        private void DrawSplitPlane(KDTreeNode node)
        {
            Vector3 center = GetCenter(node.Bound);
            Vector3 size = GetSize(node.Bound);
            int axis = (int)node.PartitionAxis;
            center[axis] = node.PartitionCoordinate;
            size[axis] = 0f;

            Gizmos.DrawWireCube(center, size);
        }

        private void DrawPoints(NativeArray<float3> points)
        {
            Gizmos.color = pointColor;
            for (int i = 0; i < points.Length; i++)
            {
                Gizmos.DrawSphere(ToVector3(points[i]), pointRadius);
            }
        }

        private Vector3 GetCenter(KDTreeBound bound)
        {
            return ToVector3((bound.Min + bound.Max) * 0.5f);
        }

        private Vector3 GetSize(KDTreeBound bound)
        {
            float3 size = math.max(bound.Size, new float3(0.01f, minBoxHeight, 0.01f));
            return ToVector3(size);
        }

        private Vector3 ToVector3(float3 value)
        {
            return new Vector3(value.x, value.y, value.z);
        }

        private Color GetDepthColor(int depth, bool isLeaf)
        {
            if (isLeaf)
            {
                return leafBoundColor;
            }

            float hue = math.frac(depth * 0.137f);
            Color color = Color.HSVToRGB(hue, 0.55f, 1f);
            color.a = 0.45f;
            return color;
        }

        private Color GetSplitColor(KDTreePartitionAxis axis)
        {
            switch (axis)
            {
                case KDTreePartitionAxis.X:
                    return xSplitColor;
                case KDTreePartitionAxis.Y:
                    return ySplitColor;
                case KDTreePartitionAxis.Z:
                    return zSplitColor;
                default:
                    return rootBoundColor;
            }
        }
    }
}
