using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 全局坐标转换工具类
    /// </summary>
    public static class TransformUtils
    {
        /// <summary>
        /// 将输入方向映射到相机坐标系下的世界方向
        /// 用于保持屏幕上的移动符合玩家输入直觉
        /// </summary>
        /// <param name="inputDirection">输入方向（二维）</param>
        /// <param name="cameraTransform">相机Transform</param>
        /// <returns>映射后的三维方向（y=0）</returns>
        public static Vector3 MapInputToWorldDirection(Vector2 inputDirection, Transform cameraTransform)
        {
            if (inputDirection == Vector2.zero)
                return Vector3.zero;

            // 获取相机的前方向和右方向，忽略y轴分量
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            // 计算世界空间中的移动方向
            Vector3 worldDirection = cameraForward * inputDirection.y + cameraRight * inputDirection.x;

            // 归一化，保持对角线移动速度一致
            if (worldDirection != Vector3.zero)
                worldDirection.Normalize();

            return worldDirection;
        }

        /// <summary>
        /// 将输入方向映射到相机坐标系下的世界方向（返回二维向量）
        /// </summary>
        /// <param name="inputDirection">输入方向（二维）</param>
        /// <param name="cameraTransform">相机Transform</param>
        /// <returns>映射后的二维方向</returns>
        public static Vector2 MapInputToWorldDirection2D(Vector2 inputDirection, Transform cameraTransform)
        {
            Vector3 worldDir3D = MapInputToWorldDirection(inputDirection, cameraTransform);
            return new Vector2(worldDir3D.x, worldDir3D.z);
        }
        
        /// <summary>
        /// 将世界方向转换回输入空间
        /// </summary>
        /// <param name="worldDirection">世界空间方向（三维）</param>
        /// <param name="cameraTransform">相机Transform</param>
        /// <returns>输入空间方向（二维）</returns>
        public static Vector2 MapWorldDirectionToInput(Vector3 worldDirection, Transform cameraTransform)
        {
            // 获取相机的前方向和右方向，忽略y轴分量
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            // 将世界方向投影到相机的前和右方向上
            float forwardComponent = Vector3.Dot(worldDirection, cameraForward);
            float rightComponent = Vector3.Dot(worldDirection, cameraRight);

            return new Vector2(rightComponent, forwardComponent);
        }
    }
}