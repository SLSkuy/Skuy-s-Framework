using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 把两拍权威位姿写成画面节点与 mesh / orientation 的画面位姿。
    /// </summary>
    public sealed class EntityVisualPresentation : MonoBehaviour
    {
        public const string VISUAL_CHILD_NAME = "visual";
        private const string MESH_CHILD_NAME = "mesh";
        private const string ORIENTATION_CHILD_NAME = "orientation";

        private Transform _root;
        private Transform _visual;
        private Transform _mesh;
        private Transform _orientation;
        private ViewTransform _previous;
        private ViewTransform _current;

        private struct ViewTransform
        {
            public Vector3 RootPosition;
            public Quaternion MeshRotation;
            public Quaternion OrientationRotation;
        }

        /// <summary>
        /// 解析画面节点并与当前权威对齐。
        /// </summary>
        public void Init()
        {
            _root = transform;
            _visual = transform.Find(VISUAL_CHILD_NAME);
            _mesh = _visual.Find(MESH_CHILD_NAME);
            _orientation = _visual.Find(ORIENTATION_CHILD_NAME);
            SnapToAuthority();
        }

        /// <summary>
        /// 步进前把画面节点归单位局部，并把旋转掰回权威。
        /// </summary>
        public void PrepareSimulation()
        {
            _visual.localPosition = Vector3.zero;
            _visual.localRotation = Quaternion.identity;
            ApplyAuthorityRotations();
        }

        /// <summary>
        /// 步进后把当前权威收成「当前拍」，上一拍前移。
        /// </summary>
        public void CaptureAuthority()
        {
            _previous = _current;
            SampleCurrent();
        }

        /// <summary>
        /// 生成、传送、回滚后画面立刻等于权威。
        /// </summary>
        public void SnapToAuthority()
        {
            SampleCurrent();
            _previous = _current;
            _visual.localPosition = Vector3.zero;
            _visual.localRotation = Quaternion.identity;
            ApplyAuthorityRotations();
        }

        /// <summary>
        /// 按残差写出画面位移与朝向。
        /// </summary>
        public void Present(float alpha)
        {
            _visual.position = Vector3.Lerp(_previous.RootPosition, _current.RootPosition, alpha);
            _mesh.rotation = Quaternion.Slerp(_previous.MeshRotation, _current.MeshRotation, alpha);
            _orientation.rotation = Quaternion.Slerp(_previous.OrientationRotation, _current.OrientationRotation, alpha);
        }

        /// <summary>
        /// 采样当前状态
        /// </summary>
        private void SampleCurrent()
        {
            _current.RootPosition = _root.position;
            _current.MeshRotation = _mesh.rotation;
            _current.OrientationRotation = _orientation.rotation;
        }

        /// <summary>
        /// 应用权威的模型状态
        /// </summary>
        private void ApplyAuthorityRotations()
        {
            _mesh.rotation = _current.MeshRotation;
            _orientation.rotation = _current.OrientationRotation;
        }
    }
}
