using UnityEngine;
using UnityEngine.InputSystem;

namespace Utils
{
    /// <summary>
    /// 全局屏幕操作相关工具类
    /// </summary>
    public static class ScreenUtils
    {
        /// <summary>
        /// 尝试获取当前鼠标指针对应的世界坐标位置
        /// </summary>
        /// <param name="camera">需要映射的摄像机</param>
        /// <param name="position">对应的世界坐标</param>
        /// <param name="layerMask">检测层级</param>
        /// <param name="planeY">检测失败时，返回的默认平面高度偏移</param>
        /// <param name="maxDistance">最大检测距离</param>
        /// <returns>是否检测成功</returns>
        public static bool TryGetMouseWorldPosition(Camera camera, out Vector3 position, LayerMask layerMask = default, float planeY = 0, float maxDistance = 1000f)
        {
            position = default;

            if (!camera || Mouse.current == null) return false;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = camera.ScreenPointToRay(mousePos);
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, layerMask))
            {
                position = hit.point;
                return true;
            }
            
            Plane plane = new Plane(Vector3.up, new Vector3(0, planeY, 0));

            return RaycastPlane(ray, plane, out position);
        }

        /// <summary>
        /// 射线与平面求交
        /// </summary>
        /// <returns>是否相交</returns>
        public static bool RaycastPlane(Ray ray, Plane plane, out Vector3 hitPoint)
        {
            if (plane.Raycast(ray, out float distance))
            {
                hitPoint = ray.GetPoint(distance);
                return true;
            }

            hitPoint = default;
            return false;
        }
    }
}
