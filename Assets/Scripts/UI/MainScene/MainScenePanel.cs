using Framework;
using GamePlay.Procedure;

namespace UI.MainScene
{
    public class MainScenePanel : UnityEngine.MonoBehaviour
    {
        public void UI_LocalPlay()
        {
            ProcedureCore.Instance.StartLocalPlay();
        }

        public void UI_MultiPlay()
        {
            ProcedureCore.Instance.HostMultiplay();
        }

        public void UI_Join()
        {
            ProcedureCore.Instance.JoinRemote();
        }

        public void UI_Exit()
        {
            Global.QuitGame();
        }
    }
}
