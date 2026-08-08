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
        
        #endregion
        
        #region 生命周期

        private void Awake()
        {
            _character = GetComponent<EntityCharacter>();
            _inputProvider = GetComponent<IInputStateProvider>();
        }

        #endregion
    }
}