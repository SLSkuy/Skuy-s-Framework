using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 客户端本地玩家控制器，负责控制客户端对应角色
    /// </summary>
    public class LocalController : AutoEventMonoBehaviour
    {
        private IInputStateProvider _inputProvider;
        private EntityCharacter _character;

        #region 实体控制
        
        [AutoEvent("OnMove", nameof(_inputProvider))]
        private void Move(Vector2 move)
        {
            _character.Move(move);
        }

        [AutoEvent("OnAim", nameof(_inputProvider))]
        private void Aim(Vector2 aim)
        {
            _character.Aim(aim);
        }

        [AutoEvent("OnJump", nameof(_inputProvider))]
        private void Jump()
        {
            _character.Jump();
        }

        [AutoEvent("OnSprintPressed", nameof(_inputProvider))]
        private void SprintPressed()
        {
            _character.StartSprint();
        }

        [AutoEvent("OnSprintReleased", nameof(_inputProvider))]
        private void SprintReleased()
        {
            _character.StopSprint();
        }

        [AutoEvent("OnSwitchModePressed", nameof(_inputProvider))]
        private void SwitchModePressed()
        {
            _character.ToggleRun();
        }

        #endregion

        #region 生命周期

        protected override void Start()
        {
            _character = GetComponent<EntityCharacter>();
            _inputProvider = GetComponent<IInputStateProvider>();

            Cursor.lockState = CursorLockMode.Locked;
            Global.Get<CameraManager>().SetTarget(transform.Find("orientation"));

            base.Start();
        }

        #endregion
    }
}
