using Framework;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    public class PlayerController : AutoEventMonoBehaviour
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