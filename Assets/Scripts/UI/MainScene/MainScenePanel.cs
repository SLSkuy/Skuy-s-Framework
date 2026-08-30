using Core;
using UnityEngine;

namespace UI.MainScene
{
    public class MainScenePanel : MonoBehaviour
    {
        public void UI_LocalPlay()
        {

        }

        public void UI_MultiPlay()
        {

        }

        public void UI_Exit()
        {
            GameCore.Instance.QuitGame();
        }
    }
}
