using Core;
using UnityEngine;

namespace UI.MainScene
{
    public class MainScenePanel : MonoBehaviour
    {
        public void UI_LocalPlay()
        {
            GameCore.Instance.StartLocalPlay();
        }

        public void UI_MultiPlay()
        {
            GameCore.Instance.StartMultiPlay();
        }

        public void UI_Exit()
        {
            GameCore.Instance.QuitGame();
        }
    }
}
